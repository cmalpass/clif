// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.Input;
using CLIF.Mcp.Core;
using CLIF.Mcp.Security;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to click an element by its ref.
/// </summary>
public class ClickTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the click tool.
    /// </summary>
    /// <param name="elementRegistry">Registry used to resolve element references.</param>
    public ClickTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_click";

    /// <inheritdoc />
    public override McpCapability RequiredCapability => McpCapability.Input;

    /// <inheritdoc />
    public override string Description =>
        "Click an element by its ref (from clif_snapshot). Prefers Invoke pattern for reliability, " +
        "falls back to mouse click if needed. Supports right-click, middle-click, and double-click.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref from clif_snapshot (e.g., 'w1e5')",
            },
            button = new
            {
                type = "string",
                @enum = new[] { "left", "right", "middle" },
                description = "Mouse button to click (default: left)",
            },
            doubleClick = new
            {
                type = "boolean",
                description = "Whether to double-click (default: false)",
            },
        },
        required = new[] { "ref" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var refId = GetStringArgument(arguments, "ref");
        if (string.IsNullOrEmpty(refId))
        {
            return Task.FromResult(ErrorResult("Missing required argument: ref"));
        }

        var button = GetStringArgument(arguments, "button") ?? "left";
        var doubleClick = GetBoolArgument(arguments, "doubleClick");

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return Task.FromResult(ErrorResult(
                $"Element not found: {refId}. Run clif_snapshot to refresh element refs."));
        }

        try
        {
            var elementName = element.Properties.Name.ValueOrDefault ?? refId;

            // Try Invoke pattern first (most reliable for buttons)
            if (button == "left" && !doubleClick && element.Patterns.Invoke.IsSupported)
            {
                element.Patterns.Invoke.Pattern.Invoke();
                return Task.FromResult(TextResult($"Invoked {elementName}"));
            }

            // Try Toggle pattern for checkboxes
            if (button == "left" && !doubleClick && element.Patterns.Toggle.IsSupported)
            {
                element.Patterns.Toggle.Pattern.Toggle();
                var newState = element.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault;
                return Task.FromResult(TextResult($"Toggled {elementName} to {newState}"));
            }

            // Try SelectionItem pattern for list items
            if (button == "left" && !doubleClick && element.Patterns.SelectionItem.IsSupported)
            {
                element.Patterns.SelectionItem.Pattern.Select();
                return Task.FromResult(TextResult($"Selected {elementName}"));
            }

            // Fall back to mouse click
            var clickPoint = element.GetClickablePoint();

            var mouseButton = button switch
            {
                "right" => MouseButton.Right,
                "middle" => MouseButton.Middle,
                _ => MouseButton.Left,
            };

            if (doubleClick)
            {
                Mouse.DoubleClick(clickPoint, mouseButton);
                return Task.FromResult(TextResult($"Double-clicked {elementName}"));
            }
            else
            {
                Mouse.Click(clickPoint, mouseButton);
                return Task.FromResult(TextResult($"Clicked {elementName}"));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to click {refId}: {ex.Message}"));
        }
    }
}
