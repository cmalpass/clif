// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using CLIF.Mcp.Core;
using CLIF.Mcp.Security;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to type text into an element (appends to existing content).
/// </summary>
public class TypeTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the text-entry tool.
    /// </summary>
    /// <param name="elementRegistry">Registry used to resolve element references.</param>
    public TypeTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_type";

    /// <inheritdoc />
    public override McpCapability RequiredCapability => McpCapability.Input;

    /// <inheritdoc />
    public override string Description =>
        "Type text into a snapshotted element (appends to existing content). " +
        "A ref is required so CLIF never types into an implicit focused element. " +
        "Optionally press Enter after typing with submit=true.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref to focus before typing.",
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
        required = new[] { "ref", "text" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments) => ExecuteCoreAsync(arguments, CancellationToken.None);

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments, CancellationToken cancellationToken) =>
        ExecuteCoreAsync(arguments, cancellationToken);

    private async Task<McpToolResult> ExecuteCoreAsync(JsonElement? arguments, CancellationToken cancellationToken)
    {
        var text = GetStringArgument(arguments, "text");
        if (string.IsNullOrEmpty(text))
        {
            return ErrorResult("Missing required argument: text");
        }

        var refId = GetStringArgument(arguments, "ref");
        var submit = GetBoolArgument(arguments, "submit");

        if (string.IsNullOrEmpty(refId))
        {
            return ErrorResult("Missing required argument: ref");
        }

        try
        {
            var element = _elementRegistry.GetElement(refId);
            if (element == null)
            {
                return ErrorResult($"Element not found: {refId}. Run clif_snapshot to refresh element refs.");
            }

            // Value controls (including WPF TextBox) do not guarantee that keyboard
            // input begins at the end of their current content. Set the combined value
            // directly so the documented append operation is deterministic.
            if (element.Patterns.Value.IsSupported)
            {
                var existing = element.Patterns.Value.Pattern.Value.ValueOrDefault ?? string.Empty;
                element.Patterns.Value.Pattern.SetValue(existing + text);
            }
            else
            {
                element.Focus();
                await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(50), cancellationToken);
                Keyboard.Type(text);
            }

            if (submit)
            {
                element.Focus();
                Keyboard.Press(VirtualKeyShort.ENTER);
                return TextResult($"Typed and submitted text into {refId}");
            }

            return TextResult($"Typed text into {refId}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to type: {ex.Message}");
        }
    }
}

/// <summary>
/// MCP tool to clear an element's value and fill with new content (replaces existing).
/// </summary>
public class FillTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the field-fill tool.
    /// </summary>
    /// <param name="elementRegistry">Registry used to resolve element references.</param>
    public FillTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_fill";

    /// <inheritdoc />
    public override McpCapability RequiredCapability => McpCapability.Input;

    /// <inheritdoc />
    public override string Description =>
        "Clear and fill an element's value (replaces existing content). " +
        "Uses the Value pattern when available, otherwise falls back to Ctrl+A and typing.";

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments) => ExecuteCoreAsync(arguments, CancellationToken.None);

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments, CancellationToken cancellationToken) =>
        ExecuteCoreAsync(arguments, cancellationToken);

    private async Task<McpToolResult> ExecuteCoreAsync(JsonElement? arguments, CancellationToken cancellationToken)
    {
        var refId = GetStringArgument(arguments, "ref");
        var value = GetStringArgument(arguments, "value");

        if (string.IsNullOrEmpty(refId) || value == null)
        {
            return ErrorResult("Missing required arguments: ref, value");
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return ErrorResult($"Element not found: {refId}. Run clif_snapshot to refresh element refs.");
        }

        try
        {
            if (element.Patterns.Value.IsSupported)
            {
                element.Patterns.Value.Pattern.SetValue(value);
                return TextResult($"Filled {refId} with \"{value}\"");
            }

            // Fallback: focus, select all, type
            element.Focus();
            await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(30), cancellationToken);
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
            await UiDispatcher.DelayAsync(TimeSpan.FromMilliseconds(30), cancellationToken);
            Keyboard.Type(value);
            return TextResult($"Filled {refId} with \"{value}\"");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to fill {refId}: {ex.Message}");
        }
    }
}
