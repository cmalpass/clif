// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.AutomationElements;
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
                minLength = 1,
                description = "Registered window handle (e.g., 'w1').",
            },
        },
        required = new[] { "handle" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        if (string.IsNullOrEmpty(handle))
        {
            return Task.FromResult(ErrorResult(
                "Missing required argument: handle. Use clif_launch or clif_list_windows first."));
        }

        try
        {
            var window = _sessionManager.GetWindow(handle);
            if (window == null)
            {
                return Task.FromResult(ErrorResult($"Window not found: {handle}"));
            }

            var snapshotBuilder = new SnapshotBuilder(_elementRegistry);
            var snapshot = snapshotBuilder.BuildSnapshot(handle, window);
            return Task.FromResult(TextResult(snapshot));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Failed to capture snapshot: {ex.Message}"));
        }
    }
}
