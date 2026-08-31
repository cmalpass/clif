// <copyright file="ElementSearchCriteria.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

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
