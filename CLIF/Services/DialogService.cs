using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using CLIF.Core;

namespace CLIF.Services;

public interface IDialogService
{
    Task HandleModalDialogsAsync(UIA3Automation? automation = null);
}

public class DialogService : IDialogService
{
    private readonly ILogger<DialogService> _logger;

    // Windows API for dialog handling
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

    private const uint WM_KEYDOWN = 0x0100;
    private const int VK_RETURN = 0x0D;
    private const int VK_ESCAPE = 0x1B;
    private const uint BM_CLICK = 0x00F5;

    public DialogService(ILogger<DialogService> logger)
    {
        _logger = logger;
    }

    public async Task HandleModalDialogsAsync(UIA3Automation? automation = null)
    {
        await Task.Run(async () =>
        {
            try
            {
                // Try FlaUI managed approach first
                if (automation != null)
                {
                    var desktop = automation.GetDesktop();
                    var dialogs = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                        .Where(w => !w.IsOffscreen)
                        .ToArray();

                    foreach (var dialog in dialogs)
                    {
                        try
                        {
                            // Check if this window might be a dialog (has certain characteristics)
                            if (dialog.Name.Contains("Information") || dialog.Name.Contains("Button Click") ||
                                dialog.Name.Contains("Warning") || dialog.Name.Contains("Error"))
                            {
                                _logger.LogInformation($"Found FlaUI modal dialog: {dialog.Name}");

                                // Try to bring dialog to foreground using managed code
                                try
                                {
                                    var window = dialog.AsWindow();
                                    window?.SetForeground();
                                }
                                catch
                                {
                                    // Fallback to Win32 if managed approach fails
                                    SetForegroundWindow(dialog.Properties.NativeWindowHandle);
                                }

                                // Look for OK, Yes, or Close buttons
                                var buttons = dialog.FindAllChildren(cf => cf.ByControlType(ControlType.Button));
                                var dismissButton = buttons.FirstOrDefault(b =>
                                    b.Name?.ToLower().Contains("ok") == true ||
                                    b.Name?.ToLower().Contains("yes") == true ||
                                    b.Name?.ToLower().Contains("close") == true);

                                if (dismissButton != null)
                                {
                                    dismissButton.Click();
                                    _logger.LogInformation($"Clicked '{dismissButton.Name}' button to dismiss dialog");
                                    await Task.Delay(AutomationConstants.ValidationDelayMs);
                                    return; // Successfully handled
                                }
                                else
                                {
                                    // Send Escape to close dialog using managed keyboard
                                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
                                    _logger.LogInformation("Sent Escape key to dismiss dialog");
                                    await Task.Delay(AutomationConstants.ValidationDelayMs);
                                    return; // Successfully handled
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug($"Error handling FlaUI dialog: {ex.Message}");
                        }
                    }
                }

                // Fallback to Win32 API for native dialogs not in UI Automation tree
                var dialogPatterns = new[]
                {
                    new { ClassName = "#32770", Title = (string?)null }, // Standard Windows dialog
                    new { ClassName = (string?)null, Title = "Button Click" },
                    new { ClassName = (string?)null, Title = "Information" },
                    new { ClassName = (string?)null, Title = "Warning" },
                    new { ClassName = (string?)null, Title = "Error" },
                    new { ClassName = (string?)null, Title = "Confirm" }
                };

                foreach (var pattern in dialogPatterns)
                {
                    IntPtr dialogHandle = FindWindow(pattern.ClassName, pattern.Title);
                    if (dialogHandle != IntPtr.Zero)
                    {
                        _logger.LogInformation($"Found native Win32 dialog: {pattern.ClassName ?? "Unknown"} - {pattern.Title ?? "Unknown title"}");

                        // Bring dialog to foreground
                        SetForegroundWindow(dialogHandle);
                        Thread.Sleep(AutomationConstants.ShortDelayMs);

                        // Try to find and click OK button first
                        IntPtr okButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "OK");
                        if (okButton != IntPtr.Zero)
                        {
                            PostMessage(okButton, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
                            _logger.LogInformation("Clicked OK button on native dialog");
                        }
                        else
                        {
                            // Fallback: Send Enter key to dismiss dialog
                            PostMessage(dialogHandle, WM_KEYDOWN, new IntPtr(VK_RETURN), IntPtr.Zero);
                            _logger.LogInformation("Sent Enter key to dismiss native dialog");
                        }

                        Thread.Sleep(AutomationConstants.ValidationDelayMs);
                        break; // Handle one dialog at a time
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error in dialog handling: {ex.Message}");
            }
        });
    }
}
