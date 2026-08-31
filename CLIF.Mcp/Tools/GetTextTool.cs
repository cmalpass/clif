// Licensed under the MIT License.

using System.Text.Json;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to extract text content from an element.
/// </summary>
public class GetTextTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the text extraction tool.
    /// </summary>
    /// <param name="elementRegistry">Registry used to resolve element references.</param>
    public GetTextTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_get_text";

    /// <inheritdoc />
    public override string Description =>
        "Get the text content of an element. " +
        "Tries Value pattern first, then Name property, then Text pattern.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref to read text from",
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

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return Task.FromResult(ErrorResult(
                $"Element not found: {refId}. Run clif_snapshot to refresh element refs."));
        }

        try
        {
            // Try Value pattern first (for textboxes)
            if (element.Patterns.Value.IsSupported)
            {
                var value = element.Patterns.Value.Pattern.Value.ValueOrDefault;
                if (!string.IsNullOrEmpty(value))
                {
                    return Task.FromResult(TextResult(value));
                }
            }

            // Try Name property
            var name = element.Properties.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(name))
            {
                return Task.FromResult(TextResult(name));
            }

            // Try Text pattern (for documents)
            if (element.Patterns.Text.IsSupported)
            {
                var text = element.Patterns.Text.Pattern.DocumentRange.GetText(-1);
                if (!string.IsNullOrEmpty(text))
                {
                    return Task.FromResult(TextResult(text));
                }
            }

            return Task.FromResult(TextResult(string.Empty));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to get text from {refId}: {ex.Message}"));
        }
    }
}
