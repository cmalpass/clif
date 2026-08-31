// <copyright file="TreePrintOptions.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

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
