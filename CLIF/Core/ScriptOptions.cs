// <copyright file="ScriptOptions.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Controls script execution behavior.</summary>
public class ScriptOptions
{
    /// <summary>Gets or sets a value indicating whether gets or sets whether execution stops after an error.</summary>
    public bool StopOnError { get; set; } = true;

    /// <summary>Sets a value indicating whether maps the legacy continueOnError option to stopOnError when reading scripts.</summary>
    [JsonPropertyName("continueOnError")]
    public bool LegacyContinueOnError
    {
        set => this.StopOnError = !value;
    }

    /// <summary>Gets or sets a value indicating whether gets or sets whether individual steps are logged.</summary>
    public bool LogSteps { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether gets or sets whether screenshots are captured during execution.</summary>
    public bool TakeScreenshots { get; set; } = false;

    /// <summary>Gets or sets the default step timeout in milliseconds.</summary>
    public int DefaultTimeoutMs { get; set; } = 5000;

    /// <summary>Sets maps the legacy timeout option to defaultTimeoutMs when reading scripts.</summary>
    [JsonPropertyName("timeout")]
    public int LegacyTimeout
    {
        set => this.DefaultTimeoutMs = value;
    }

    /// <summary>Gets or sets the delay used between steps when one is not specified on the step.</summary>
    public int DelayBetweenActionsMs { get; set; }

    /// <summary>Sets maps the legacy delayBetweenActions option to the current delay property when reading scripts.</summary>
    [JsonPropertyName("delayBetweenActions")]
    public int LegacyDelayBetweenActions
    {
        set => this.DelayBetweenActionsMs = value;
    }

    /// <summary>Gets or sets the directory used for execution output.</summary>
    public string OutputPath { get; set; } = string.Empty;
}
