// Licensed under the MIT License.

using System.Text.Json;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to validate inline CLIF JSON automation script content.
/// </summary>
public class ScriptTool : ToolBase
{
    private static readonly HashSet<string> SupportedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "log", "wait", "screenshot", "clear", "type", "click", "focus", "select",
        "setvalue", "selecttab", "selectrow", "selectcell", "expand", "collapse",
        "getvalue", "getselection", "getstate", "validate",
    };

    private static readonly HashSet<string> ActionsWithoutElement = new(StringComparer.OrdinalIgnoreCase)
    {
        "log", "wait", "screenshot",
    };

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
                minLength = 1,
                description = "Inline JSON script content (alternative to path)",
            },
        },
        required = new[] { "content" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var content = GetStringArgument(arguments, "content");

        if (string.IsNullOrEmpty(content))
        {
            return Task.FromResult(ErrorResult("Provide inline script JSON in the required 'content' argument."));
        }

        try
        {
            var script = JsonSerializer.Deserialize<ScriptDefinition>(content, McpProtocol.JsonOptions);
            if (script == null)
            {
                return Task.FromResult(ErrorResult("Failed to parse script JSON"));
            }

            if (!TryValidateScript(script, out var validationError))
            {
                return Task.FromResult(ErrorResult(validationError));
            }

            return Task.FromResult(TextResult(
                $"Script \"{script.Name}\" is valid with {script.Steps?.Count ?? 0} steps. " +
                "Use clif_batch for MCP automation or the CLIF CLI to execute a script."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to process script: {ex.Message}"));
        }
    }

    private static bool TryValidateScript(ScriptDefinition script, out string error)
    {
        if (string.IsNullOrWhiteSpace(script.Name))
        {
            error = "Script name is required.";
            return false;
        }

        if (script.Steps == null)
        {
            error = "Script steps must be an array.";
            return false;
        }

        foreach (var (step, index) in script.Steps.Select((value, offset) => (value, offset + 1)))
        {
            if (string.IsNullOrWhiteSpace(step.Action))
            {
                error = $"Step {index} requires an action.";
                return false;
            }

            if (!SupportedActions.Contains(step.Action))
            {
                error = $"Step {index} uses unsupported action '{step.Action}'.";
                return false;
            }

            if (!ActionsWithoutElement.Contains(step.Action) && string.IsNullOrWhiteSpace(step.Element))
            {
                error = $"Step {index} action '{step.Action}' requires an element selector.";
                return false;
            }

            if (step.DelayMs < 0)
            {
                error = $"Step {index} delayMs must not be negative.";
                return false;
            }
        }

        error = string.Empty;
        return true;
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
    public int DelayMs { get; set; }
}
