// Licensed under the MIT License.

using System.Text.Json;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to validate inline CLIF JSON automation script content.
/// </summary>
public class ScriptTool : ToolBase
{
    /// <inheritdoc />
    public override string Name => "clif_validate_script";

    /// <inheritdoc />
    public override string Description =>
        "Validate inline CLIF JSON automation script content. This tool does not execute scripts.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            content = new
            {
                type = "string",
                description = "Inline JSON script content (alternative to path)",
            },
        },
    };

    /// <inheritdoc />
    public override async Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var content = GetStringArgument(arguments, "content");

        if (string.IsNullOrEmpty(content))
        {
            return ErrorResult("Provide inline script JSON in the required 'content' argument.");
        }

        try
        {
            var script = JsonSerializer.Deserialize<ScriptDefinition>(content, McpProtocol.JsonOptions);
            if (script == null)
            {
                return ErrorResult("Failed to parse script JSON");
            }

            return TextResult(
                $"Script \"{script.Name}\" is valid with {script.Steps?.Count ?? 0} steps. " +
                "Use clif_batch for MCP automation or the CLIF CLI to execute a script.");
        }
        catch (Exception ex)
        {
            return ErrorResult($"Failed to process script: {ex.Message}");
        }
    }
}

/// <summary>
/// Minimal script definition for MCP parsing.
/// </summary>
internal sealed class ScriptDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public List<ScriptStepDefinition>? Steps { get; set; }
}

/// <summary>
/// Minimal script step definition for MCP parsing.
/// </summary>
internal sealed class ScriptStepDefinition
{
    public string Action { get; set; } = string.Empty;
    public string Element { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
