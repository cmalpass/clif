// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace CLIF.Mcp.Core;

/// <summary>
/// Manages UI Automation sessions, window tracking, and application launching.
/// </summary>
public class WindowSessionManager : IDisposable
{
    private static readonly Regex HandlePattern = new(@"^w[1-9][0-9]{0,8}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private const int WindowSearchRetries = 10;
    private const int WindowSearchDelayMs = 500;
    private const int AppLaunchDelayMs = 1000;
    private const int InputIdleTimeoutMs = 5000;

    private readonly UIA3Automation _automation;
    private readonly Dictionary<string, Window> _windows = new();
    private readonly Dictionary<nint, string> _nativeHandleToHandle = new();
    private readonly Dictionary<string, int> _windowProcessIds = new();
    private readonly Dictionary<string, Process> _launchedProcesses = new();
    private readonly object _sync = new();
    private int _windowCounter;
    private bool _disposed;

    /// <summary>Raised when a tracked window is removed because it closed or its process exited.</summary>
    public event Action<string>? WindowRemoved;

    /// <summary>
    /// Initializes a session manager backed by UI Automation 3.
    /// </summary>
    public WindowSessionManager()
    {
        _automation = new UIA3Automation();
    }

    /// <summary>
    /// Provides access to the underlying UIA3Automation instance.
    /// </summary>
    public UIA3Automation Automation => _automation;

    /// <summary>
    /// Launch an application and return the window handle and Window object.
    /// </summary>
    public (string handle, Window window) LaunchApp(
        string appPath,
        string[]? args = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var psi = new ProcessStartInfo
        {
            FileName = appPath,
            UseShellExecute = true,
        };

        if (args != null)
        {
            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }
        }

        var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {appPath}");
        }

        void AbortLaunch()
        {
            if (!process.HasExited)
            {
                try
                {
                    process.CloseMainWindow();
                }
                catch
                {
                    // The process may have exited while cancellation was observed.
                }

                if (!process.HasExited)
                {
                    try
                    {
                        if (!process.WaitForExit(1_000) && !process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                            process.WaitForExit(1_000);
                        }
                    }
                    catch
                    {
                        // Best-effort cleanup; the original launch error is more useful.
                    }
                }
            }

            process.Dispose();
        }

        try
        {
            process.WaitForInputIdle(InputIdleTimeoutMs);
        }
        catch
        {
            // Some processes don't support WaitForInputIdle
        }

        if (cancellationToken.WaitHandle.WaitOne(AppLaunchDelayMs))
        {
            AbortLaunch();
            cancellationToken.ThrowIfCancellationRequested();
        }

        var desktop = _automation.GetDesktop();
        Window? window = null;

        // Try to find by process ID first
        var element = desktop.FindFirstDescendant(cf => cf.ByProcessId(process.Id));
        if (element != null)
        {
            window = element.AsWindow();
        }

        // Fallback: search for new windows by title matching
        if (window == null)
        {
            var existingTitles = new HashSet<string>(
                _windows.Values
                    .Select(w => w.Title)
                    .Where(t => !string.IsNullOrEmpty(t)));

            for (int i = 0; i < WindowSearchRetries && window == null; i++)
            {
                if (cancellationToken.WaitHandle.WaitOne(WindowSearchDelayMs))
                {
                    AbortLaunch();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                cancellationToken.ThrowIfCancellationRequested();
                var windows = desktop.FindAllChildren(
                    cf => cf.ByControlType(ControlType.Window));

                foreach (var w in windows)
                {
                    var win = w.AsWindow();
                    if (win != null && !string.IsNullOrEmpty(win.Title))
                    {
                        var title = win.Title.ToLowerInvariant();
                        var appName = Path.GetFileNameWithoutExtension(appPath).ToLowerInvariant();
                        if (title.Contains(appName) || !existingTitles.Contains(win.Title))
                        {
                            window = win;
                            break;
                        }
                    }
                }
            }
        }

        if (window == null)
        {
            AbortLaunch();
            throw new InvalidOperationException(
                $"Could not find window for {appPath}. Try using clif_list_windows and clif_focus instead.");
        }

        if (process.HasExited)
        {
            AbortLaunch();
            throw new InvalidOperationException($"Application exited before its window became usable: {appPath}");
        }

        var windowHandle = RegisterWindow(window);
        lock (_sync)
        {
            _launchedProcesses[windowHandle] = process;
            process.EnableRaisingEvents = true;
            process.Exited += (_, _) => OnLaunchedProcessExited(windowHandle, process);
        }

        return (windowHandle, window);
    }

    /// <summary>
    /// Attach to an existing window by its title.
    /// </summary>
    public (string handle, Window window) AttachToWindow(string title)
    {
        var desktop = _automation.GetDesktop();
        var window = desktop.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Window).And(cf.ByName(title)))?.AsWindow();

        if (window == null)
        {
            throw new InvalidOperationException($"Window not found: {title}");
        }

        var handle = RegisterWindow(window);
        return (handle, window);
    }

    /// <summary>
    /// Register a window and return its handle. De-duplicates by native window handle
    /// so the same window always gets the same logical handle.
    /// </summary>
    public string RegisterWindow(Window window)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CleanupInactiveWindows();
        var nativeHandle = window.Properties.NativeWindowHandle.ValueOrDefault;
        var processId = window.Properties.ProcessId.ValueOrDefault;
        lock (_sync)
        {
            if (nativeHandle != default && _nativeHandleToHandle.TryGetValue(nativeHandle, out var existingHandle))
            {
                // Update the window object in case it was refreshed, but keep the same handle.
                _windows[existingHandle] = window;
                if (processId > 0)
                {
                    _windowProcessIds[existingHandle] = processId;
                }

                return existingHandle;
            }

            var handle = $"w{++_windowCounter}";
            _windows[handle] = window;
            if (processId > 0)
            {
                _windowProcessIds[handle] = processId;
            }

            if (nativeHandle != default)
            {
                _nativeHandleToHandle[nativeHandle] = handle;
            }

            return handle;
        }
    }

    /// <summary>
    /// Get a window by its handle.
    /// </summary>
    public Window? GetWindow(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || !HandlePattern.IsMatch(handle))
        {
            return null;
        }

        CleanupInactiveWindows();
        lock (_sync)
        {
            return _windows.TryGetValue(handle, out var window) ? window : null;
        }
    }

    /// <summary>
    /// List all currently open windows on the desktop.
    /// </summary>
    public List<(string handle, string title, string? processName)> ListWindows()
    {
        CleanupInactiveWindows();
        var desktop = _automation.GetDesktop();
        var windows = desktop.FindAllChildren(
            cf => cf.ByControlType(ControlType.Window));

        var result = new List<(string, string, string?)>();
        foreach (var w in windows)
        {
            var window = w.AsWindow();
            if (window != null && !string.IsNullOrEmpty(window.Title))
            {
                var handle = RegisterWindow(window);
                string? processName = null;
                try
                {
                    processName = window.Properties.ProcessId.TryGetValue(out var pid)
                        ? Process.GetProcessById(pid).ProcessName
                        : null;
                }
                catch
                {
                    // Process may have exited
                }

                result.Add((handle, window.Title, processName));
            }
        }

        return result;
    }

    /// <summary>
    /// Focus a window by its handle.
    /// </summary>
    public void FocusWindow(string handle)
    {
        var window = GetWindow(handle)
            ?? throw new InvalidOperationException($"Window not found: {handle}");
        window.Focus();
    }

    /// <summary>
    /// Close a window by its handle.
    /// </summary>
    public void CloseWindow(string handle)
    {
        var window = GetWindow(handle)
            ?? throw new InvalidOperationException($"Window not found: {handle}");
        var nativeHandle = window.Properties.NativeWindowHandle.ValueOrDefault;
        window.Close();
        Process? process = null;
        lock (_sync)
        {
            _windows.Remove(handle);
            if (nativeHandle != default)
            {
                _nativeHandleToHandle.Remove(nativeHandle);
            }

            if (_launchedProcesses.Remove(handle, out process))
            {
                // The exit callback is harmless after removal and may already be queued.
            }
        }

        if (process != null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    process.WaitForExit(2_000);
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(2_000);
                    }
                }
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Process[] processes;
        lock (_sync)
        {
            processes = _launchedProcesses.Values.ToArray();
            _launchedProcesses.Clear();
            _windows.Clear();
            _nativeHandleToHandle.Clear();
            _windowProcessIds.Clear();
        }

        foreach (var process in processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                    if (!process.WaitForExit(2_000) && !process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(2_000);
                    }
                }
            }
            catch
            {
                // Cleanup must not prevent the UIA provider from being disposed.
            }
            finally
            {
                process.Dispose();
            }
        }

        _automation.Dispose();
        GC.SuppressFinalize(this);
    }

    private void CleanupInactiveWindows()
    {
        string[] exitedLaunchedHandles;
        lock (_sync)
        {
            exitedLaunchedHandles = _launchedProcesses
                .Where(pair => HasExited(pair.Value))
                .Select(pair => pair.Key)
                .ToArray();
        }

        foreach (var handle in exitedLaunchedHandles)
        {
            RemoveWindow(handle);
        }

        TrackedWindow[] trackedWindows;
        lock (_sync)
        {
            trackedWindows = _windows
                .Select(pair => new TrackedWindow(
                    pair.Key,
                    pair.Value,
                    _windowProcessIds.GetValueOrDefault(pair.Key)))
                .ToArray();
        }

        var inactiveHandles = trackedWindows
            .Where(window => !IsWindowActive(window))
            .Select(window => window.Handle)
            .ToArray();

        foreach (var handle in inactiveHandles)
        {
            RemoveWindow(handle);
        }
    }

    /// <summary>Invalidates a handle after its UI Automation provider becomes unavailable.</summary>
    public void InvalidateWindow(string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || !HandlePattern.IsMatch(handle))
        {
            return;
        }

        RemoveWindow(handle);
    }

    private static bool IsWindowActive(TrackedWindow trackedWindow)
    {
        try
        {
            var nativeHandle = trackedWindow.Window.Properties.NativeWindowHandle.ValueOrDefault;
            if (nativeHandle != default && !IsWindow(nativeHandle))
            {
                return false;
            }

            if (trackedWindow.ProcessId <= 0)
            {
                // A provider that does not expose a process ID cannot be checked here.
                // Retain the handle and let the requested UIA operation report its error.
                return true;
            }

            if (trackedWindow.Window.Properties.ProcessId.TryGetValue(out var currentProcessId) &&
                currentProcessId > 0 && currentProcessId != trackedWindow.ProcessId)
            {
                return false;
            }

            using var process = Process.GetProcessById(trackedWindow.ProcessId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (COMException)
        {
            // UIA reports stale providers as COM failures (for example, after a
            // target process is terminated). Those handles must not remain usable.
            return false;
        }
        catch
        {
            // Do not discard an otherwise valid handle merely because Windows denied
            // inspection of the target process.
            return true;
        }
    }

    private static bool HasExited(Process process)
    {
        try
        {
            return process.HasExited;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private void OnLaunchedProcessExited(string handle, Process process)
    {
        RemoveWindow(handle);
    }

    private void RemoveWindow(string handle)
    {
        Process? process = null;
        var removed = false;
        lock (_sync)
        {
            if (!_windows.Remove(handle, out var window))
            {
                return;
            }

            removed = true;

            var nativeHandle = window.Properties.NativeWindowHandle.ValueOrDefault;
            if (nativeHandle != default)
            {
                _nativeHandleToHandle.Remove(nativeHandle);
            }

            _windowProcessIds.Remove(handle);
            _launchedProcesses.Remove(handle, out process);
        }

        process?.Dispose();
        if (removed)
        {
            WindowRemoved?.Invoke(handle);
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint hWnd);

    private readonly record struct TrackedWindow(string Handle, Window Window, int ProcessId);
}
