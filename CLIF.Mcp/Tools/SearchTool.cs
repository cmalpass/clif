// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
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

    public SearchTool(WindowSessionManager sessionManager, ElementRegistry elementRegistry)
    {
        _sessionManager = sessionManager;
        _elementRegistry = elementRegistry;
    }

    public override string Name => "clif_search_elements";

    public override string Description =>
        "Search for UI elements in a window matching specified criteria. " +
        "Returns matching elements with their refs, names, and control types. " +
        "Useful for finding elements without browsing the full snapshot tree.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            handle = new
            {
                type = "string",
                description = "Window handle to search in. If omitted, uses focused window.",
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
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var handle = GetStringArgument(arguments, "handle");
        var name = GetStringArgument(arguments, "name");
        var automationId = GetStringArgument(arguments, "automationId");
        var controlTypeFilter = GetStringArgument(arguments, "controlType");
        var className = GetStringArgument(arguments, "className");

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
                return Task.FromResult(ErrorResult("No window found. Use clif_launch or clif_list_windows first."));
            }

            // First take a snapshot to register all elements
            var snapshotBuilder = new SnapshotBuilder(_elementRegistry);
            snapshotBuilder.BuildSnapshot(handle!, window);

            // Now search through the tree
            var results = new List<string>();
            SearchElement(window, handle!, name, automationId, controlTypeFilter, className, results, 0);

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
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult($"Search failed: {ex.Message}"));
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
