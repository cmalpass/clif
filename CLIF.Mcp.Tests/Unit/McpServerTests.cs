using System.IO;
using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Integration tests for McpServer - tests the full JSON-RPC request/response lifecycle
/// by simulating stdin/stdout via streams.
/// </summary>
public class McpServerTests
{
    /// <summary>
    /// Helper that sends JSON-RPC requests to the McpServer via in-memory streams
    /// and returns the responses.
    /// </summary>
    private static async Task<List<JsonRpcResponse>> SendRequestsAsync(
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
        var responses = new List<JsonRpcResponse>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                var response = JsonSerializer.Deserialize<JsonRpcResponse>(line, McpProtocol.JsonOptions);
                if (response != null) responses.Add(response);
            }
        }

        return responses;
    }

    [Fact]
    public async Task Initialize_ReturnsServerInfo()
    {
        var registry = new ToolRegistry();
        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":1,"method":"initialize"}""");

        responses.Should().HaveCount(1);
        responses[0].Id.Should().NotBeNull();
        responses[0].Error.Should().BeNull();
        responses[0].Result.Should().NotBeNull();

        var resultJson = JsonSerializer.Serialize(responses[0].Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("clif-mcp");
        resultJson.Should().Contain("2024-11-05");
    }

    [Fact]
    public async Task NotificationsInitialized_ReturnsNoResponse()
    {
        var registry = new ToolRegistry();
        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""");

        // Notifications should not produce a response
        responses.Should().BeEmpty();
    }

    [Fact]
    public async Task ToolsList_ReturnsRegisteredTools()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool("clif_test1", "Test tool 1"));
        registry.RegisterTool(new TestTool("clif_test2", "Test tool 2"));

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""");

        responses.Should().HaveCount(1);
        responses[0].Error.Should().BeNull();
        var resultJson = JsonSerializer.Serialize(responses[0].Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("clif_test1");
        resultJson.Should().Contain("clif_test2");
    }

    [Fact]
    public async Task ToolsList_EmptyRegistry_ReturnsEmptyList()
    {
        var registry = new ToolRegistry();

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":3,"method":"tools/list"}""");

        responses.Should().HaveCount(1);
        responses[0].Error.Should().BeNull();
        var resultJson = JsonSerializer.Serialize(responses[0].Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("\"tools\":[]");
    }

    [Fact]
    public async Task ToolsCall_ExecutesToolAndReturnsResult()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool("clif_greet", "Greet someone"));

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"clif_greet","arguments":{"who":"World"}}}""");

        responses.Should().HaveCount(1);
        responses[0].Error.Should().BeNull();
        var resultJson = JsonSerializer.Serialize(responses[0].Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("TestTool executed");
    }

    [Fact]
    public async Task ToolsCall_UnknownTool_ReturnsToolError()
    {
        var registry = new ToolRegistry();

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"clif_nonexistent"}}""");

        responses.Should().HaveCount(1);
        responses[0].Error.Should().BeNull(); // JSON-RPC level is ok, error is in the tool result
        var resultJson = JsonSerializer.Serialize(responses[0].Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("Unknown tool");
    }

    [Fact]
    public async Task ToolsCall_MissingParams_ReturnsError()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool("clif_test", "Test"));

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":6,"method":"tools/call"}""");

        responses.Should().HaveCount(1);
        responses[0].Error.Should().BeNull(); // Handled at tool call level
        var resultJson = JsonSerializer.Serialize(responses[0].Result, McpProtocol.JsonOptions);
        resultJson.Should().Contain("Missing params");
    }

    [Fact]
    public async Task UnknownMethod_ReturnsJsonRpcError()
    {
        var registry = new ToolRegistry();

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":7,"method":"unknown/method"}""");

        responses.Should().HaveCount(1);
        responses[0].Error.Should().NotBeNull();
        responses[0].Error!.Code.Should().Be(-32601);
        responses[0].Error!.Message.Should().Contain("Unknown method");
    }

    [Fact]
    public async Task MultipleRequests_AllProcessed()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new TestTool("clif_test", "Test"));

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":1,"method":"initialize"}""",
            """{"jsonrpc":"2.0","method":"notifications/initialized"}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"clif_test"}}""");

        // notification doesn't produce a response
        responses.Should().HaveCount(3);
    }

    [Fact]
    public async Task BlankLines_AreIgnored()
    {
        var registry = new ToolRegistry();
        var input = "\n\n" + """{"jsonrpc":"2.0","id":1,"method":"initialize"}""" + "\n\n\n";
        using var inputStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input));
        using var outputStream = new MemoryStream();

        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        try
        {
            Console.SetIn(new StreamReader(inputStream));
            Console.SetOut(new StreamWriter(outputStream) { AutoFlush = true });

            var server = new McpServer(registry);
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
        var responses = new List<JsonRpcResponse>();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (!string.IsNullOrWhiteSpace(line))
            {
                var response = JsonSerializer.Deserialize<JsonRpcResponse>(line, McpProtocol.JsonOptions);
                if (response != null) responses.Add(response);
            }
        }

        responses.Should().HaveCount(1);
    }

    [Fact]
    public async Task IdPreservation_ResponseMatchesRequestId()
    {
        var registry = new ToolRegistry();

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":42,"method":"initialize"}""");

        responses.Should().HaveCount(1);
        responses[0].Id.Should().NotBeNull();
        responses[0].Id!.Value.GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task StringIdPreservation_ResponseMatchesRequestId()
    {
        var registry = new ToolRegistry();

        var responses = await SendRequestsAsync(registry,
            """{"jsonrpc":"2.0","id":"request-abc","method":"initialize"}""");

        responses.Should().HaveCount(1);
        responses[0].Id.Should().NotBeNull();
        responses[0].Id!.Value.GetString().Should().Be("request-abc");
    }

    /// <summary>
    /// Simple test tool implementation for McpServer tests.
    /// </summary>
    private sealed class TestTool : ToolBase
    {
        private readonly string _name;
        private readonly string _description;

        public TestTool(string name, string description)
        {
            _name = name;
            _description = description;
        }

        public override string Name => _name;
        public override string Description => _description;
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            return Task.FromResult(TextResult("TestTool executed"));
        }
    }
}
