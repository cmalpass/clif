// Licensed under the MIT License.

using System.Drawing.Imaging;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to capture screenshots and return them as base64-encoded images.
/// </summary>
public class ScreenshotTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    public ScreenshotTool(WindowSessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    public override string Name => "clif_screenshot";

    public override string Description =>
        "Capture a screenshot and return it as a base64-encoded PNG image. " +
        "Can capture a specific element, a window, or the full screen.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            handle = new
            {
                type = "string",
                description = "Window handle to capture",
            },
            @ref = new
            {
                type = "string",
                description = "Element ref to capture (captures just that element)",
            },
            fullScreen = new
            {
                type = "boolean",
                description = "Capture the entire screen (default: false)",
            },
        },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var refId = GetStringArgument(arguments, "ref");
        var fullScreen = GetBoolArgument(arguments, "fullScreen");

        try
        {
            FlaUI.Core.Capturing.CaptureImage capture;

            if (fullScreen)
            {
                capture = FlaUI.Core.Capturing.Capture.Screen();
            }
            else if (!string.IsNullOrEmpty(refId))
            {
                var element = _elementRegistry.GetElement(refId);
                if (element == null)
                {
                    return Task.FromResult(ErrorResult(
                        $"Element not found: {refId}. Run clif_snapshot to refresh element refs."));
                }

                capture = FlaUI.Core.Capturing.Capture.Element(element);
            }
            else if (!string.IsNullOrEmpty(handle))
            {
                var window = _sessionManager.GetWindow(handle);
                if (window == null)
                {
                    return Task.FromResult(ErrorResult($"Window not found: {handle}"));
                }

                capture = FlaUI.Core.Capturing.Capture.Element(window);
            }
            else
            {
                // Capture focused window
                var focusedElement = _sessionManager.Automation.FocusedElement();
                if (focusedElement != null)
                {
                    var current = focusedElement;
                    while (current != null)
                    {
                        if (current.Properties.ControlType.ValueOrDefault == ControlType.Window)
                        {
                            capture = FlaUI.Core.Capturing.Capture.Element(current);
                            return ReturnImage(capture);
                        }

                        current = current.Parent;
                    }
                }

                capture = FlaUI.Core.Capturing.Capture.Screen();
            }

            return ReturnImage(capture);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture screenshot: {ex.Message}"));
        }
    }

    private static Task<McpToolResult> ReturnImage(FlaUI.Core.Capturing.CaptureImage capture)
    {
        using (capture)
        {
            using var stream = new MemoryStream();
            capture.Bitmap.Save(stream, ImageFormat.Png);
            return Task.FromResult(ImageResult(stream.ToArray()));
        }
    }
}
