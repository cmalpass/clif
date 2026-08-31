// <copyright file="SessionCaptureService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using Microsoft.Extensions.Logging;

namespace CLIF.Services;

/// <summary>
/// Captures screenshots and interaction logs for an automation session.
/// </summary>
public interface ISessionCaptureService
{
    /// <summary>Starts a new capture session.</summary>
    /// <param name="sessionName">Optional session identifier.</param>
    /// <param name="targetWindow">Optional window to capture.</param>
    /// <returns>The new session identifier.</returns>
    Task<string> StartSessionAsync(string? sessionName = null, AutomationElement? targetWindow = null);

    /// <summary>Captures a screenshot and log entry after an interaction.</summary>
    /// <param name="actionType">Action that was performed.</param>
    /// <param name="elementInfo">Description of the target element.</param>
    /// <param name="success">Whether the interaction succeeded.</param>
    /// <param name="validationResult">Optional validation detail.</param>
    Task CaptureAfterInteractionAsync(string actionType, string elementInfo, bool success, string? validationResult = null);

    /// <summary>Writes an interaction message to the current session log.</summary>
    /// <param name="message">Message to record.</param>
    /// <param name="level">Severity assigned to the message.</param>
    Task LogInteractionAsync(string message, LogLevel level = LogLevel.Information);

    /// <summary>Ends the current capture session.</summary>
    Task EndSessionAsync();

    /// <summary>Sets the window used for subsequent captures.</summary>
    /// <param name="targetWindow">Window to capture, or <see langword="null"/> for the full screen.</param>
    void SetTargetWindow(AutomationElement? targetWindow);

    /// <summary>Gets the current session identifier, if a session is active.</summary>
    string? CurrentSessionId { get; }

    /// <summary>Gets the path of the current session, if a session is active.</summary>
    string? CurrentSessionPath { get; }
}

/// <summary>
/// Persists screenshots and interaction logs for an automation session.
/// </summary>
public class SessionCaptureService : ISessionCaptureService
{
    // Windows API for bringing window to foreground
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    private readonly ILogger<SessionCaptureService> logger;
    private string? currentSessionId;
    private string? currentSessionPath;
    private string? logFilePath;
    private int captureCounter = 0;
    private readonly object lockObject = new();
    private AutomationElement? targetWindow;

    /// <inheritdoc />
    public string? CurrentSessionId => this.currentSessionId;
    /// <inheritdoc />
    public string? CurrentSessionPath => this.currentSessionPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionCaptureService"/> class.
    /// Initializes the session capture service.
    /// </summary>
    /// <param name="logger">Logger used for capture diagnostics.</param>
    public SessionCaptureService(ILogger<SessionCaptureService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> StartSessionAsync(string? sessionName = null, AutomationElement? targetWindow = null)
    {
        return await Task.Run(() =>
        {
            lock (this.lockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                this.currentSessionId = sessionName ?? $"CLIF_Session_{timestamp}";
                this.targetWindow = targetWindow;

                // Create session directory in the workspace root
                var workspaceRoot = this.FindWorkspaceRoot();
                var sessionsDir = Path.Combine(workspaceRoot, "sessions");
                this.currentSessionPath = Path.Combine(sessionsDir, this.currentSessionId);

                Directory.CreateDirectory(this.currentSessionPath);
                Directory.CreateDirectory(Path.Combine(this.currentSessionPath, "screenshots"));

                // Create log file
                this.logFilePath = Path.Combine(this.currentSessionPath, "session.log");
                this.captureCounter = 0;

                // Write session header
                var sessionInfo = new[]
                {
                    $"=== CLIF Automation Session ===",
                    $"Session ID: {this.currentSessionId}",
                    $"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"Session Path: {this.currentSessionPath}",
                    $"Target Window: {(targetWindow?.Name ?? "Full Screen")}",
                    "=" + new string('=', 50),
                    string.Empty,
                };

                File.WriteAllLines(this.logFilePath, sessionInfo);

                this.logger.LogInformation($"📁 Started capture session: {this.currentSessionId}");
                this.logger.LogInformation($"📂 Session path: {this.currentSessionPath}");

                return this.currentSessionId;
            }
        });
    }

    /// <inheritdoc />
    public async Task CaptureAfterInteractionAsync(string actionType, string elementInfo, bool success, string? validationResult = null)
    {
        if (this.currentSessionPath == null || this.logFilePath == null)
        {
            this.logger.LogWarning("No active session for capturing");
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                lock (this.lockObject)
                {
                    this.captureCounter++;
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var screenshotName = $"{this.captureCounter:D3}_{actionType}_{timestamp.Replace(":", string.Empty)}.png";
                    var screenshotPath = Path.Combine(this.currentSessionPath, "screenshots", screenshotName);

                    // Capture screenshot
                    this.CaptureScreenshot(screenshotPath);

                    // Log the interaction
                    var logEntry = new[]
                    {
                        $"[{DateTime.Now:HH:mm:ss.fff}] Step {this.captureCounter}: {actionType}",
                        $"  Element: {elementInfo}",
                        $"  Success: {success}",
                        $"  Validation: {validationResult ?? "N/A"}",
                        $"  Screenshot: {screenshotName}",
                        string.Empty,
                    };

                    File.AppendAllLines(this.logFilePath, logEntry);

                    this.logger.LogInformation($"📸 Captured step {this.captureCounter}: {actionType} → {screenshotName}");
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Failed to capture interaction: {actionType}");
            }
        });
    }

    /// <inheritdoc />
    public async Task LogInteractionAsync(string message, LogLevel level = LogLevel.Information)
    {
        if (this.logFilePath == null)
        {
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                var logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {level}: {message}";
                File.AppendAllText(this.logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to write to session log");
            }
        });
    }

    /// <inheritdoc />
    public void SetTargetWindow(AutomationElement? targetWindow)
    {
        this.targetWindow = targetWindow;
    }

    /// <inheritdoc />
    public async Task EndSessionAsync()
    {
        if (this.currentSessionPath == null || this.logFilePath == null)
        {
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                lock (this.lockObject)
                {
                    // Write session footer
                    var sessionFooter = new[]
                    {
                        string.Empty,
                        "=" + new string('=', 50),
                        $"Session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        $"Total captures: {this.captureCounter}",
                        $"Screenshots saved in: {Path.Combine(this.currentSessionPath, "screenshots")}",
                        "=== End of Session ===",
                    };

                    File.AppendAllLines(this.logFilePath, sessionFooter);

                    this.logger.LogInformation($"📋 Session completed: {this.currentSessionId}");
                    this.logger.LogInformation($"📊 Total captures: {this.captureCounter}");
                    this.logger.LogInformation($"📁 Session saved: {this.currentSessionPath}");

                    // Reset session state
                    this.currentSessionId = null;
                    this.currentSessionPath = null;
                    this.logFilePath = null;
                    this.captureCounter = 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to end session properly");
            }
        });
    }

    private void CaptureScreenshot(string filePath)
    {
        try
        {
            // If we have a target window, bring it to foreground and capture it specifically
            if (this.targetWindow != null)
            {
                this.logger.LogDebug("Bringing target window to foreground for screenshot");
                this.BringWindowToForeground(this.targetWindow);
                Thread.Sleep(500); // Longer delay to ensure window is in focus and UI changes are rendered

                this.logger.LogDebug($"Capturing window: {this.targetWindow.Name} (Size: {this.targetWindow.BoundingRectangle})");

                // Capture the specific window
                using var capture = FlaUI.Core.Capturing.Capture.Element(this.targetWindow);
                using var bitmap = capture.Bitmap;
                bitmap.Save(filePath, ImageFormat.Png);

                this.logger.LogDebug($"Screenshot saved: {filePath} (Size: {bitmap.Width}x{bitmap.Height})");
            }
            else
            {
                // Fallback to full screen capture
                using var capture = FlaUI.Core.Capturing.Capture.Screen();
                using var bitmap = capture.Bitmap;
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning($"Failed to capture screenshot using FlaUI, trying fallback: {ex.Message}");

            try
            {
                // Fallback to basic screen capture
                this.CaptureScreenshotFallback(filePath);
            }
            catch (Exception fallbackEx)
            {
                this.logger.LogError(fallbackEx, "All screenshot capture methods failed");
            }
        }
    }

    private void BringWindowToForeground(AutomationElement window)
    {
        try
        {
            var windowHandle = new IntPtr(window.Properties.NativeWindowHandle);

            // Restore window if minimized
            ShowWindow(windowHandle, SW_RESTORE);

            // Bring to foreground
            SetForegroundWindow(windowHandle);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning($"Failed to bring window to foreground: {ex.Message}");
        }
    }

    private void CaptureScreenshotFallback(string filePath)
    {
        var screen = Screen.PrimaryScreen
            ?? Screen.AllScreens.FirstOrDefault()
            ?? throw new InvalidOperationException("No display screen is available for fallback screenshot capture.");
        var bounds = screen.Bounds;
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
        bitmap.Save(filePath, ImageFormat.Png);
    }

    private string FindWorkspaceRoot()
    {
        // Start from current directory and walk up to find workspace root
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);

        while (dir != null)
        {
            // Look for indicators of workspace root
            if (dir.GetFiles("*.sln").Any() ||
                dir.GetDirectories("CLIF").Any() ||
                dir.GetDirectories("TestWpfApp").Any())
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        // Fallback to current directory
        return currentDir;
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
