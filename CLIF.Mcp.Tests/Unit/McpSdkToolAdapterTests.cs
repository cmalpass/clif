using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

#pragma warning disable MCPEXP002, MCP9005, CS0672

namespace CLIF.Mcp.Tests.Unit;

public sealed class McpSdkToolAdapterTests
{
    [Fact]
    public void Create_PreservesCanonicalDefinition()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new AdapterEchoTool());

        var sdkTool = McpSdkToolAdapter.Create(new AdapterEchoTool(), registry);

        sdkTool.ProtocolTool.Name.Should().Be("clif_adapter_echo");
        sdkTool.ProtocolTool.Description.Should().Be("Adapter echo tool");
        sdkTool.ProtocolTool.InputSchema.GetProperty("type").GetString().Should().Be("object");
        sdkTool.ProtocolTool.InputSchema.GetProperty("properties").GetProperty("message").GetProperty("type").GetString().Should().Be("string");
        sdkTool.ProtocolTool.Annotations!.ReadOnlyHint.Should().BeTrue();
        sdkTool.ProtocolTool.Annotations.DestructiveHint.Should().BeFalse();
    }

    [Fact]
    public void CreateAll_UsesRegistryNameOrder()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new AdapterEchoTool("clif_z_echo"));
        registry.RegisterTool(new AdapterEchoTool("clif_a_echo"));

        var sdkTools = McpSdkToolAdapter.CreateAll(registry);

        sdkTools.Select(tool => tool.ProtocolTool.Name)
            .Should().Equal("clif_a_echo", "clif_z_echo");
    }

    [Fact]
    public void CreateCollection_EmitsToolsInOrdinalNameOrder()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new AdapterEchoTool("clif_z_echo"));
        registry.RegisterTool(new AdapterEchoTool("clif_a_echo"));

        var collection = McpSdkToolAdapter.CreateCollection(McpSdkToolAdapter.CreateAll(registry));

        collection.ToArray().Select(tool => tool.ProtocolTool.Name)
            .Should().Equal("clif_a_echo", "clif_z_echo");
    }

    [Fact]
    public async Task Create_InvokesThroughRegistryAndPreservesResult()
    {
        var registry = new ToolRegistry();
        registry.RegisterTool(new AdapterEchoTool());
        var sdkTool = McpSdkToolAdapter.Create(new AdapterEchoTool(), registry);
        var arguments = new Dictionary<string, JsonElement>
        {
            ["message"] = JsonSerializer.SerializeToElement("hello sdk"),
        };
        var request = new RequestContext<CallToolRequestParams>(
            new TestMcpServer(),
            new ModelContextProtocol.Protocol.JsonRpcRequest { Method = "tools/call" },
            new CallToolRequestParams { Name = "clif_adapter_echo", Arguments = arguments });

        var result = await sdkTool.InvokeAsync(request, CancellationToken.None);

        result.IsError.Should().BeNull();
        result.Content.Should().ContainSingle().Which.Should().BeOfType<TextContentBlock>()
            .Which.Text.Should().Be("hello sdk");
    }

    private sealed class AdapterEchoTool : ToolBase
    {
        private readonly string _name;

        public AdapterEchoTool(string name = "clif_adapter_echo") => _name = name;

        public override string Name => _name;
        public override string Description => "Adapter echo tool";
        public override object InputSchema => new
        {
            type = "object",
            properties = new { message = new { type = "string" } },
        };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            var message = arguments?.GetProperty("message").GetString() ?? string.Empty;
            return Task.FromResult(new McpToolResult
            {
                Content = new List<McpContent> { new() { Type = "text", Text = message } },
            });
        }
    }

    private sealed class TestMcpServer : ModelContextProtocol.Server.McpServer
    {
        public override ClientCapabilities? ClientCapabilities => null;
        public override Implementation? ClientInfo => null;
        public override McpServerOptions ServerOptions => new();
        public override IServiceProvider Services => null!;
        public override LoggingLevel? LoggingLevel => null;
        public override Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override string? SessionId => null;
        public override string? NegotiatedProtocolVersion => null;
        public override Task<ModelContextProtocol.Protocol.JsonRpcResponse> SendRequestAsync(
            ModelContextProtocol.Protocol.JsonRpcRequest request,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public override Task SendMessageAsync(
            ModelContextProtocol.Protocol.JsonRpcMessage message,
            CancellationToken cancellationToken) => throw new NotSupportedException();
        public override IAsyncDisposable RegisterNotificationHandler(
            string method,
            Func<ModelContextProtocol.Protocol.JsonRpcNotification, CancellationToken, ValueTask> handler) => throw new NotSupportedException();
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

#pragma warning restore MCPEXP002, MCP9005, CS0672
