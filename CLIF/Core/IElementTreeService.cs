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
