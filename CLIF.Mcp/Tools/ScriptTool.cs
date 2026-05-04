// Licensed under the MIT License.

using System.Text.Json;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to execute a CLIF JSON automation script.
/// </summary>
public class ScriptTool : ToolBase
{
    public override string Name => "clif_run_script";

    public override string Description =>
        "Execute a CLIF JSON automation script. The script defines a sequence of " +
        "automation steps (click, type, select, etc.) with target process and options. " +
        "Provide either a file path or inline JSON content.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            path = new
            {
                type = "string",
                description = "Path to a CLIF JSON script file",
            },
            content = new
            {
                type = "string",
                description = "Inline JSON script content (alternative to path)",
            },
        },
    };

    public override async Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var path = GetStringArgument(arguments, "path");
        var content = GetStringArgument(arguments, "content");

        if (string.IsNullOrEmpty(path) && string.IsNullOrEmpty(content))
        {
            return ErrorResult("Provide either 'path' to a script file or 'content' with inline JSON");
        }

        try
        {
            string jsonContent;
            if (!string.IsNullOrEmpty(path))
            {
                if (!File.Exists(path))
                {
                    return ErrorResult($"Script file not found: {path}");
                }

                jsonContent = await File.ReadAllTextAsync(path);
            }
            else
            {
                jsonContent = content!;
            }

            // Parse and validate the script
            var script = JsonSerializer.Deserialize<ScriptDefinition>(jsonContent, McpProtocol.JsonOptions);
            if (script == null)
            {
                return ErrorResult("Failed to parse script JSON");
            }

            return TextResult(
                $"Script \"{script.Name}\" loaded with {script.Steps?.Count ?? 0} steps.\n" +
                "Note: Full script execution requires the CLIF CLI. " +
                "Use 'clif script <path>' to execute, or use clif_batch for multi-step automation via MCP.");
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
