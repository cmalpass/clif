// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using System.Globalization;
using System.Text.Json;

namespace CLIF.Mcp;

/// <summary>
/// MCP Server that handles JSON-RPC 2.0 over stdio.
/// </summary>
public class McpServer
{
    private readonly ToolRegistry _toolRegistry;
    private McpSessionState _sessionState = McpSessionState.Uninitialized;

    public McpServer(ToolRegistry toolRegistry)
    {
        _toolRegistry = toolRegistry;
    }

    /// <summary>
    /// Run the MCP server, reading JSON-RPC requests from stdin and writing responses to stdout.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var reader = Console.In;
        var writer = Console.Out;

        // Redirect stderr for logging (MCP servers must not write non-JSON-RPC to stdout)
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                if (!TryDeserializeRequest(line, out var request, out var errorResponse))
                {
                    await WriteResponseAsync(writer, errorResponse!);
                    continue;
                }

                var response = await HandleRequestAsync(request!);
                if (response != null)
                {
                    await WriteResponseAsync(writer, response);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                await WriteResponseAsync(writer, CreateError(null, JsonRpcErrors.InternalError, "Internal error."));
            }
        }
    }

    private async Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request)
    {
        if (!request.HasId)
        {
            await HandleNotificationAsync(request);
            return null;
        }

        if (!IsValidRequest(request))
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidRequest, "Invalid request.");
        }

        try
        {
            return request.Method switch
            {
                "initialize" => HandleInitializeRequest(request),
                "notifications/initialized" => CreateError(request.Id, JsonRpcErrors.InvalidRequest, "notifications/initialized must not include an id."),
                "tools/list" => HandleToolsListRequest(request),
                "tools/call" => await HandleToolCallRequestAsync(request),
                _ => CreateError(request.Id, JsonRpcErrors.MethodNotFound, "Method not found."),
            };
        }
        catch
        {
            return CreateError(request.Id, JsonRpcErrors.InternalError, "Internal error.");
        }
    }

    private async Task HandleNotificationAsync(JsonRpcRequest request)
    {
        if (!IsValidRequest(request)) return;

        if (request.Method == "notifications/initialized")
        {
            if (_sessionState == McpSessionState.AwaitingInitialized)
            {
                _sessionState = McpSessionState.Active;
            }

            return;
        }

        if (_sessionState != McpSessionState.Active) return;

        if (request.Method == "tools/call" && TryGetToolCallParams(request, out var callParams))
        {
            await _toolRegistry.ExecuteToolAsync(callParams.Name, callParams.Arguments);
        }
    }

    private JsonRpcResponse HandleInitializeRequest(JsonRpcRequest request)
    {
        if (_sessionState != McpSessionState.Uninitialized)
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidRequest, "Session is already initialized.");
        }

        if (!TryGetInitializeParams(request, out _))
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidParams, "Invalid initialize parameters.");
        }

        _sessionState = McpSessionState.AwaitingInitialized;
        return Success(request.Id, HandleInitialize());
    }

    private static McpInitializeResult HandleInitialize()
    {
        return new McpInitializeResult
        {
            ProtocolVersion = McpProtocol.SupportedProtocolVersion,
            Capabilities = new McpCapabilities
            {
                Tools = new ToolsCapability { ListChanged = false },
            },
            ServerInfo = new McpServerInfo
            {
                Name = "clif-mcp",
                Version = "0.1.0",
            },
        };
    }

    private McpToolsListResult HandleToolsList()
    {
        return new McpToolsListResult
        {
            Tools = _toolRegistry.GetToolDefinitions(),
        };
    }

    private JsonRpcResponse HandleToolsListRequest(JsonRpcRequest request)
    {
        if (_sessionState != McpSessionState.Active)
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidRequest, "Session is not active.");
        }

        if (request.Params.HasValue && request.Params.Value.ValueKind != JsonValueKind.Object)
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidParams, "Invalid tools/list parameters.");
        }

        return Success(request.Id, HandleToolsList());
    }

    private async Task<JsonRpcResponse> HandleToolCallRequestAsync(JsonRpcRequest request)
    {
        if (_sessionState != McpSessionState.Active)
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidRequest, "Session is not active.");
        }

        if (!TryGetToolCallParams(request, out var callParams))
        {
            return CreateError(request.Id, JsonRpcErrors.InvalidParams, "Invalid tools/call parameters.");
        }

        return Success(request.Id, await _toolRegistry.ExecuteToolAsync(callParams.Name, callParams.Arguments));
    }

    private static bool TryDeserializeRequest(string line, out JsonRpcRequest? request, out JsonRpcResponse? errorResponse)
    {
        request = null;
        errorResponse = null;

        try
        {
            using var document = JsonDocument.Parse(line);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errorResponse = CreateError(null, JsonRpcErrors.InvalidRequest, "Invalid request.");
                return false;
            }

            request = JsonSerializer.Deserialize<JsonRpcRequest>(line, McpProtocol.JsonOptions);
            if (request == null)
            {
                errorResponse = CreateError(null, JsonRpcErrors.InvalidRequest, "Invalid request.");
                return false;
            }

            request.HasId = document.RootElement.TryGetProperty("id", out _);
            return true;
        }
        catch (JsonException)
        {
            errorResponse = CreateError(null, JsonRpcErrors.ParseError, "Parse error.");
            return false;
        }
    }

    private static bool IsValidRequest(JsonRpcRequest request)
    {
        if (request.JsonRpc != "2.0" || string.IsNullOrWhiteSpace(request.Method)) return false;
        if (!request.HasId) return true;
        if (!request.Id.HasValue) return false;

        return request.Id.Value.ValueKind is JsonValueKind.String or JsonValueKind.Number or JsonValueKind.Null;
    }

    private static bool TryGetInitializeParams(JsonRpcRequest request, out McpInitializeParams initializeParams)
    {
        initializeParams = new McpInitializeParams();
        if (!request.Params.HasValue || request.Params.Value.ValueKind != JsonValueKind.Object) return false;

        try
        {
            initializeParams = JsonSerializer.Deserialize<McpInitializeParams>(request.Params.Value.GetRawText(), McpProtocol.JsonOptions) ?? new McpInitializeParams();
            return IsProtocolVersion(initializeParams.ProtocolVersion) &&
                   initializeParams.Capabilities.HasValue && initializeParams.Capabilities.Value.ValueKind == JsonValueKind.Object &&
                   initializeParams.ClientInfo is { Name.Length: > 0, Version.Length: > 0 };
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsProtocolVersion(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static bool TryGetToolCallParams(JsonRpcRequest request, out McpToolCallParams callParams)
    {
        callParams = new McpToolCallParams();
        if (!request.Params.HasValue || request.Params.Value.ValueKind != JsonValueKind.Object) return false;

        try
        {
            callParams = JsonSerializer.Deserialize<McpToolCallParams>(request.Params.Value.GetRawText(), McpProtocol.JsonOptions) ?? new McpToolCallParams();
            return !string.IsNullOrWhiteSpace(callParams.Name) &&
                   (!callParams.Arguments.HasValue || callParams.Arguments.Value.ValueKind == JsonValueKind.Object);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonRpcResponse Success(JsonElement? id, object result) => new() { Id = id, Result = result };

    private static JsonRpcResponse CreateError(JsonElement? id, int code, string message) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message },
    };

    private static async Task WriteResponseAsync(TextWriter writer, JsonRpcResponse response)
    {
        var responseJson = JsonSerializer.Serialize(response, McpProtocol.JsonOptions);
        await writer.WriteLineAsync(responseJson);
        await writer.FlushAsync();
    }

    private enum McpSessionState
    {
        Uninitialized,
        AwaitingInitialized,
        Active,
    }

    private static class JsonRpcErrors
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}
