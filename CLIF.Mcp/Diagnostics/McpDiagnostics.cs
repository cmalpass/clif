// Licensed under the MIT License.

using System.Text.Json;

namespace CLIF.Mcp.Diagnostics;

/// <summary>
/// Emits structured, redacted diagnostics to stderr without writing to MCP stdout.
/// </summary>
public sealed class McpDiagnostics
{
    private readonly TextWriter _writer;
    private readonly object _sync = new();
    private readonly bool _enabled;

    /// <summary>Initializes diagnostics with an optional stderr writer.</summary>
    public McpDiagnostics(TextWriter? writer = null, bool? enabled = null)
    {
        _writer = writer ?? Console.Error;
        _enabled = enabled ?? !string.Equals(
            Environment.GetEnvironmentVariable("CLIF_MCP_LOG_LEVEL"),
            "off",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Gets a value indicating whether diagnostics are enabled.</summary>
    public bool Enabled => _enabled;

    /// <summary>Writes one structured event with sanitized metadata.</summary>
    public void Log(string eventName, string? correlationId = null, IReadOnlyDictionary<string, object?>? fields = null)
    {
        if (!_enabled)
        {
            return;
        }

        var payload = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["event"] = eventName,
            ["correlationId"] = correlationId,
        };

        if (fields != null)
        {
            foreach (var field in fields)
            {
                payload[field.Key] = field.Value;
            }
        }

        var line = JsonSerializer.Serialize(payload);
        lock (_sync)
        {
            _writer.WriteLine(line);
            _writer.Flush();
        }
    }
}
