using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace CLIF.Tests.McpUI;

/// <summary>
/// Hosts the published MCP executable as an external stdio process for black-box tests.
/// </summary>
public sealed class McpProcessFixture : IDisposable
{
    public const string McpExecutablePathVariable = "CLIF_MCP_EXE_PATH";
    public const string WpfExecutablePathVariable = "TEST_WPF_APP_PATH";

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProcessExitTimeout = TimeSpan.FromSeconds(10);

    private readonly HashSet<int> _existingWpfProcessIds;
    private readonly HashSet<int> _launchedWpfProcessIds = [];
    private readonly StringBuilder _standardError = new();
    private readonly Process _mcpProcess;
    private readonly Task _standardErrorReader;
    private readonly StreamWriter _standardInput;
    private readonly StreamReader _standardOutput;
    private int _nextRequestId;
    private string? _launchedWindowHandle;
    private bool _disposed;

    /// <summary>
    /// Starts the MCP host with the fixture WPF application as its sole launch allowlist entry.
    /// Both executable paths must be supplied by the test environment.
    /// </summary>
    public McpProcessFixture()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("MCP UI black-box tests require a Windows desktop session.");
        }

        McpExecutablePath = RequireExecutablePath(McpExecutablePathVariable);
        WpfExecutablePath = RequireExecutablePath(WpfExecutablePathVariable);
        _existingWpfProcessIds = GetWpfProcesses().Select(process => process.Id).ToHashSet();

        var startInfo = new ProcessStartInfo
        {
            FileName = McpExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(McpExecutablePath)!,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        // The test proves that a real MCP host honors the explicit allowlist rather
        // than relying on an unrestricted development-machine configuration.
        startInfo.Environment["CLIF_MCP_ALLOWED_APPS"] = WpfExecutablePath;
        startInfo.Environment["CLIF_MCP_ALLOW_WINDOW_CLOSE"] = "true";

        _mcpProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start MCP executable '{McpExecutablePath}'.");
        _standardInput = _mcpProcess.StandardInput;
        _standardInput.AutoFlush = true;
        _standardOutput = _mcpProcess.StandardOutput;
        _standardErrorReader = DrainStandardErrorAsync(_mcpProcess.StandardError);
    }

    /// <summary>Gets the required compiled MCP executable path.</summary>
    public string McpExecutablePath { get; }

    /// <summary>Gets the required compiled WPF fixture executable path.</summary>
    public string WpfExecutablePath { get; }

    /// <summary>
    /// Completes MCP initialization and transitions the server to its active session state.
    /// </summary>
    public async Task InitializeAsync()
    {
        using var response = await SendRequestAsync("initialize", new
        {
            protocolVersion = "2025-06-18",
            capabilities = new { },
            clientInfo = new { name = "clif-mcp-ui-tests", version = "1.0.0" },
        });

        AssertNoProtocolError(response.RootElement);
        var result = response.RootElement.GetProperty("result");
        Assert.Equal("2025-06-18", result.GetProperty("protocolVersion").GetString());

        await SendNotificationAsync("notifications/initialized", new { });
    }

    /// <summary>Sends an MCP request and returns its JSON-RPC response.</summary>
    public async Task<JsonDocument> SendRequestAsync(string method, object? parameters = null)
    {
        ThrowIfDisposed();
        EnsureMcpProcessIsRunning();

        var id = Interlocked.Increment(ref _nextRequestId);
        var request = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
        };
        if (parameters is not null)
        {
            request["params"] = parameters;
        }

        await _standardInput.WriteLineAsync(JsonSerializer.Serialize(request));

        using var cancellation = new CancellationTokenSource(RequestTimeout);
        var responseLine = await _standardOutput.ReadLineAsync(cancellation.Token);
        if (responseLine is null)
        {
            throw new InvalidOperationException(
                $"MCP host exited before responding to '{method}'. {GetMcpDiagnostics()}");
        }

        var response = JsonDocument.Parse(responseLine);
        if (!response.RootElement.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id)
        {
            response.Dispose();
            throw new InvalidOperationException($"MCP returned an unexpected response id for '{method}'.");
        }

        return response;
    }

    /// <summary>Sends an MCP notification, which must not yield a JSON-RPC response.</summary>
    public async Task SendNotificationAsync(string method, object? parameters = null)
    {
        ThrowIfDisposed();
        EnsureMcpProcessIsRunning();

        var notification = new Dictionary<string, object?>
        {
            ["jsonrpc"] = "2.0",
            ["method"] = method,
        };
        if (parameters is not null)
        {
            notification["params"] = parameters;
        }

        await _standardInput.WriteLineAsync(JsonSerializer.Serialize(notification));
    }

    /// <summary>Calls a named MCP tool and returns its tool result JSON object.</summary>
    public Task<JsonDocument> CallToolAsync(string name, object? arguments = null) =>
        SendRequestAsync("tools/call", new { name, arguments });

    /// <summary>
    /// Records the launched window so cleanup can close it through MCP, then captures
    /// the newly created WPF process so it can be terminated if graceful shutdown fails.
    /// </summary>
    public async Task TrackLaunchedWpfApplicationAsync(string windowHandle)
    {
        _launchedWindowHandle = windowHandle;
        var deadline = DateTime.UtcNow + ProcessExitTimeout;

        while (DateTime.UtcNow < deadline)
        {
            foreach (var process in GetWpfProcesses())
            {
                if (!_existingWpfProcessIds.Contains(process.Id))
                {
                    _launchedWpfProcessIds.Add(process.Id);
                    return;
                }
            }

            await Task.Delay(100);
        }

        throw new InvalidOperationException(
            $"MCP launched window '{windowHandle}', but no new {Path.GetFileName(WpfExecutablePath)} process was found for cleanup.");
    }

    /// <summary>Throws a useful assertion failure when a JSON-RPC response is an error.</summary>
    public static void AssertNoProtocolError(JsonElement response)
    {
        if (!response.TryGetProperty("error", out var error)) return;

        throw new Xunit.Sdk.XunitException($"MCP protocol error: {error.GetRawText()}");
    }

    /// <summary>Asserts the tool result succeeded and returns all text content.</summary>
    public static string GetSuccessfulToolText(JsonElement response)
    {
        AssertNoProtocolError(response);
        var result = response.GetProperty("result");
        if (result.TryGetProperty("isError", out var isError) && isError.ValueKind == JsonValueKind.True)
        {
            throw new Xunit.Sdk.XunitException($"MCP tool failed: {result.GetRawText()}");
        }

        if (!result.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            throw new Xunit.Sdk.XunitException($"MCP tool result did not include content: {result.GetRawText()}");
        }

        return string.Join(
            Environment.NewLine,
            content.EnumerateArray()
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "text")
                .Select(item => item.TryGetProperty("text", out var text) ? text.GetString() : null)
                .Where(text => text is not null)!);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        TryCloseLaunchedWindow();
        TerminateLaunchedWpfProcesses();
        StopMcpProcess();

        GC.SuppressFinalize(this);
    }

    private static string RequireExecutablePath(string environmentVariable)
    {
        var configuredPath = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException(
                $"Required test path '{environmentVariable}' is not set. CI must provide a compiled Windows executable path.");
        }

        var fullPath = Path.GetFullPath(configuredPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Required test executable from '{environmentVariable}' was not found.", fullPath);
        }

        return fullPath;
    }

    private IEnumerable<Process> GetWpfProcesses()
    {
        var processName = Path.GetFileNameWithoutExtension(WpfExecutablePath);
        return Process.GetProcessesByName(processName);
    }

    private async Task DrainStandardErrorAsync(StreamReader errorReader)
    {
        while (await errorReader.ReadLineAsync() is { } line)
        {
            lock (_standardError)
            {
                _standardError.AppendLine(line);
            }
        }
    }

    private void TryCloseLaunchedWindow()
    {
        if (string.IsNullOrEmpty(_launchedWindowHandle) || _mcpProcess.HasExited) return;

        try
        {
            using var response = CallToolAsync("clif_close", new { handle = _launchedWindowHandle }).GetAwaiter().GetResult();
            _ = GetSuccessfulToolText(response.RootElement);
        }
        catch
        {
            // The process cleanup below is the recovery path when the MCP process is no longer usable.
        }
    }

    private void TerminateLaunchedWpfProcesses()
    {
        foreach (var processId in _launchedWpfProcessIds)
        {
            try
            {
                using var process = Process.GetProcessById(processId);
                if (process.HasExited) continue;

                process.CloseMainWindow();
                if (!process.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds))
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds);
                }
            }
            catch (ArgumentException)
            {
                // The process already exited.
            }
            catch (InvalidOperationException)
            {
                // The process already exited.
            }
        }
    }

    private void StopMcpProcess()
    {
        try { _standardInput.Close(); }
        catch { /* The MCP host may already be gone. */ }

        try
        {
            if (!_mcpProcess.HasExited && !_mcpProcess.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds))
            {
                _mcpProcess.Kill(entireProcessTree: true);
                _mcpProcess.WaitForExit((int)ProcessExitTimeout.TotalMilliseconds);
            }
        }
        catch (InvalidOperationException)
        {
            // The MCP host already exited.
        }
        finally
        {
            try { _standardErrorReader.Wait(ProcessExitTimeout); }
            catch { /* Diagnostic collection must not hide the test result. */ }
            _mcpProcess.Dispose();
        }
    }

    private void EnsureMcpProcessIsRunning()
    {
        if (_mcpProcess.HasExited)
        {
            throw new InvalidOperationException($"MCP host has exited. {GetMcpDiagnostics()}");
        }
    }

    private string GetMcpDiagnostics()
    {
        lock (_standardError)
        {
            return _standardError.Length == 0
                ? "No MCP stderr output was captured."
                : $"MCP stderr:{Environment.NewLine}{_standardError}";
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
