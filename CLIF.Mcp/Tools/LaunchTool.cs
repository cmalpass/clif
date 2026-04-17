// Licensed under the MIT License.

using System.Text.Json;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to launch a Windows application and return its window handle.
/// </summary>
public class LaunchTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;

    public LaunchTool(WindowSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override string Name => "clif_launch";

    public override string Description =>
        "Launch a Windows application and return its window handle. " +
        "Supports Win32, WPF, WinForms, and UWP applications.";

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

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var app = GetStringArgument(arguments, "app");
        if (string.IsNullOrEmpty(app))
        {
            return Task.FromResult(ErrorResult("Missing required argument: app"));
        }

        var args = GetArgument<string[]>(arguments, "args");

        try
        {
            var (handle, window) = _sessionManager.LaunchApp(app, args);
            return Task.FromResult(TextResult(
                $"Launched {app}\nWindow handle: {handle}\nTitle: {window.Title}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to launch {app}: {ex.Message}"));
        }
    }
}
