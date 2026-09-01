// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using System.Text.Json;
using System.Text;
using CLIF.Mcp.Security;

namespace CLIF.Mcp;

/// <summary>
/// Registry for MCP tools - maps tool names to handlers.
/// </summary>
public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new();
    private readonly McpSafetyPolicy _safetyPolicy;

    /// <summary>Initializes a registry using the supplied immutable session policy.</summary>
    public ToolRegistry(McpSafetyPolicy? safetyPolicy = null)
    {
        _safetyPolicy = safetyPolicy ?? McpSafetyPolicy.FromEnvironment();
    }

    /// <summary>
    /// Register an MCP tool implementation.
    /// </summary>
    public void RegisterTool(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    /// <summary>
    /// Return definitions for all registered tools in ordinal name order.
    /// </summary>
    public List<McpTool> GetToolDefinitions()
    {
        return _tools.Values
            .OrderBy(tool => tool.Name, StringComparer.Ordinal)
            .Select(tool => tool.GetDefinition())
            .ToList();
    }

    /// <summary>
    /// Execute a tool by name with the given arguments.
    /// </summary>
    public async Task<McpToolResult> ExecuteToolAsync(
        string name,
        JsonElement? arguments,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_tools.TryGetValue(name, out var tool))
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"Unknown tool: {name}" },
                },
                IsError = true,
            };
        }

        if (!ValidateArguments(arguments, out var validationError))
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"MCP_INVALID_PARAMS: {validationError}" },
                },
                IsError = true,
            };
        }

        if (!_safetyPolicy.IsCapabilityAllowed(tool.RequiredCapability))
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"MCP_PERMISSION_DENIED: capability '{tool.RequiredCapability}' is disabled by policy." },
                },
                IsError = true,
            };
        }

        try
        {
            return await tool.ExecuteAsync(arguments, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new McpToolResult
            {
                Content = new List<McpContent>
                {
                    new() { Type = "text", Text = $"Error: {ex.Message}" },
                },
                IsError = true,
            };
        }
    }

    private static bool ValidateArguments(JsonElement? arguments, out string error)
    {
        error = string.Empty;
        if (!arguments.HasValue)
        {
            return true;
        }

        if (arguments.Value.ValueKind != JsonValueKind.Object)
        {
            error = "tool arguments must be a JSON object";
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetByteCount(arguments.Value.GetRawText());
        if (payloadBytes > McpSafetyPolicy.MaximumArgumentPayloadBytes)
        {
            error = $"argument payload exceeds {McpSafetyPolicy.MaximumArgumentPayloadBytes} bytes";
            return false;
        }

        return ValidateValue(arguments.Value, out error);
    }

    private static bool ValidateValue(JsonElement value, out string error)
    {
        error = string.Empty;
        if (value.ValueKind == JsonValueKind.String &&
            value.GetString()?.Length > McpSafetyPolicy.MaximumArgumentStringLength)
        {
            error = $"string arguments may not exceed {McpSafetyPolicy.MaximumArgumentStringLength} characters";
            return false;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in value.EnumerateArray())
            {
                if (!ValidateValue(child, out error)) return false;
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var child in value.EnumerateObject())
            {
                if (!ValidateValue(child.Value, out error)) return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Interface for MCP tools.
/// </summary>
public interface ITool
{
    /// <summary>Gets the capability required before this tool may execute.</summary>
    McpCapability RequiredCapability => McpCapability.ReadOnly;
    /// <summary>
    /// Unique tool name (e.g. "clif_click").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Return the MCP tool definition.
    /// </summary>
    McpTool GetDefinition();

    /// <summary>
    /// Execute the tool with the provided JSON arguments.
    /// </summary>
    Task<McpToolResult> ExecuteAsync(JsonElement? arguments);

    /// <summary>
    /// Execute the tool with the provided JSON arguments and cancellation token.
    /// Implementations that do not need cooperative cancellation retain the
    /// original argument-only execution behavior by default.
    /// </summary>
    Task<McpToolResult> ExecuteAsync(JsonElement? arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ExecuteAsync(arguments);
    }
}

/// <summary>
/// Base class for MCP tools with common utilities.
/// </summary>
public abstract class ToolBase : ITool
{
    /// <inheritdoc />
    public virtual McpCapability RequiredCapability => McpCapability.ReadOnly;
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <summary>
    /// Human-readable description shown to MCP clients.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// JSON Schema describing the tool's input parameters.
    /// </summary>
    public abstract object InputSchema { get; }

    /// <inheritdoc />
    public McpTool GetDefinition() => new()
    {
        Name = Name,
        Description = Description,
        InputSchema = InputSchema,
    };

    /// <inheritdoc />
    public abstract Task<McpToolResult> ExecuteAsync(JsonElement? arguments);

    /// <inheritdoc />
    public virtual Task<McpToolResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ExecuteAsync(arguments);
    }

    /// <summary>
    /// Helper to create a text result.
    /// </summary>
    protected static McpToolResult TextResult(string text) => new()
    {
        Content = new List<McpContent>
        {
            new() { Type = "text", Text = text },
        },
    };

    /// <summary>
    /// Helper to create an error result.
    /// </summary>
    protected static McpToolResult ErrorResult(string message) => new()
    {
        Content = new List<McpContent>
        {
            new() { Type = "text", Text = message },
        },
        IsError = true,
    };

    /// <summary>
    /// Helper to create an image result.
    /// </summary>
    protected static McpToolResult ImageResult(byte[] imageData, string mimeType = "image/png") => new()
    {
        Content = new List<McpContent>
        {
            new()
            {
                Type = "image",
                Data = Convert.ToBase64String(imageData),
                MimeType = mimeType,
            },
        },
    };

    /// <summary>
    /// Extract a typed argument from the JSON arguments.
    /// </summary>
    protected T? GetArgument<T>(JsonElement? arguments, string name)
    {
        if (arguments == null) return default;
        if (!arguments.Value.TryGetProperty(name, out var prop)) return default;
        return JsonSerializer.Deserialize<T>(prop.GetRawText(), McpProtocol.JsonOptions);
    }

    /// <summary>
    /// Extract a string argument.
    /// </summary>
    protected string? GetStringArgument(JsonElement? arguments, string name)
    {
        if (arguments == null) return null;
        if (!arguments.Value.TryGetProperty(name, out var prop)) return null;
        return prop.GetString();
    }

    /// <summary>
    /// Extract a boolean argument with a default value.
    /// </summary>
    protected bool GetBoolArgument(JsonElement? arguments, string name, bool defaultValue = false)
    {
        if (arguments == null) return defaultValue;
        if (!arguments.Value.TryGetProperty(name, out var prop)) return defaultValue;
        return prop.GetBoolean();
    }

    /// <summary>
    /// Extract an integer argument with a default value.
    /// </summary>
    protected int GetIntArgument(JsonElement? arguments, string name, int defaultValue = 0)
    {
        if (arguments == null) return defaultValue;
        if (!arguments.Value.TryGetProperty(name, out var prop)) return defaultValue;
        return prop.GetInt32();
    }

    /// <summary>
    /// Extract a double argument with a default value.
    /// </summary>
    protected double GetDoubleArgument(JsonElement? arguments, string name, double defaultValue = 0)
    {
        if (arguments == null) return defaultValue;
        if (!arguments.Value.TryGetProperty(name, out var prop)) return defaultValue;
        return prop.GetDouble();
    }
}
