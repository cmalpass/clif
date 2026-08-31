// Licensed under the MIT License.

using System.Text.Json;
using CLIF.Mcp.Core;
using CLIF.Mcp.Security;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to launch a Windows application and return its window handle.
/// </summary>
public class LaunchTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly McpSafetyPolicy _safetyPolicy;

    /// <summary>
    /// Initializes the window launch tool.
    /// </summary>
    /// <param name="sessionManager">Manager used to track launched windows.</param>
    /// <param name="safetyPolicy">Optional policy restricting launch operations.</param>
    public LaunchTool(WindowSessionManager sessionManager, McpSafetyPolicy? safetyPolicy = null)
    {
        _sessionManager = sessionManager;
        _safetyPolicy = safetyPolicy ?? McpSafetyPolicy.FromEnvironment();
    }

    /// <inheritdoc />
    public override string Name => "clif_launch";

    /// <inheritdoc />
    public override McpCapability RequiredCapability => McpCapability.Launch;

    /// <inheritdoc />
    public override string Description =>
        "Launch a Windows application and return its window handle. " +
        "Supports Win32, WPF, WinForms, and UWP applications.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            app = new
            {
                type = "string",
                description = "Application path or executable name (e.g., 'calc.exe', 'C:\\Program Files\\App\\app.exe')",
            },
            args = new
            {
                type = "array",
                items = new { type = "string" },
                description = "Optional command line arguments",
            },
        },
        required = new[] { "app" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var app = GetStringArgument(arguments, "app");
        if (string.IsNullOrEmpty(app))
        {
            return Task.FromResult(ErrorResult("Missing required argument: app"));
        }

        if (!_safetyPolicy.IsApplicationAllowed(app))
        {
            return Task.FromResult(ErrorResult(
                "Launch denied by policy. Add the executable name or path to CLIF_MCP_ALLOWED_APPS."));
        }

        var args = GetArgument<string[]>(arguments, "args");

        try
        {
            var (handle, window) = _sessionManager.LaunchApp(app, args);
            return Task.FromResult(TextResult(
                $"Launched {app}\nWindow handle: {handle}\nTitle: {window.Title}"));
        }
        catch (Exception)
        {
            return Task.FromResult(ErrorResult("Failed to launch the approved application."));
        }
    }

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(
        JsonElement? arguments,
        CancellationToken cancellationToken)
    {
        var app = GetStringArgument(arguments, "app");
        if (string.IsNullOrEmpty(app))
        {
            return Task.FromResult(ErrorResult("Missing required argument: app"));
        }

        if (!_safetyPolicy.IsApplicationAllowed(app))
        {
            return Task.FromResult(ErrorResult(
                "MCP_PERMISSION_DENIED: launch is not allowed by policy."));
        }

        var args = GetArgument<string[]>(arguments, "args");
        try
        {
            var (handle, window) = _sessionManager.LaunchApp(app, args, cancellationToken);
            return Task.FromResult(TextResult(
                $"Launched {app}\nWindow handle: {handle}\nTitle: {window.Title}"));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(ErrorResult("Failed to launch the approved application."));
        }
    }
}
