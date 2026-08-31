// Licensed under the MIT License.

using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp.Diagnostics;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>Verifies structured diagnostics are emitted safely and deterministically.</summary>
public sealed class McpDiagnosticsTests
{
    [Fact]
    public void Log_WritesStructuredEventToTheProvidedWriter()
    {
        using var writer = new StringWriter();
        var diagnostics = new McpDiagnostics(writer, enabled: true);

        diagnostics.Log("mcp.request.completed", "42", new Dictionary<string, object?>
        {
            ["method"] = "tools/call",
            ["outcome"] = "success",
        });

        using var document = JsonDocument.Parse(writer.ToString());
        document.RootElement.GetProperty("event").GetString().Should().Be("mcp.request.completed");
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("42");
        document.RootElement.GetProperty("method").GetString().Should().Be("tools/call");
    }

    [Fact]
    public void DisabledDiagnostics_DoNotWriteAnything()
    {
        using var writer = new StringWriter();
        var diagnostics = new McpDiagnostics(writer, enabled: false);

        diagnostics.Log("mcp.request.received");

        writer.ToString().Should().BeEmpty();
    }
}
