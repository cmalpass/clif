using System.Text.Json;
using CLIF.Mcp;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Tests cooperative cancellation from the MCP session through request dispatch
/// and into tool execution.
/// </summary>
public class McpCancellationTests
{
    [Fact]
    public async Task RunAsync_CancelingToolExecutionStopsWithoutWritingInternalError()
    {
        var tool = new BlockingTool();
        var registry = new ToolRegistry();
        registry.RegisterTool(tool);

        var input = new StringReader(string.Join("\n",
            InitializeRequest("1"),
            "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}",
            "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"clif_blocking\"}}") + "\n");
        var output = new StringWriter();
        var originalIn = Console.In;
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        using var cancellation = new CancellationTokenSource();
        Task serverTask;

        try
        {
            Console.SetIn(input);
            Console.SetOut(output);

            serverTask = new McpServer(registry).RunAsync(cancellation.Token);
            var receivedToken = await tool.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(receivedToken.CanBeCanceled);

            cancellation.Cancel();
            await serverTask.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            cancellation.Cancel();
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }

        var responses = output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<JsonRpcResponse>(line, McpProtocol.JsonOptions))
            .Where(response => response is not null)
            .ToList();

        var response = Assert.Single(responses);
        Assert.Equal(1, response!.Id!.Value.GetInt32());
        Assert.DoesNotContain("Internal error", output.ToString());
    }

    private static string InitializeRequest(string id) =>
        "{\"jsonrpc\":\"2.0\",\"id\":" + id +
        ",\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"" +
        McpProtocol.SupportedProtocolVersion +
        "\",\"capabilities\":{},\"clientInfo\":{\"name\":\"clif-tests\",\"version\":\"1.0\"}}}";

    private sealed class BlockingTool : ToolBase
    {
        public override string Name => "clif_blocking";
        public override string Description => "Cancellation blocking tool for testing";
        public override object InputSchema => new { type = "object" };
        public TaskCompletionSource<CancellationToken> Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments) =>
            Task.FromResult(TextResult("legacy execution"));

        public override async Task<McpToolResult> ExecuteAsync(
            JsonElement? arguments,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return TextResult("unexpected completion");
        }
    }
}
