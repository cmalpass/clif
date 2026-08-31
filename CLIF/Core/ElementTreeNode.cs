// <copyright file="ElementTreeNode.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

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
