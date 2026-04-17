// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using System.Diagnostics;
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
    private readonly UIA3Automation _automation;
    private readonly Dictionary<string, Window> _windows = new();
    private int _windowCounter;

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
    public (string handle, Window window) LaunchApp(string appPath, string[]? args = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = appPath,
            Arguments = args != null ? string.Join(" ", args) : string.Empty,
            UseShellExecute = true,
        };

        var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {appPath}");
        }

        try
        {
            process.WaitForInputIdle(5000);
        }
        catch
        {
            // Some processes don't support WaitForInputIdle
        }

        Thread.Sleep(1000);

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

            for (int i = 0; i < 10 && window == null; i++)
            {
                Thread.Sleep(500);
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
            throw new InvalidOperationException(
                $"Could not find window for {appPath}. Try using clif_list_windows and clif_focus instead.");
        }

        var windowHandle = RegisterWindow(window);
        return (windowHandle, window);
    }

    /// <summary>
    /// Attach to an existing window by its title.
    /// </summary>
    public (string handle, Window window) AttachToWindow(string title)
    {
        var desktop = _automation.GetDesktop();
        var window = desktop.FindFirstDescendant(cf => cf.ByName(title))?.AsWindow();

        if (window == null)
        {
            throw new InvalidOperationException($"Window not found: {title}");
        }

        var handle = RegisterWindow(window);
        return (handle, window);
    }

    /// <summary>
    /// Register a window and return its handle.
    /// </summary>
    public string RegisterWindow(Window window)
    {
        var handle = $"w{++_windowCounter}";
        _windows[handle] = window;
        return handle;
    }

    /// <summary>
    /// Get a window by its handle.
    /// </summary>
    public Window? GetWindow(string handle)
    {
        return _windows.TryGetValue(handle, out var window) ? window : null;
    }

    /// <summary>
    /// List all currently open windows on the desktop.
    /// </summary>
    public List<(string handle, string title, string? processName)> ListWindows()
    {
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
        window.Close();
        _windows.Remove(handle);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _windows.Clear();
        _automation.Dispose();
        GC.SuppressFinalize(this);
    }
}
