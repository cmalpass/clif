// <copyright file="IElementTreeService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

/// <summary>Builds, prints, and searches UI Automation element trees.</summary>
public interface IElementTreeService
{
    /// <summary>Builds a tree rooted at an automation element.</summary>
    /// <param name="rootElement">Element at which to start.</param>
    /// <param name="includeChildren">Whether child elements should be included.</param>
    /// <param name="maxDepth">Maximum depth to traverse.</param>
    /// <returns>The constructed tree.</returns>
    Task<ElementTreeNode> BuildTreeAsync(AutomationElement rootElement, bool includeChildren = true, int maxDepth = 10);

    /// <summary>Formats a tree for display.</summary>
    /// <param name="root">Tree root to print.</param>
    /// <param name="options">Optional filtering and display settings.</param>
    /// <returns>The formatted tree text.</returns>
    Task<string> PrintTreeAsync(ElementTreeNode root, TreePrintOptions? options = null);

    /// <summary>Finds nodes matching search criteria.</summary>
    /// <param name="root">Tree root to search.</param>
    /// <param name="criteria">Criteria applied to each node.</param>
    /// <returns>Matching nodes in traversal order.</returns>
    Task<List<ElementTreeNode>> SearchTreeAsync(ElementTreeNode root, ElementSearchCriteria criteria);

    /// <summary>Finds a node in a tree by selector.</summary>
    /// <param name="root">Tree root to search.</param>
    /// <param name="selector">Selector identifying the desired node.</param>
    /// <returns>The matching node, or <see langword="null"/> when none is found.</returns>
    Task<ElementTreeNode?> FindElementInTreeAsync(ElementTreeNode root, string selector);
}

/// <summary>Represents one node in an automation element tree.</summary>
public class ElementTreeNode
{
    /// <summary>Gets or sets the visible element name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the automation identifier.</summary>
    public string AutomationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the element class name.</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Gets or sets the control type.</summary>
    public string ControlType { get; set; } = string.Empty;

    /// <summary>Gets or sets the localized control type.</summary>
    public string LocalizedControlType { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether gets or sets whether the element is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether gets or sets whether the element is visible.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Gets or sets the element bounds as formatted text.</summary>
    public string BoundingRectangle { get; set; } = string.Empty;

    /// <summary>Gets or sets the value exposed by the element.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning process identifier.</summary>
    public int ProcessId { get; set; }

    /// <summary>Gets or sets the child nodes.</summary>
    public List<ElementTreeNode> Children { get; set; } = new();

    /// <summary>Gets or sets the underlying automation element.</summary>
    public AutomationElement? Element { get; set; }

    /// <summary>Gets or sets the node depth relative to the tree root.</summary>
    public int Depth { get; set; }

    /// <summary>Gets or sets the selector that identifies this node.</summary>
    public string Selector { get; set; } = string.Empty;
}

/// <summary>Controls how an automation tree is printed.</summary>
public class TreePrintOptions
{
    /// <summary>Gets or sets a value indicating whether gets or sets whether element properties are printed.</summary>
    public bool ShowProperties { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether gets or sets whether disabled elements are omitted.</summary>
    public bool ShowOnlyEnabled { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether gets or sets whether invisible elements are omitted.</summary>
    public bool ShowOnlyVisible { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether gets or sets whether bounds are printed.</summary>
    public bool ShowBoundingRectangle { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether gets or sets whether process identifiers are printed.</summary>
    public bool ShowProcessId { get; set; } = false;

    /// <summary>Gets or sets a value indicating whether gets or sets whether selectors are printed.</summary>
    public bool ShowSelector { get; set; } = true;

    /// <summary>Gets or sets the maximum printed depth.</summary>
    public int MaxDepth { get; set; } = 10;

    /// <summary>Gets or sets control types to include.</summary>
    public List<string> IncludeControlTypes { get; set; } = new();

    /// <summary>Gets or sets control types to exclude.</summary>
    public List<string> ExcludeControlTypes { get; set; } = new();
}

/// <summary>Defines filters used when searching an automation tree.</summary>
public class ElementSearchCriteria
{
    /// <summary>Gets or sets the name filter.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the automation identifier filter.</summary>
    public string? AutomationId { get; set; }

    /// <summary>Gets or sets the class name filter.</summary>
    public string? ClassName { get; set; }

    /// <summary>Gets or sets the control type filter.</summary>
    public string? ControlType { get; set; }

    /// <summary>Gets or sets the enabled-state filter.</summary>
    public bool? IsEnabled { get; set; }

    /// <summary>Gets or sets the visible-state filter.</summary>
    public bool? IsVisible { get; set; }

    /// <summary>Gets or sets a substring filter for element values.</summary>
    public string? ValueContains { get; set; }

    /// <summary>Gets or sets a value indicating whether gets or sets whether text filters are interpreted as regular expressions.</summary>
    public bool UseRegex { get; set; } = false;
}
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
