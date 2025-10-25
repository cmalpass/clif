using FlaUI.Core.AutomationElements;
using Microsoft.Extensions.Logging;
using CLIF.Core;
using System.Text;
using System.Text.RegularExpressions;

namespace CLIF.Services;

public class ElementTreeService : IElementTreeService
{
    private readonly ILogger<ElementTreeService> _logger;

    public ElementTreeService(ILogger<ElementTreeService> logger)
    {
        _logger = logger;
    }

    public async Task<ElementTreeNode> BuildTreeAsync(AutomationElement rootElement, bool includeChildren = true, int maxDepth = 10)
    {
        return await Task.Run(() =>
        {
            try
            {
                return BuildTreeNode(rootElement, 0, maxDepth, includeChildren);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building element tree");
                return new ElementTreeNode();
            }
        });
    }

    public async Task<string> PrintTreeAsync(ElementTreeNode root, TreePrintOptions? options = null)
    {
        return await Task.Run(() =>
        {
            options ??= new TreePrintOptions();
            var sb = new StringBuilder();
            PrintTreeNode(root, sb, "", true, options, 0);
            return sb.ToString();
        });
    }

    public async Task<List<ElementTreeNode>> SearchTreeAsync(ElementTreeNode root, ElementSearchCriteria criteria)
    {
        return await Task.Run(() =>
        {
            var results = new List<ElementTreeNode>();
            SearchTreeNode(root, criteria, results);
            return results;
        });
    }

    public async Task<ElementTreeNode?> FindElementInTreeAsync(ElementTreeNode root, string selector)
    {
        return await Task.Run(() =>
        {
            return FindElementInTreeNode(root, selector);
        });
    }

    private ElementTreeNode BuildTreeNode(AutomationElement element, int currentDepth, int maxDepth, bool includeChildren)
    {
        var node = new ElementTreeNode
        {
            Element = element,
            Depth = currentDepth,
            Name = element.Name ?? string.Empty,
            AutomationId = element.AutomationId ?? string.Empty,
            ClassName = element.ClassName ?? string.Empty,
            ControlType = element.ControlType.ToString(),
            LocalizedControlType = element.ControlType.ToString(),
            IsEnabled = element.IsEnabled,
            IsVisible = !element.IsOffscreen,
            BoundingRectangle = element.BoundingRectangle.ToString(),
            ProcessId = element.Properties.ProcessId.ValueOrDefault,
            Selector = GenerateSelector(element)
        };

        // Get value if available
        try
        {
            if (element.Patterns.Value.TryGetPattern(out var valuePattern))
            {
                node.Value = valuePattern.Value ?? string.Empty;
            }
        }
        catch { /* Ignore pattern access errors */ }

        // Build children if requested and within depth limit
        if (includeChildren && currentDepth < maxDepth)
        {
            try
            {
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    try
                    {
                        var childNode = BuildTreeNode(child, currentDepth + 1, maxDepth, includeChildren);
                        node.Children.Add(childNode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Error processing child element: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting children for element: {ex.Message}");
            }
        }

        return node;
    }

    private void PrintTreeNode(ElementTreeNode node, StringBuilder sb, string prefix, bool isLast, TreePrintOptions options, int currentDepth)
    {
        if (currentDepth > options.MaxDepth) return;

        // Apply filters
        if (options.ShowOnlyEnabled && !node.IsEnabled) return;
        if (options.ShowOnlyVisible && !node.IsVisible) return;
        if (options.IncludeControlTypes.Any() && !options.IncludeControlTypes.Contains(node.ControlType)) return;
        if (options.ExcludeControlTypes.Contains(node.ControlType)) return;

        // Build the tree line
        var connector = isLast ? "└── " : "├── ";
        var displayName = string.IsNullOrEmpty(node.Name) ? $"<{node.ControlType}>" : node.Name;
        
        sb.AppendLine($"{prefix}{connector}{displayName}");

        if (options.ShowProperties)
        {
            var propertyPrefix = prefix + (isLast ? "    " : "│   ");
            
            if (!string.IsNullOrEmpty(node.AutomationId))
                sb.AppendLine($"{propertyPrefix}  AutomationId: {node.AutomationId}");
            
            if (!string.IsNullOrEmpty(node.ClassName))
                sb.AppendLine($"{propertyPrefix}  ClassName: {node.ClassName}");
            
            sb.AppendLine($"{propertyPrefix}  ControlType: {node.ControlType}");
            sb.AppendLine($"{propertyPrefix}  Enabled: {node.IsEnabled}");
            sb.AppendLine($"{propertyPrefix}  Visible: {node.IsVisible}");
            
            if (!string.IsNullOrEmpty(node.Value))
                sb.AppendLine($"{propertyPrefix}  Value: {node.Value}");

            if (options.ShowBoundingRectangle)
                sb.AppendLine($"{propertyPrefix}  BoundingRect: {node.BoundingRectangle}");

            if (options.ShowProcessId)
                sb.AppendLine($"{propertyPrefix}  ProcessId: {node.ProcessId}");

            if (options.ShowSelector && !string.IsNullOrEmpty(node.Selector))
                sb.AppendLine($"{propertyPrefix}  Selector: {node.Selector}");
        }

        // Print children
        var childPrefix = prefix + (isLast ? "    " : "│   ");
        for (int i = 0; i < node.Children.Count; i++)
        {
            var isLastChild = i == node.Children.Count - 1;
            PrintTreeNode(node.Children[i], sb, childPrefix, isLastChild, options, currentDepth + 1);
        }
    }

    private void SearchTreeNode(ElementTreeNode node, ElementSearchCriteria criteria, List<ElementTreeNode> results)
    {
        if (MatchesCriteria(node, criteria))
        {
            results.Add(node);
        }

        foreach (var child in node.Children)
        {
            SearchTreeNode(child, criteria, results);
        }
    }

    private ElementTreeNode? FindElementInTreeNode(ElementTreeNode node, string selector)
    {
        if (node.Selector == selector || node.Name == selector || node.AutomationId == selector)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var result = FindElementInTreeNode(child, selector);
            if (result != null) return result;
        }

        return null;
    }

    private bool MatchesCriteria(ElementTreeNode node, ElementSearchCriteria criteria)
    {
        if (!string.IsNullOrEmpty(criteria.Name))
        {
            if (criteria.UseRegex)
            {
                if (!Regex.IsMatch(node.Name, criteria.Name, RegexOptions.IgnoreCase))
                    return false;
            }
            else
            {
                if (!node.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        if (!string.IsNullOrEmpty(criteria.AutomationId) && 
            !node.AutomationId.Contains(criteria.AutomationId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(criteria.ClassName) && 
            !node.ClassName.Contains(criteria.ClassName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(criteria.ControlType) && 
            !node.ControlType.Contains(criteria.ControlType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (criteria.IsEnabled.HasValue && node.IsEnabled != criteria.IsEnabled.Value)
            return false;

        if (criteria.IsVisible.HasValue && node.IsVisible != criteria.IsVisible.Value)
            return false;

        if (!string.IsNullOrEmpty(criteria.ValueContains) && 
            !node.Value.Contains(criteria.ValueContains, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private string GenerateSelector(AutomationElement element)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(element.AutomationId))
            return $"id={element.AutomationId}";

        if (!string.IsNullOrEmpty(element.Name))
            parts.Add($"name={element.Name}");

        if (!string.IsNullOrEmpty(element.ClassName))
            parts.Add($"class={element.ClassName}");

        parts.Add($"type={element.ControlType}");

        return string.Join(" and ", parts);
    }
}