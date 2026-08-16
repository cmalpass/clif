using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Tests for the MCP ToolRegistry - tool registration, lookup, execution, and error handling.
/// </summary>
public class ToolRegistryTests
{
    [Fact]
    public void RegisterTool_AddsToolSuccessfully()
    {
        var registry = new ToolRegistry();
        var tool = new FakeToolA();

        registry.RegisterTool(tool);

        var definitions = registry.GetToolDefinitions();
        definitions.Should().HaveCount(1);
        definitions[0].Name.Should().Be("clif_test_a");
    }

    [Fact]
    public void RegisterTool_OverwritesSameNameTool()
    {
        var registry = new ToolRegistry();
        var tool1 = new FakeToolA();
        var tool2 = new FakeToolA(); // same name

        registry.RegisterTool(tool1);
        registry.RegisterTool(tool2);

        var definitions = registry.GetToolDefinitions();
        definitions.Should().HaveCount(1);
    }

    [Fact]
    public void RegisterTool_MultipleToolsWithDifferentNames()
    {
        var registry = new ToolRegistry();

        registry.RegisterTool(new FakeToolA());
        registry.RegisterTool(new FakeToolB());

        var definitions = registry.GetToolDefinitions();
        definitions.Should().HaveCount(2);
        definitions.Select(d => d.Name).Should().Contain("clif_test_a");
        definitions.Select(d => d.Name).Should().Contain("clif_test_b");
    }

    [Fact]
    public void GetToolDefinitions_ReturnsToolsInDeterministicOrdinalNameOrder()
    {
        var registry = new ToolRegistry();

        registry.RegisterTool(new FakeToolB());
        registry.RegisterTool(new FakeToolA());

        registry.GetToolDefinitions()
            .Select(definition => definition.Name)
            .Should()
            .Equal("clif_test_a", "clif_test_b");
    }

    [Fact]
    public void GetToolDefinitions_ReturnsEmptyForNoTools()
    {
        var registry = new ToolRegistry();

        var definitions = registry.GetToolDefinitions();

        definitions.Should().BeEmpty();
    }

    [Fact]
    public void GetToolDefinitions_ReturnsCorrectDefinitions()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new FakeToolA());

        var definitions = registry.GetToolDefinitions();

        definitions.Should().HaveCount(1);
        definitions[0].Name.Should().Be("clif_test_a");
        definitions[0].Description.Should().Be("Fake tool A for testing");
    }

    [Fact]
    public async Task ExecuteToolAsync_KnownTool_ReturnsResult()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new FakeToolA());

        var result = await registry.ExecuteToolAsync("clif_test_a", null);

        result.Should().NotBeNull();
        result.Content.Should().HaveCount(1);
        result.Content[0].Text.Should().Be("FakeToolA executed");
        result.IsError.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ReturnsError()
    {
        var registry = new ToolRegistry();

        var result = await registry.ExecuteToolAsync("nonexistent_tool", null);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Unknown tool: nonexistent_tool");
    }

    [Fact]
    public async Task ExecuteToolAsync_ToolThrowsException_ReturnsError()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new ThrowingTool());

        var result = await registry.ExecuteToolAsync("clif_throwing", null);

        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error: Intentional test exception");
    }

    [Fact]
    public async Task ExecuteToolAsync_WithArguments_PassesThrough()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());

        var argsJson = """{"message":"hello world"}""";
        var args = JsonSerializer.Deserialize<JsonElement>(argsJson);

        var result = await registry.ExecuteToolAsync("clif_echo", args);

        result.Should().NotBeNull();
        result.Content[0].Text.Should().Contain("hello world");
    }

    [Fact]
    public async Task ExecuteToolAsync_WithNullArguments_ToolHandlesGracefully()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());

        var result = await registry.ExecuteToolAsync("clif_echo", null);

        result.Should().NotBeNull();
        result.Content[0].Text.Should().Be("No message provided");
    }

    [Fact]
    public async Task ExecuteToolAsync_PropagatesCancellationTokenToTool()
    {
        var registry = new ToolRegistry();
        var tool = new CancellationAwareTool();
        registry.RegisterTool(tool);
        using var cts = new CancellationTokenSource();

        await registry.ExecuteToolAsync("clif_cancellation_aware", null, cts.Token);

        tool.ReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ExecuteToolAsync_DoesNotConvertRequestedCancellationToToolError()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new CancellationAwareTool());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var action = () => registry.ExecuteToolAsync("clif_cancellation_aware", null, cts.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    // --- Fake tool implementations for testing ---

    private sealed class FakeToolA : ToolBase
    {
        public override string Name => "clif_test_a";
        public override string Description => "Fake tool A for testing";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            return Task.FromResult(TextResult("FakeToolA executed"));
        }
    }

    private sealed class FakeToolB : ToolBase
    {
        public override string Name => "clif_test_b";
        public override string Description => "Fake tool B for testing";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            return Task.FromResult(TextResult("FakeToolB executed"));
        }
    }

    private sealed class ThrowingTool : ToolBase
    {
        public override string Name => "clif_throwing";
        public override string Description => "Tool that throws for testing";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            throw new InvalidOperationException("Intentional test exception");
        }
    }

    private sealed class EchoTool : ToolBase
    {
        public override string Name => "clif_echo";
        public override string Description => "Echo tool for testing";
        public override object InputSchema => new
        {
            type = "object",
            properties = new
            {
                message = new { type = "string" },
            },
        };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            var message = GetStringArgument(arguments, "message");
            if (string.IsNullOrEmpty(message))
            {
                return Task.FromResult(TextResult("No message provided"));
            }

            return Task.FromResult(TextResult($"Echo: {message}"));
        }
    }

    private sealed class CancellationAwareTool : ToolBase
    {
        public override string Name => "clif_cancellation_aware";
        public override string Description => "Cancellation-aware tool for testing";
        public override object InputSchema => new { type = "object" };
        public CancellationToken ReceivedToken { get; private set; }

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments) =>
            Task.FromResult(TextResult("legacy execution"));

        public override Task<McpToolResult> ExecuteAsync(
            JsonElement? arguments,
            CancellationToken cancellationToken)
        {
            ReceivedToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(TextResult("cancellation-aware execution"));
        }
    }
}
