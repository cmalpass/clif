// <copyright file="ScriptTarget.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Identifies the process targeted by a script.</summary>
public class ScriptTarget
{
    /// <summary>Gets or sets the target process name.</summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>Gets or sets the target window title.</summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>Gets or sets the target process identifier.</summary>
    public int ProcessId { get; set; }

    /// <summary>Gets or sets the target timeout in milliseconds.</summary>
    public int TimeoutMs { get; set; } = 30000;
}
