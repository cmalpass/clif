// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// MCP tool to search for elements in a window's UI tree by various criteria.
/// </summary>
public class SearchTool : ToolBase
{
    private const int MaxSearchDepth = 15;

    private readonly WindowSessionManager _sessionManager;
    private readonly ElementRegistry _elementRegistry;

    /// <summary>
    /// Initializes the element search tool.
    /// </summary>
    /// <param name="sessionManager">Manager used to resolve target windows.</param>
    /// <param name="elementRegistry">Registry used to store discovered elements.</param>
    public SearchTool(WindowSessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    /// <inheritdoc />
    public override string Name => "clif_search_elements";

    /// <inheritdoc />
    public override string Description =>
        "Search for UI elements in a window matching specified criteria. " +
        "Returns matching elements with their refs, names, and control types. " +
        "Useful for finding elements without browsing the full snapshot tree.";

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
                description = "Registered window handle to search in.",
            },
            name = new
            {
                type = "string",
                description = "Filter by element name (partial match)",
            },
            automationId = new
            {
                type = "string",
                description = "Filter by AutomationId (exact match)",
            },
            controlType = new
            {
                type = "string",
                description = "Filter by control type (e.g., 'Button', 'TextBox', 'ComboBox')",
            },
            className = new
            {
                type = "string",
                description = "Filter by class name (partial match)",
            },
        },
        required = new[] { "handle" },
    };

    /// <inheritdoc />
    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var name = GetStringArgument(arguments, "name");
        var automationId = GetStringArgument(arguments, "automationId");
        var controlTypeFilter = GetStringArgument(arguments, "controlType");
        var className = GetStringArgument(arguments, "className");

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

            // Search directly through the live tree. Do not rebuild the snapshot here,
            // because BuildSnapshot clears and regenerates the window registry, which
            // invalidates previously issued element refs as a hidden side effect.
            var results = new List<string>();
            SearchElement(window, handle, name, automationId, controlTypeFilter, className, results, 0);

            if (results.Count == 0)
            {
                return Task.FromResult(TextResult("No matching elements found."));
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found {results.Count} matching element(s):");
            foreach (var r in results)
            {
                sb.AppendLine(r);
            }

            return Task.FromResult(TextResult(sb.ToString()));
        }
        catch (Exception)
        {
            _sessionManager.InvalidateWindow(handle);
            _elementRegistry.RemoveWindow(handle);
            return Task.FromResult(ErrorResult(
                $"Window is no longer available: {handle}. Run clif_list_windows and acquire a new handle."));
        }
    }

    private void SearchElement(
        AutomationElement element,
        string windowHandle,
        string? name,
        string? automationId,
        string? controlTypeFilter,
        string? className,
        List<string> results,
        int depth)
    {
        if (depth > MaxSearchDepth) return;

        try
        {
            var matches = true;

            if (!string.IsNullOrEmpty(name))
            {
                var elName = element.Properties.Name.ValueOrDefault ?? string.Empty;
                if (!elName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    matches = false;
            }

            if (!string.IsNullOrEmpty(automationId))
            {
                var elId = element.Properties.AutomationId.ValueOrDefault ?? string.Empty;
                if (!elId.Equals(automationId, StringComparison.OrdinalIgnoreCase))
                    matches = false;
            }

            if (!string.IsNullOrEmpty(controlTypeFilter))
            {
                var elType = element.Properties.ControlType.ValueOrDefault.ToString();
                if (!elType.Equals(controlTypeFilter, StringComparison.OrdinalIgnoreCase))
                    matches = false;
            }

            if (!string.IsNullOrEmpty(className))
            {
                var elClass = element.Properties.ClassName.ValueOrDefault ?? string.Empty;
                if (!elClass.Contains(className, StringComparison.OrdinalIgnoreCase))
                    matches = false;
            }

            if (matches && (name != null || automationId != null || controlTypeFilter != null || className != null))
            {
                var refId = _elementRegistry.Register(windowHandle, element);
                var elName = element.Properties.Name.ValueOrDefault ?? "(unnamed)";
                var elType = element.Properties.ControlType.ValueOrDefault.ToString();
                var elId = element.Properties.AutomationId.ValueOrDefault;
                var idStr = !string.IsNullOrEmpty(elId) ? $" id=\"{elId}\"" : string.Empty;
                results.Add($"  - {elType} \"{elName}\"{idStr} [ref={refId}]");
            }

            var children = element.FindAllChildren();
            foreach (var child in children)
            {
                SearchElement(child, windowHandle, name, automationId, controlTypeFilter, className, results, depth + 1);
            }
        }
        catch
        {
            // Skip elements that throw
        }
    }
}
