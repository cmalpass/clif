// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to capture an accessibility snapshot of a window's UI tree.
/// </summary>
public class SnapshotTool : ToolBase
{
    private readonly WindowSessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the snapshot tool.
    /// </summary>
    /// <param name="sessionManager">Manager used to resolve target windows.</param>
    /// <param name="elementRegistry">Registry used to register snapshot elements.</param>
    public SnapshotTool(WindowSessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_snapshot";

    /// <inheritdoc />
    public override string Description =>
        "Get an accessibility snapshot of a window's UI tree with semantic element references. " +
        "Returns a structured tree showing roles, names, refs, and states. " +
        "Use the returned refs with other tools to interact with elements.";

    /// <inheritdoc />
    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            handle = new
            {
                type = "string",
                description = "Window handle (e.g., 'w1'). If omitted, uses the focused window.",
            },
        },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");

        try
        {
            Window? window = null;

            if (!string.IsNullOrEmpty(handle))
            {
                window = _sessionManager.GetWindow(handle);
                if (window == null)
                {
                    return Task.FromResult(ErrorResult($"Window not found: {handle}"));
                }
            }
            else
            {
                // Try to use the focused window
                var focusedElement = _sessionManager.Automation.FocusedElement();
                if (focusedElement != null)
                {
                    var current = focusedElement;
                    while (current != null)
                    {
                        if (current.Properties.ControlType.ValueOrDefault == ControlType.Window)
                        {
                            window = current.AsWindow();
                            handle = _sessionManager.RegisterWindow(window);
                            break;
                        }

                        current = current.Parent;
                    }
                }
            }

            if (window == null)
            {
                return Task.FromResult(ErrorResult(
                    "No window found. Use clif_launch or clif_list_windows first."));
            }

            var snapshotBuilder = new SnapshotBuilder(_elementRegistry);
            var snapshot = snapshotBuilder.BuildSnapshot(handle!, window);
            return Task.FromResult(TextResult(snapshot));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture snapshot: {ex.Message}"));
        }
    }
}
