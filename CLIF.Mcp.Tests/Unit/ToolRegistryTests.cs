using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;
using CLIF.Mcp.Diagnostics;
using CLIF.Mcp.Security;

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

        tool.ReceivedToken.CanBeCanceled.Should().BeTrue();
        tool.ReceivedToken.IsCancellationRequested.Should().BeFalse();
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

    [Fact]
    public async Task ExecuteToolAsync_DeniesCapabilityBeforeToolExecution()
    {
        var registry = new ToolRegistry(new McpSafetyPolicy(allowInput: false));
        var tool = new InputTool();
        registry.RegisterTool(tool);

        var result = await registry.ExecuteToolAsync(tool.Name, null);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().StartWith("MCP_PERMISSION_DENIED:");
        tool.Executed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteToolAsync_AllowsCapabilityWhenGranted()
    {
        var registry = new ToolRegistry(new McpSafetyPolicy(allowInput: true));
        var tool = new InputTool();
        registry.RegisterTool(tool);

        var result = await registry.ExecuteToolAsync(tool.Name, null);

        result.IsError.Should().NotBeTrue();
        tool.Executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteToolAsync_RejectsNonObjectArguments()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());
        using var document = JsonDocument.Parse("[1,2,3]");

        var result = await registry.ExecuteToolAsync("clif_echo", document.RootElement);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Be("MCP_INVALID_PARAMS: tool arguments must be a JSON object");
    }

    [Fact]
    public async Task ExecuteToolAsync_RejectsOversizedStringArguments()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());
        var payload = JsonSerializer.Serialize(new { message = new string('x', McpSafetyPolicy.MaximumArgumentStringLength + 1) });
        using var document = JsonDocument.Parse(payload);

        var result = await registry.ExecuteToolAsync("clif_echo", document.RootElement);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("MCP_INVALID_PARAMS");
    }

    [Fact]
    public async Task ExecuteToolAsync_EnforcesRequiredTypeEnumAndClosedObjectSchemaBeforeExecution()
    {
        var registry = new ToolRegistry();
        var tool = new SchemaBoundTool();
        registry.RegisterTool(tool);

        using var missingRequired = JsonDocument.Parse("{}");
        var missingResult = await registry.ExecuteToolAsync(tool.Name, missingRequired.RootElement);
        missingResult.Content[0].Text.Should().Contain("missing required argument 'mode'");

        using var invalidType = JsonDocument.Parse("""{"mode":4}""");
        var typeResult = await registry.ExecuteToolAsync(tool.Name, invalidType.RootElement);
        typeResult.Content[0].Text.Should().Contain("arguments.mode must be a string");

        using var invalidEnum = JsonDocument.Parse("""{"mode":"unsafe"}""");
        var enumResult = await registry.ExecuteToolAsync(tool.Name, invalidEnum.RootElement);
        enumResult.Content[0].Text.Should().Contain("arguments.mode is not an allowed value");

        using var unexpectedArgument = JsonDocument.Parse("""{"mode":"safe","typo":true}""");
        var unexpectedResult = await registry.ExecuteToolAsync(tool.Name, unexpectedArgument.RootElement);
        unexpectedResult.Content[0].Text.Should().Contain("unexpected argument 'typo'");

        tool.Executed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteToolAsync_ReturnsTimeoutForCooperativelyCancellableTool()
    {
        var registry = new ToolRegistry(toolExecutionTimeout: TimeSpan.FromMilliseconds(25));
        registry.RegisterTool(new BlockingTool());

        var result = await registry.ExecuteToolAsync("clif_blocking", null);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().StartWith("MCP_TOOL_TIMEOUT:");
    }

    [Fact]
    public async Task ExecuteToolAsync_EmitsStartedAndCompletedDiagnosticsWithoutArguments()
    {
        using var writer = new StringWriter();
        var registry = new ToolRegistry(diagnostics: new McpDiagnostics(writer, enabled: true));
        registry.RegisterTool(new EchoTool());

        await registry.ExecuteToolAsync("clif_echo", null);

        var events = new List<string?>();
        foreach (var line in writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            using var document = JsonDocument.Parse(line);
            events.Add(document.RootElement.GetProperty("event").GetString());
        }
        events.Should().ContainInOrder("mcp.tool.started", "mcp.tool.completed");
        writer.ToString().Should().NotContain("hello world");
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

    private sealed class InputTool : ToolBase
    {
        public override string Name => "clif_input";
        public override string Description => "Input tool for testing";
        public override object InputSchema => new { type = "object" };
        public override McpCapability RequiredCapability => McpCapability.Input;
        public bool Executed { get; private set; }

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            Executed = true;
            return Task.FromResult(TextResult("executed"));
        }
    }

    private sealed class SchemaBoundTool : ToolBase
    {
        public override string Name => "clif_schema_bound";
        public override string Description => "Schema validation test tool";
        public override object InputSchema => new
        {
            type = "object",
            properties = new
            {
                mode = new { type = "string", @enum = new[] { "safe", "strict" } },
                attempts = new { type = "integer", minimum = 0, maximum = 3 },
            },
            required = new[] { "mode" },
            additionalProperties = false,
        };
        public bool Executed { get; private set; }

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            Executed = true;
            return Task.FromResult(TextResult("executed"));
        }
    }

    private sealed class BlockingTool : ToolBase
    {
        public override string Name => "clif_blocking";
        public override string Description => "Cooperatively cancellable blocking test tool";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments) =>
            Task.FromResult(TextResult("legacy execution"));

        public override async Task<McpToolResult> ExecuteAsync(
            JsonElement? arguments,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return TextResult("unreachable");
        }
    }
}
