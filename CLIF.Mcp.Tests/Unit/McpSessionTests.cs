using System.IO;
using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// End-to-end MCP session tests that simulate a complete MCP client lifecycle:
/// initialize → tools/list → tools/call → verify responses.
/// </summary>
public class McpSessionTests
{
    /// <summary>
    /// Helper that sends JSON-RPC requests to the McpServer via in-memory streams.
    /// </summary>
    private static async Task<List<string>> RunMcpSessionAsync(
        ToolRegistry toolRegistry,
        params string[] requestJsonLines)
    {
        var input = string.Join("\n", requestJsonLines) + "\n";
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input));
        using var outputStream = new MemoryStream();

        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        try
        {
            Console.SetIn(new StreamReader(inputStream));
            Console.SetOut(new StreamWriter(outputStream) { AutoFlush = true });

            var server = new McpServer(toolRegistry);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await server.RunAsync(cts.Token);
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }

        outputStream.Position = 0;
        using var reader = new StreamReader(outputStream);
        var lines = new List<string>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    [Fact]
    public async Task FullSession_InitializeAndListTools()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());

        var lines = await RunMcpSessionAsync(registry,
            """{"jsonrpc":"2.0","id":0,"method":"initialize"}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""");

        lines.Should().HaveCount(2); // initialize + tools/list (notification has no response)

        // Verify initialize response
        var initResponse = JsonSerializer.Deserialize<JsonRpcResponse>(lines[0], McpProtocol.JsonOptions);
        initResponse.Should().NotBeNull();
        initResponse!.Error.Should().BeNull();
        var initJson = JsonSerializer.Serialize(initResponse.Result, McpProtocol.JsonOptions);
        initJson.Should().Contain("clif-mcp");
        initJson.Should().Contain("0.1.0");

        // Verify tools/list response
        var listResponse = JsonSerializer.Deserialize<JsonRpcResponse>(lines[1], McpProtocol.JsonOptions);
        listResponse.Should().NotBeNull();
        listResponse!.Error.Should().BeNull();
        var listJson = JsonSerializer.Serialize(listResponse.Result, McpProtocol.JsonOptions);
        listJson.Should().Contain("clif_echo");
    }

    [Fact]
    public async Task FullSession_CallToolSuccessfully()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());

        var lines = await RunMcpSessionAsync(registry,
            """{"jsonrpc":"2.0","id":0,"method":"initialize"}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"clif_echo","arguments":{"message":"Hello from AI agent!"}}}""");

        lines.Should().HaveCount(2); // initialize + tool call

        var callResponse = JsonSerializer.Deserialize<JsonRpcResponse>(lines[1], McpProtocol.JsonOptions);
        callResponse.Should().NotBeNull();
        callResponse!.Error.Should().BeNull();
        var resultJson = JsonSerializer.Serialize(callResponse.Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("Echo: Hello from AI agent!");
    }

    [Fact]
    public async Task FullSession_ToolError_ReturnsErrorResult()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new FailingTool());

        var lines = await RunMcpSessionAsync(registry,
            """{"jsonrpc":"2.0","id":0,"method":"initialize"}""",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"clif_fail","arguments":{}}}""");

        lines.Should().HaveCount(2);

        var callResponse = JsonSerializer.Deserialize<JsonRpcResponse>(lines[1], McpProtocol.JsonOptions);
        callResponse.Should().NotBeNull();
        callResponse!.Error.Should().BeNull(); // Error is in the tool result, not the JSON-RPC error
        var resultJson = JsonSerializer.Serialize(callResponse.Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("Error:");
        resultJson.Should().Contain("\"isError\":true");
    }

    [Fact]
    public async Task FullSession_MultipleToolCalls()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new CounterTool());

        var lines = await RunMcpSessionAsync(registry,
            """{"jsonrpc":"2.0","id":0,"method":"initialize"}""",
            """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"clif_counter"}}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"clif_counter"}}""",
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"clif_counter"}}""");

        lines.Should().HaveCount(4);

        // Each call should increment the counter
        for (int i = 1; i <= 3; i++)
        {
            var response = JsonSerializer.Deserialize<JsonRpcResponse>(lines[i], McpProtocol.JsonOptions);
            var resultJson = JsonSerializer.Serialize(response!.Result, McpProtocol.JsonOptions);
            resultJson.Should().Contain($"Count: {i}");
        }
    }

    [Fact]
    public async Task FullSession_ResponsesAreValidJsonRpc()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());

        var lines = await RunMcpSessionAsync(registry,
            """{"jsonrpc":"2.0","id":1,"method":"initialize"}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"clif_echo","arguments":{"message":"test"}}}""");

        foreach (var line in lines)
        {
            // Each line should be valid JSON
            var action = () => JsonSerializer.Deserialize<JsonRpcResponse>(line, McpProtocol.JsonOptions);
            action.Should().NotThrow("every response line should be valid JSON");

            var response = JsonSerializer.Deserialize<JsonRpcResponse>(line, McpProtocol.JsonOptions);
            response!.JsonRpc.Should().Be("2.0", "every response must have jsonrpc 2.0");
            response.Id.Should().NotBeNull("responses to requests must have an id");
        }
    }

    [Fact]
    public async Task FullSession_ResponseOrderMatchesRequestOrder()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new EchoTool());

        var lines = await RunMcpSessionAsync(registry,
            """{"jsonrpc":"2.0","id":10,"method":"initialize"}""",
            """{"jsonrpc":"2.0","id":20,"method":"tools/list"}""",
            """{"jsonrpc":"2.0","id":30,"method":"tools/call","params":{"name":"clif_echo","arguments":{"message":"test"}}}""");

        lines.Should().HaveCount(3);

        var ids = lines.Select(line =>
        {
            var response = JsonSerializer.Deserialize<JsonRpcResponse>(line, McpProtocol.JsonOptions);
            return response!.Id!.Value.GetInt32();
        }).ToList();

        ids.Should().Equal(10, 20, 30);
    }

    [Fact]
    public async Task FullSession_ScriptToolCall_WithInlineContent()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new CLIF.Mcp.Tools.ScriptTool());

        var scriptContent = "{\"name\":\"Automated Test\",\"description\":\"test\",\"version\":\"1.0\",\"steps\":[{\"action\":\"click\",\"element\":\"id=btn\"}]}";
        var escapedContent = JsonSerializer.Serialize(scriptContent); // produces a JSON string with quotes
        var callJson = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"clif_run_script\",\"arguments\":{\"content\":" + escapedContent + "}}}";

        var lines = await RunMcpSessionAsync(registry, callJson);

        lines.Should().HaveCount(1);
        var response = JsonSerializer.Deserialize<JsonRpcResponse>(lines[0], McpProtocol.JsonOptions);
        response!.Error.Should().BeNull();
        var resultJson = JsonSerializer.Serialize(response.Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("Automated Test");
        resultJson.Should().Contain("1 steps");
    }

    // --- Test tool implementations ---

    private sealed class EchoTool : ToolBase
    {
        public override string Name => "clif_echo";
        public override string Description => "Echo test tool";
        public override object InputSchema => new
        {
            type = "object",
            properties = new { message = new { type = "string" } },
        };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            var message = GetStringArgument(arguments, "message");
            return Task.FromResult(TextResult(
                string.IsNullOrEmpty(message) ? "No message" : $"Echo: {message}"));
        }
    }

    private sealed class FailingTool : ToolBase
    {
        public override string Name => "clif_fail";
        public override string Description => "Failing tool for testing";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            throw new InvalidOperationException("Intentional failure for testing");
        }
    }

    private sealed class CounterTool : ToolBase
    {
        private int _count;
        public override string Name => "clif_counter";
        public override string Description => "Counter tool for testing";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            return Task.FromResult(TextResult($"Count: {++_count}"));
        }
    }
}
