using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

public interface IElementTreeService
{
    Task<ElementTreeNode> BuildTreeAsync(AutomationElement rootElement, bool includeChildren = true, int maxDepth = 10);
    Task<string> PrintTreeAsync(ElementTreeNode root, TreePrintOptions? options = null);
    Task<List<ElementTreeNode>> SearchTreeAsync(ElementTreeNode root, ElementSearchCriteria criteria);
    Task<ElementTreeNode?> FindElementInTreeAsync(ElementTreeNode root, string selector);
}

public class ElementTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string LocalizedControlType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsVisible { get; set; }
    public string BoundingRectangle { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public List<ElementTreeNode> Children { get; set; } = new();
    public AutomationElement? Element { get; set; }
    public int Depth { get; set; }
    public string Selector { get; set; } = string.Empty;
}

public class TreePrintOptions
{
    public bool ShowProperties { get; set; } = true;
    public bool ShowOnlyEnabled { get; set; } = false;
    public bool ShowOnlyVisible { get; set; } = false;
    public bool ShowBoundingRectangle { get; set; } = false;
    public bool ShowProcessId { get; set; } = false;
    public bool ShowSelector { get; set; } = true;
    public int MaxDepth { get; set; } = 10;
    public List<string> IncludeControlTypes { get; set; } = new();
    public List<string> ExcludeControlTypes { get; set; } = new();
}

public class ElementSearchCriteria
{
    public string? Name { get; set; }
    public string? AutomationId { get; set; }
    public string? ClassName { get; set; }
    public string? ControlType { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsVisible { get; set; }
    public string? ValueContains { get; set; }
    public bool UseRegex { get; set; } = false;
}