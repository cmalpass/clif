// Licensed under the MIT License.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using CLIF.Mcp.Security;

namespace CLIF.Mcp;

/// <summary>
/// Bridges the existing CLIF tool contract into the official MCP server SDK.
/// </summary>
/// <remarks>
/// This adapter is deliberately small and one-way: the application-owned
/// <see cref="ToolRegistry"/> remains responsible for policy, validation,
/// diagnostics, and execution while the SDK owns protocol serialization and
/// tool invocation. Keeping that boundary explicit lets the protocol host be
/// migrated without duplicating safety-sensitive tool behavior.
/// </remarks>
public static class McpSdkToolAdapter
{
    /// <summary>Creates a collection that emits tools in ordinal name order.</summary>
    public static McpServerPrimitiveCollection<McpServerTool> CreateCollection(
        IEnumerable<McpServerTool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        return new SortedToolCollection(tools);
    }
    /// <summary>
    /// Creates an SDK tool for an existing CLIF tool.
    /// </summary>
    public static McpServerTool Create(ITool tool, ToolRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(registry);

        var definition = tool.GetDefinition();
        var inputSchema = JsonSerializer.SerializeToElement(definition.InputSchema);
        return new RegistryMcpServerTool(
            new Tool
            {
                Name = definition.Name,
                Description = definition.Description,
                InputSchema = inputSchema,
                Annotations = new ToolAnnotations
                {
                    ReadOnlyHint = tool.RequiredCapability == McpCapability.ReadOnly,
                    DestructiveHint = tool.RequiredCapability != McpCapability.ReadOnly,
                    IdempotentHint = false,
                    OpenWorldHint = false,
                },
            },
            tool,
            registry);
    }

    /// <summary>
    /// Creates SDK tools for the registry in the registry's deterministic order.
    /// </summary>
    public static IReadOnlyList<McpServerTool> CreateAll(ToolRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.GetToolDefinitions()
            .Select(definition => Create(
                registry.GetTool(definition.Name),
                registry))
            .ToArray();
    }

    /// <summary>Executes a registry tool for an SDK call-tool handler.</summary>
    public static Task<CallToolResult> ExecuteAsync(
        string name,
        IDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken,
        ToolRegistry registry)
    {
        if (!registry.ContainsTool(name))
        {
            throw new McpException($"Unknown tool: '{name}'");
        }

        return InvokeAsync(name, arguments, cancellationToken, registry);
    }

    private static async Task<CallToolResult> InvokeAsync(
        string name,
        IDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken,
        ToolRegistry registry)
    {
        JsonElement? argumentObject = arguments is null
            ? null
            : JsonSerializer.SerializeToElement(arguments);
        var result = await registry.ExecuteToolAsync(name, argumentObject, cancellationToken).ConfigureAwait(false);
        return new CallToolResult
        {
            Content = result.Content.Select(ToContentBlock).ToList(),
            IsError = result.IsError,
        };
    }

    private static ContentBlock ToContentBlock(McpContent content)
    {
        return content.Type switch
        {
            "image" => ImageContentBlock.FromBytes(
                Convert.FromBase64String(content.Data ?? string.Empty),
                content.MimeType ?? "application/octet-stream"),
            _ => new TextContentBlock { Text = content.Text ?? string.Empty },
        };
    }

    private sealed class RegistryMcpServerTool : McpServerTool
    {
        private readonly Tool _protocolTool;
        private readonly ITool _tool;
        private readonly ToolRegistry _registry;

        public RegistryMcpServerTool(Tool protocolTool, ITool tool, ToolRegistry registry)
        {
            _protocolTool = protocolTool;
            _tool = tool;
            _registry = registry;
        }

        public override Tool ProtocolTool => _protocolTool;

        public override IReadOnlyList<object> Metadata => Array.Empty<object>();

        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken)
        {
            return await McpSdkToolAdapter.InvokeAsync(
                _tool.Name,
                request.Params?.Arguments,
                cancellationToken,
                _registry).ConfigureAwait(false);
        }
    }

    private sealed class SortedToolCollection : McpServerPrimitiveCollection<McpServerTool>
    {
        public SortedToolCollection(IEnumerable<McpServerTool> tools)
            : base(StringComparer.Ordinal)
        {
            foreach (var tool in tools) Add(tool);
        }

        public override McpServerTool[] ToArray() => base.ToArray()
            .OrderBy(tool => tool.ProtocolTool.Name, StringComparer.Ordinal)
            .ToArray();

        public override IEnumerator<McpServerTool> GetEnumerator() => ToArray().AsEnumerable().GetEnumerator();
    }
}
