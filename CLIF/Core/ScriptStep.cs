// <copyright file="ScriptStep.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Describes one action in an automation script.</summary>
public class ScriptStep
{
    /// <summary>Gets or sets the action name.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the target element selector.</summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>Sets maps the legacy selector field to an automation-id selector when reading scripts.</summary>
    [JsonPropertyName("selector")]
    public string? LegacySelector
    {
        set => this.Element = string.IsNullOrWhiteSpace(value) || value.Contains('=') ? value ?? string.Empty : $"id={value}";
    }

    /// <summary>Gets or sets the primary action value.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Sets maps the legacy text field to the primary action value when reading scripts.</summary>
    [JsonPropertyName("text")]
    public string? LegacyText
    {
        set => this.Value = value ?? string.Empty;
    }

    /// <summary>Sets maps the legacy expectedValue field to the primary action value when reading scripts.</summary>
    [JsonPropertyName("expectedValue")]
    public string? LegacyExpectedValue
    {
        set => this.Value = value ?? string.Empty;
    }

    /// <summary>Gets or sets action-specific parameters.</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>Gets or sets a value indicating whether gets or sets whether errors for this step are ignored.</summary>
    public bool IgnoreErrors { get; set; } = false;

    /// <summary>Gets or sets the delay after the step, in milliseconds.</summary>
    public int DelayMs { get; set; } = 0;

    /// <summary>Sets maps the legacy duration field to the step delay when reading scripts.</summary>
    [JsonPropertyName("duration")]
    public int LegacyDuration
    {
        set => this.DelayMs = value;
    }

    /// <summary>Sets maps the legacy waitAfter field to the step delay when reading scripts.</summary>
    [JsonPropertyName("waitAfter")]
    public int LegacyWaitAfter
    {
        set => this.DelayMs = value;
    }

    /// <summary>Gets or sets a human-readable step description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Sets maps the legacy message field to the step description when reading scripts.</summary>
    [JsonPropertyName("message")]
    public string? LegacyMessage
    {
        set => this.Description = value ?? string.Empty;
    }

    /// <summary>Gets or sets the data-grid row index for a selectCell action.</summary>
    public int Row { get; set; }

    /// <summary>Gets or sets the data-grid column index for a selectCell action.</summary>
    public int Column { get; set; }
}
