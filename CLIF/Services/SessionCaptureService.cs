using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using System.Runtime.InteropServices;

namespace CLIF.Services;

public interface ISessionCaptureService
{
    Task<string> StartSessionAsync(string? sessionName = null, AutomationElement? targetWindow = null);
    Task CaptureAfterInteractionAsync(string actionType, string elementInfo, bool success, string? validationResult = null);
    Task LogInteractionAsync(string message, LogLevel level = LogLevel.Information);
    Task EndSessionAsync();
    void SetTargetWindow(AutomationElement? targetWindow);
    string? CurrentSessionId { get; }
    string? CurrentSessionPath { get; }
}

public class SessionCaptureService : ISessionCaptureService
{
    // Windows API for bringing window to foreground
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    private const int SW_RESTORE = 9;
    
    private readonly ILogger<SessionCaptureService> _logger;
    private string? _currentSessionId;
    private string? _currentSessionPath;
    private string? _logFilePath;
    private int _captureCounter = 0;
    private readonly object _lockObject = new();
    private AutomationElement? _targetWindow;

    public string? CurrentSessionId => _currentSessionId;
    public string? CurrentSessionPath => _currentSessionPath;

    public SessionCaptureService(ILogger<SessionCaptureService> logger)
    {
        _logger = logger;
    }

    public async Task<string> StartSessionAsync(string? sessionName = null, AutomationElement? targetWindow = null)
    {
        return await Task.Run(() =>
        {
            lock (_lockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentSessionId = sessionName ?? $"CLIF_Session_{timestamp}";
                _targetWindow = targetWindow;
                
                // Create session directory in the workspace root
                var workspaceRoot = FindWorkspaceRoot();
                var sessionsDir = Path.Combine(workspaceRoot, "sessions");
                _currentSessionPath = Path.Combine(sessionsDir, _currentSessionId);
                
                Directory.CreateDirectory(_currentSessionPath);
                Directory.CreateDirectory(Path.Combine(_currentSessionPath, "screenshots"));
                
                // Create log file
                _logFilePath = Path.Combine(_currentSessionPath, "session.log");
                _captureCounter = 0;
                
                // Write session header
                var sessionInfo = new[]
                {
                    $"=== CLIF Automation Session ===",
                    $"Session ID: {_currentSessionId}",
                    $"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"Session Path: {_currentSessionPath}",
                    $"Target Window: {(targetWindow?.Name ?? "Full Screen")}",
                    "=" + new string('=', 50),
                    ""
                };
                
                File.WriteAllLines(_logFilePath, sessionInfo);
                
                _logger.LogInformation($"📁 Started capture session: {_currentSessionId}");
                _logger.LogInformation($"📂 Session path: {_currentSessionPath}");
                
                return _currentSessionId;
            }
        });
    }

    public async Task CaptureAfterInteractionAsync(string actionType, string elementInfo, bool success, string? validationResult = null)
    {
        if (_currentSessionPath == null || _logFilePath == null)
        {
            _logger.LogWarning("No active session for capturing");
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                lock (_lockObject)
                {
                    _captureCounter++;
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var screenshotName = $"{_captureCounter:D3}_{actionType}_{timestamp.Replace(":", "")}.png";
                    var screenshotPath = Path.Combine(_currentSessionPath, "screenshots", screenshotName);
                    
                    // Capture screenshot
                    CaptureScreenshot(screenshotPath);
                    
                    // Log the interaction
                    var logEntry = new[]
                    {
                        $"[{DateTime.Now:HH:mm:ss.fff}] Step {_captureCounter}: {actionType}",
                        $"  Element: {elementInfo}",
                        $"  Success: {success}",
                        $"  Validation: {validationResult ?? "N/A"}",
                        $"  Screenshot: {screenshotName}",
                        ""
                    };
                    
                    File.AppendAllLines(_logFilePath, logEntry);
                    
                    _logger.LogInformation($"📸 Captured step {_captureCounter}: {actionType} → {screenshotName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to capture interaction: {actionType}");
            }
        });
    }

    public async Task LogInteractionAsync(string message, LogLevel level = LogLevel.Information)
    {
        if (_logFilePath == null) return;

        await Task.Run(() =>
        {
            try
            {
                var logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {level}: {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to session log");
            }
        });
    }

    public void SetTargetWindow(AutomationElement? targetWindow)
    {
        _targetWindow = targetWindow;
    }

    public async Task EndSessionAsync()
    {
        if (_currentSessionPath == null || _logFilePath == null) return;

        await Task.Run(() =>
        {
            try
            {
                lock (_lockObject)
                {
                    // Write session footer
                    var sessionFooter = new[]
                    {
                        "",
                        "=" + new string('=', 50),
                        $"Session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                        $"Total captures: {_captureCounter}",
                        $"Screenshots saved in: {Path.Combine(_currentSessionPath, "screenshots")}",
                        "=== End of Session ==="
                    };
                    
                    File.AppendAllLines(_logFilePath, sessionFooter);
                    
                    _logger.LogInformation($"📋 Session completed: {_currentSessionId}");
                    _logger.LogInformation($"📊 Total captures: {_captureCounter}");
                    _logger.LogInformation($"📁 Session saved: {_currentSessionPath}");
                    
                    // Reset session state
                    _currentSessionId = null;
                    _currentSessionPath = null;
                    _logFilePath = null;
                    _captureCounter = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to end session properly");
            }
        });
    }

    private void CaptureScreenshot(string filePath)
    {
        try
        {
            // If we have a target window, bring it to foreground and capture it specifically
            if (_targetWindow != null)
            {
                _logger.LogDebug("Bringing target window to foreground for screenshot");
                BringWindowToForeground(_targetWindow);
                Thread.Sleep(500); // Longer delay to ensure window is in focus and UI changes are rendered
                
                _logger.LogDebug($"Capturing window: {_targetWindow.Name} (Size: {_targetWindow.BoundingRectangle})");
                
                // Capture the specific window
                using var capture = FlaUI.Core.Capturing.Capture.Element(_targetWindow);
                using var bitmap = capture.Bitmap;
                bitmap.Save(filePath, ImageFormat.Png);
                
                _logger.LogDebug($"Screenshot saved: {filePath} (Size: {bitmap.Width}x{bitmap.Height})");
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
            _logger.LogWarning($"Failed to capture screenshot using FlaUI, trying fallback: {ex.Message}");
            
            try
            {
                // Fallback to basic screen capture
                CaptureScreenshotFallback(filePath);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "All screenshot capture methods failed");
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
            _logger.LogWarning($"Failed to bring window to foreground: {ex.Message}");
        }
    }
    
    private void CaptureScreenshotFallback(string filePath)
    {
        var bounds = Screen.PrimaryScreen.Bounds;
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