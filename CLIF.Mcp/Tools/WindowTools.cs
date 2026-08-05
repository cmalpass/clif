// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using CLIF.Mcp.Core;
using CLIF.Mcp.Security;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to list all open windows on the desktop.
/// </summary>
public class ListWindowsTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly McpSafetyPolicy _safetyPolicy;

    public ListWindowsTool(WindowSessionManager sessionManager, McpSafetyPolicy? safetyPolicy = null)
    {
        _sessionManager = sessionManager;
        _safetyPolicy = safetyPolicy ?? McpSafetyPolicy.FromEnvironment();
    }

    public override string Name => "clif_list_windows";

    public override string Description =>
        "List all open windows on the desktop with their handles, titles, and process names.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new { },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        try
        {
            if (!_safetyPolicy.AllowWindowEnumeration)
            {
                return Task.FromResult(ErrorResult(
                    "Window enumeration is disabled by policy. Set CLIF_MCP_ALLOW_WINDOW_ENUMERATION=true to enable it."));
            }

            var windows = _sessionManager.ListWindows();
            if (windows.Count == 0)
            {
                return Task.FromResult(TextResult("No windows found."));
            }

            var lines = windows.Select(w =>
                $"- {w.handle}: \"{w.title}\" ({w.processName ?? "unknown"})");
            return Task.FromResult(TextResult(string.Join("\n", lines)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to list windows: {ex.Message}"));
        }
    }
}

/// <summary>
/// MCP tool to bring a window to the foreground.
/// </summary>
public class FocusWindowTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;

    public FocusWindowTool(WindowSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override string Name => "clif_focus";

    public override string Description =>
        "Bring a window to the foreground and give it focus.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            handle = new
            {
                type = "string",
                description = "Window handle (e.g., 'w1')",
            },
        },
        required = new[] { "handle" },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        try
        {
            if (string.IsNullOrEmpty(handle))
            {
                return Task.FromResult(ErrorResult("Missing required argument: handle"));
            }

            _sessionManager.FocusWindow(handle);
            return Task.FromResult(TextResult($"Focused window {handle}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to focus window: {ex.Message}"));
        }
    }
}

/// <summary>
/// MCP tool to close a window.
/// </summary>
public class CloseWindowTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly ElementRegistry? _elementRegistry;
    private readonly McpSafetyPolicy _safetyPolicy;

    public CloseWindowTool(
        WindowSessionManager sessionManager,
        ElementRegistry? elementRegistry = null,
        McpSafetyPolicy? safetyPolicy = null)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
        _safetyPolicy = safetyPolicy ?? McpSafetyPolicy.FromEnvironment();
    }

    public override string Name => "clif_close";

    public override string Description =>
        "Close a window by its handle.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            handle = new
            {
                type = "string",
                description = "Window handle to close (e.g., 'w1')",
            },
        },
        required = new[] { "handle" },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        if (string.IsNullOrEmpty(handle))
        {
            return Task.FromResult(ErrorResult("Missing required argument: handle"));
        }

        try
        {
            if (!_safetyPolicy.AllowWindowClose)
            {
                return Task.FromResult(ErrorResult(
                    "Window close is disabled by policy. Set CLIF_MCP_ALLOW_WINDOW_CLOSE=true to enable it."));
            }

            _sessionManager.CloseWindow(handle);
            _elementRegistry?.RemoveWindow(handle);
            return Task.FromResult(TextResult($"Closed window {handle}"));
        }
        catch (Exception)
        {
            return Task.FromResult(ErrorResult("Failed to close the window."));
        }
    }
}
