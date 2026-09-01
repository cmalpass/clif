// Licensed under the MIT License.

using System.Drawing.Imaging;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using CLIF.Mcp.Core;
using CLIF.Mcp.Security;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to capture screenshots and return them as base64-encoded images.
/// </summary>
public class ScreenshotTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;
    private readonly McpSafetyPolicy _safetyPolicy;

    /// <summary>
    /// Initializes the screenshot tool.
    /// </summary>
    /// <param name="sessionManager">Manager used to resolve target windows.</param>
    /// <param name="elementRegistry">Registry used to resolve element references.</param>
    /// <param name="safetyPolicy">Optional policy restricting screenshot operations.</param>
    public ScreenshotTool(
        WindowSessionManager sessionManager,
        ElementRegistry elementRegistry,
        McpSafetyPolicy? safetyPolicy = null)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
        _safetyPolicy = safetyPolicy ?? McpSafetyPolicy.FromEnvironment();
    }

    /// <inheritdoc />
    public override string Name => "clif_screenshot";

    /// <inheritdoc />
    public override string Description =>
        "Capture a screenshot and return it as a base64-encoded PNG image. " +
        "Captures a specific element or registered window. Full-screen capture requires explicit host policy.";

    /// <inheritdoc />
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

    /// <inheritdoc />
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
                if (!_safetyPolicy.AllowFullScreenCapture)
                {
                    return Task.FromResult(ErrorResult(
                        "Full-screen capture is disabled by policy. Set CLIF_MCP_ALLOW_FULL_SCREEN_CAPTURE=true to enable it."));
                }

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
                return Task.FromResult(ErrorResult(
                    "Provide ref or handle. CLIF does not capture the focused window or desktop implicitly."));
            }

            return ReturnImage(capture);
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(handle))
            {
                _sessionManager.InvalidateWindow(handle);
                _elementRegistry.RemoveWindow(handle);
            }

            return Task.FromResult(ErrorResult("Failed to capture screenshot."));
        }
    }

    private static Task<McpToolResult> ReturnImage(FlaUI.Core.Capturing.CaptureImage capture)
    {
        using (capture)
        {
            using var stream = new MemoryStream();
            capture.Bitmap.Save(stream, ImageFormat.Png);
            if (stream.Length > McpSafetyPolicy.MaximumScreenshotBytes)
            {
                return Task.FromResult(ErrorResult(
                    $"Screenshot exceeds the {McpSafetyPolicy.MaximumScreenshotBytes / (1024 * 1024)} MiB policy limit."));
            }

            return Task.FromResult(ImageResult(stream.ToArray()));
        }
    }
}
