// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to type text into an element (appends to existing content).
/// </summary>
public class TypeTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    public TypeTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    public override string Name => "clif_type";

    public override string Description =>
        "Type text into an element (appends to existing content). " +
        "If no ref is provided, types into the currently focused element. " +
        "Optionally press Enter after typing with submit=true.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref to focus before typing. If omitted, types to currently focused element.",
            },
            text = new
            {
                type = "string",
                description = "Text to type",
            },
            submit = new
            {
                type = "boolean",
                description = "Press Enter after typing (default: false)",
            },
        },
        required = new[] { "text" },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var text = GetStringArgument(arguments, "text");
        if (string.IsNullOrEmpty(text))
        {
            return Task.FromResult(ErrorResult("Missing required argument: text"));
        }

        var refId = GetStringArgument(arguments, "ref");
        var submit = GetBoolArgument(arguments, "submit");

        try
        {
            if (!string.IsNullOrEmpty(refId))
            {
                var element = _elementRegistry.GetElement(refId);
                if (element == null)
                {
                    return Task.FromResult(ErrorResult(
                        $"Element not found: {refId}. Run clif_snapshot to refresh element refs."));
                }

                element.Focus();
                Thread.Sleep(50);
            }

            Keyboard.Type(text);

            if (submit)
            {
                Keyboard.Press(VirtualKeyShort.ENTER);
                return Task.FromResult(TextResult($"Typed and submitted \"{text}\" into {refId ?? "focused element"}"));
            }

            return Task.FromResult(TextResult($"Typed \"{text}\" into {refId ?? "focused element"}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to type: {ex.Message}"));
        }
    }
}

/// <summary>
/// MCP tool to clear an element's value and fill with new content (replaces existing).
/// </summary>
public class FillTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    public FillTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    public override string Name => "clif_fill";

    public override string Description =>
        "Clear and fill an element's value (replaces existing content). " +
        "Uses the Value pattern when available, otherwise falls back to Ctrl+A and typing.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref to fill",
            },
            value = new
            {
                type = "string",
                description = "Value to set",
            },
        },
        required = new[] { "ref", "value" },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var refId = GetStringArgument(arguments, "ref");
        var value = GetStringArgument(arguments, "value");

        if (string.IsNullOrEmpty(refId) || value == null)
        {
            return Task.FromResult(ErrorResult("Missing required arguments: ref, value"));
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return Task.FromResult(ErrorResult(
                $"Element not found: {refId}. Run clif_snapshot to refresh element refs."));
        }

        try
        {
            if (element.Patterns.Value.IsSupported)
            {
                element.Patterns.Value.Pattern.SetValue(value);
                return Task.FromResult(TextResult($"Filled {refId} with \"{value}\""));
            }

            // Fallback: focus, select all, type
            element.Focus();
            Thread.Sleep(30);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            Thread.Sleep(30);
            Keyboard.Type(value);
            return Task.FromResult(TextResult($"Filled {refId} with \"{value}\""));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to fill {refId}: {ex.Message}"));
        }
    }
}
