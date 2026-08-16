// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLIF.Mcp;

/// <summary>
/// MCP Protocol message types and JSON-RPC serialization options.
/// </summary>
public static class McpProtocol
{
    /// <summary>
    /// The MCP protocol version implemented by this server.
    /// </summary>
    public const string SupportedProtocolVersion = "2025-06-18";

    /// <summary>
    /// JSON serialization settings used for MCP messages.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}

/// <summary>
/// JSON-RPC 2.0 request message.
/// </summary>
public sealed class JsonRpcRequest
{
    /// <summary>
    /// Gets or sets the JSON-RPC protocol marker.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the request identifier, when one was supplied.
    /// </summary>
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    /// <summary>
    /// Gets whether the incoming JSON-RPC object contained an <c>id</c> member.
    /// This is kept separate from <see cref="Id"/> so an omitted id can be
    /// handled as a notification.
    /// </summary>
    [JsonIgnore]
    public bool HasId { get; set; }

    /// <summary>
    /// Gets or sets the method name requested by the client.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional method parameters.
    /// </summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 response message.
/// </summary>
public sealed class JsonRpcResponse
{
    /// <summary>
    /// Gets or sets the JSON-RPC protocol marker.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// Gets or sets the identifier copied from the corresponding request.
    /// </summary>
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }

    /// <summary>
    /// Gets or sets the successful result, when the request succeeded.
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Gets or sets the protocol error, when the request failed.
    /// </summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }
}

/// <summary>
/// JSON-RPC 2.0 error object.
/// </summary>
public sealed class JsonRpcError
{
    /// <summary>
    /// Gets or sets the JSON-RPC error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional error-specific data.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// MCP server information returned during initialization.
/// </summary>
public sealed class McpServerInfo
{
    /// <summary>
    /// Gets or sets the server name advertised to clients.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "clif-mcp";

    /// <summary>
    /// Gets or sets the server version advertised to clients.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1.0";
}

/// <summary>
/// MCP server capabilities advertised during initialization.
/// </summary>
public sealed class McpCapabilities
{
    /// <summary>
    /// Gets or sets the tools capability, when tools are available.
    /// </summary>
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; set; }
}

/// <summary>
/// Tools capability descriptor.
/// </summary>
public sealed class ToolsCapability
{
    /// <summary>
    /// Gets or sets whether the server may notify clients when its tool list changes.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// MCP initialization result returned to the client.
/// </summary>
public sealed class McpInitializeResult
{
    /// <summary>
    /// Gets or sets the protocol version selected for the session.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = McpProtocol.SupportedProtocolVersion;

    /// <summary>
    /// Gets or sets the capabilities exposed by the server.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets the server information returned to the client.
    /// </summary>
    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

/// <summary>
/// Parameters supplied by a client when it initializes an MCP session.
/// </summary>
public sealed class McpInitializeParams
{
    /// <summary>
    /// Gets or sets the protocol version requested by the client.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque client capability payload.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public JsonElement? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets basic metadata about the client.
    /// </summary>
    [JsonPropertyName("clientInfo")]
    public McpClientInfo? ClientInfo { get; set; }
}

/// <summary>
/// Basic metadata about the MCP client opening the session.
/// </summary>
public sealed class McpClientInfo
{
    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// MCP tool definition exposed via tools/list.
/// </summary>
public sealed class McpTool
{
    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tool description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON schema for tool arguments.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { type = "object" };
}

/// <summary>
/// MCP tools/list response.
/// </summary>
public sealed class McpToolsListResult
{
    /// <summary>
    /// Gets or sets the tools exposed by the server.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();
}

/// <summary>
/// Parameters for a tools/call request.
/// </summary>
public sealed class McpToolCallParams
{
    /// <summary>
    /// Gets or sets the name of the tool to invoke.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional tool arguments.
    /// </summary>
    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; set; }
}

/// <summary>
/// Result returned from an MCP tool execution.
/// </summary>
public sealed class McpToolResult
{
    /// <summary>
    /// Gets or sets the content returned by the tool.
    /// </summary>
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the tool reported an execution error.
    /// </summary>
    [JsonPropertyName("isError")]
    public bool? IsError { get; set; }
}

/// <summary>
/// A single content item in a tool result (text, image, etc.).
/// </summary>
public sealed class McpContent
{
    /// <summary>
    /// Gets or sets the content type, such as <c>text</c> or <c>image</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Gets or sets text content, when the item contains text.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets base64-encoded content data, when applicable.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the MIME type for content data.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }
}
