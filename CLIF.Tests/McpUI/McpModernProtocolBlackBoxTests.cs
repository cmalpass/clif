using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace CLIF.Tests.McpUI;

[CollectionDefinition("McpModernProtocol", DisableParallelization = true)]
public sealed class McpModernProtocolCollection : ICollectionFixture<ModernMcpProcessFixture>
{
}

/// <summary>Black-box contract checks for the modern stateless MCP revision.</summary>
[Collection("McpModernProtocol")]
[Trait("Category", "McpUI")]
public sealed class McpModernProtocolBlackBoxTests
{
    private readonly ModernMcpProcessFixture _fixture;

    public McpModernProtocolBlackBoxTests(ModernMcpProcessFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ToolsList_ContainsModernEnvelopeMetadataAndCacheFields()
    {
        using var response = await _fixture.SendAsync("tools/list");
        var result = response.RootElement.GetProperty("result");

        Assert.Equal("complete", result.GetProperty("resultType").GetString());
        Assert.Equal(0, result.GetProperty("ttlMs").GetInt32());
        Assert.Equal("private", result.GetProperty("cacheScope").GetString());
        Assert.Equal("clif-mcp", result.GetProperty("_meta")
            .GetProperty("io.modelcontextprotocol/serverInfo")
            .GetProperty("name").GetString());
        Assert.Contains("clif_launch", result.GetProperty("tools")
            .EnumerateArray().Select(tool => tool.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task UnknownTool_IsProtocolInvalidParamsError()
    {
        using var response = await _fixture.SendAsync("tools/call", new { name = "clif_missing" });
        var error = response.RootElement.GetProperty("error");

        Assert.Equal(-32602, error.GetProperty("code").GetInt32());
        Assert.Contains("Unknown tool", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ToolCall_RejectsMissingRequiredArgumentsBeforeUiAutomation()
    {
        using var response = await _fixture.SendAsync("tools/call", new
        {
            name = "clif_snapshot",
            arguments = new { },
        });
        var result = response.RootElement.GetProperty("result");

        Assert.True(result.GetProperty("isError").GetBoolean());
        Assert.Contains("MCP_INVALID_PARAMS: missing required argument 'handle'", result
            .GetProperty("content")[0]
            .GetProperty("text").GetString());
    }

    [Fact]
    public async Task Discover_AdvertisesModernCapabilitiesAndSupportedRevision()
    {
        using var response = await _fixture.SendAsync("server/discover");
        var result = response.RootElement.GetProperty("result");

        Assert.Contains("2026-07-28", result.GetProperty("supportedVersions")
            .EnumerateArray().Select(version => version.GetString()));
        Assert.True(result.GetProperty("capabilities").GetProperty("tools")
            .GetProperty("listChanged").GetBoolean());
        Assert.Equal("complete", result.GetProperty("resultType").GetString());
    }

    [Fact]
    public async Task CancelledNotification_StopsAnInFlightToolWithoutAResponse()
    {
        var requestId = await _fixture.StartRequestAsync("tools/call", new
        {
            name = "clif_batch",
            arguments = new
            {
                actions = new[] { new { action = "wait", ms = 5_000 } },
            },
        });
        await Task.Delay(100);
        await _fixture.SendNotificationAsync("notifications/cancelled", new { requestId, reason = "conformance test" });

        using var response = await _fixture.TryReadAsync(TimeSpan.FromSeconds(2));
        Assert.Null(response);
    }
}

public sealed class ModernMcpProcessFixture : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _input;
    private readonly StreamReader _output;
    private int _nextId;

    public ModernMcpProcessFixture()
    {
        var executable = Environment.GetEnvironmentVariable(McpProcessFixture.McpExecutablePathVariable);
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
        {
            throw new InvalidOperationException("CLIF_MCP_EXE_PATH must point to a built MCP executable.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.Environment["CLIF_MCP_ALLOW_INPUT"] = "true";
        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start MCP executable.");
        _input = _process.StandardInput;
        _input.AutoFlush = true;
        _output = _process.StandardOutput;
    }

    public async Task<JsonDocument> SendAsync(string method, object? parameters = null)
    {
        var id = await StartRequestAsync(method, parameters);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var line = await _output.ReadLineAsync(timeout.Token)
            ?? throw new InvalidOperationException("MCP process closed stdout before responding.");
        var response = JsonDocument.Parse(line);
        Assert.Equal(id, response.RootElement.GetProperty("id").GetInt32());
        return response;
    }

    public async Task<int> StartRequestAsync(string method, object? parameters = null)
    {
        var id = Interlocked.Increment(ref _nextId);
        var requestParameters = new Dictionary<string, object?>
        {
            ["_meta"] = new Dictionary<string, object?>
            {
                ["io.modelcontextprotocol/protocolVersion"] = "2026-07-28",
                ["io.modelcontextprotocol/clientCapabilities"] = new { },
                ["io.modelcontextprotocol/clientInfo"] = new { name = "clif-modern-tests", version = "1.0" },
            },
        };
        if (parameters is not null)
        {
            using var parameterDocument = JsonSerializer.SerializeToDocument(parameters);
            foreach (var property in parameterDocument.RootElement.EnumerateObject())
            {
                requestParameters[property.Name] = property.Value.Clone();
            }
        }

        var request = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = requestParameters,
        };

        await _input.WriteLineAsync(JsonSerializer.Serialize(request));
        return id;
    }

    public async Task SendNotificationAsync(string method, object? parameters = null)
    {
        var notification = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
            ["params"] = parameters,
        };
        await _input.WriteLineAsync(JsonSerializer.Serialize(notification));
    }

    public async Task<JsonDocument?> TryReadAsync(TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            var line = await _output.ReadLineAsync(cancellation.Token);
            return line is null ? null : JsonDocument.Parse(line);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        try { _input.Close(); } catch (ObjectDisposedException) { }
        if (!_process.HasExited && !_process.WaitForExit(5000))
        {
            _process.Kill(entireProcessTree: true);
        }
        _process.Dispose();
    }
}
