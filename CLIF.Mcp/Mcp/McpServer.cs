// Licensed under the MIT License.
// Inspired by FlaUI-MCP (https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

using System.Text.Json;

namespace CLIF.Mcp;

/// <summary>
/// MCP Server that handles JSON-RPC 2.0 over stdio.
/// </summary>
public class McpServer
{
    private readonly ToolRegistry _toolRegistry;

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
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, McpProtocol.JsonOptions);
                if (request == null) continue;

                var response = await HandleRequestAsync(request);
                if (response != null)
                {
                    var responseJson = JsonSerializer.Serialize(response, McpProtocol.JsonOptions);
                    await writer.WriteLineAsync(responseJson);
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error processing request: {ex.Message}");
            }
        }
    }

    private async Task<JsonRpcResponse?> HandleRequestAsync(JsonRpcRequest request)
    {
        try
        {
            object? result = request.Method switch
            {
                "initialize" => HandleInitialize(),
                "notifications/initialized" => null,
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolCallAsync(request),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}"),
            };

            if (result == null) return null;

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result,
            };
        }
        catch (Exception ex)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = -32601,
                    Message = ex.Message,
                },
            };
        }
    }

    private static McpInitializeResult HandleInitialize()
    {
        return new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
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

    private async Task<McpToolResult> HandleToolCallAsync(JsonRpcRequest request)
    {
        if (request.Params == null)
        {
            return ErrorResult("Missing params");
        }

        var callParams = JsonSerializer.Deserialize<McpToolCallParams>(
            request.Params.Value.GetRawText(),
            McpProtocol.JsonOptions);

        if (callParams == null)
        {
            return ErrorResult("Invalid tool call params");
        }

        return await _toolRegistry.ExecuteToolAsync(callParams.Name, callParams.Arguments);
    }

    private static McpToolResult ErrorResult(string message) => new()
    {
        Content = new List<McpContent>
        {
            new() { Type = "text", Text = message },
        },
        IsError = true,
    };
}
