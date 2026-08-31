// <copyright file="Script.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Represents a serializable CLIF automation script.</summary>
public class Script
{
    /// <summary>Gets or sets the script name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the script description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the script version.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>Gets or sets the process targeting settings.</summary>
    public ScriptTarget Target { get; set; } = new();

    /// <summary>Sets maps the legacy targetProcess field to the current target.processName field when reading scripts.</summary>
    [JsonPropertyName("targetProcess")]
    public string? LegacyTargetProcess
    {
        set => this.Target.ProcessName = value ?? string.Empty;
    }

    /// <summary>Gets or sets the ordered script steps.</summary>
    public List<ScriptStep> Steps { get; set; } = new();

    /// <summary>Gets or sets variables available to script steps.</summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>Gets or sets execution options.</summary>
    public ScriptOptions Options { get; set; } = new();
}
