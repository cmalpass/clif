// <copyright file="ScriptExecutionResult.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Reports the outcome of script execution.</summary>
public class ScriptExecutionResult
{
    /// <summary>Gets or sets a value indicating whether gets or sets whether the script completed successfully.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the result message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution log entries.</summary>
    public List<string> ExecutionLog { get; set; } = new();

    /// <summary>Gets or sets values produced during execution.</summary>
    public Dictionary<string, object> Results { get; set; } = new();

    /// <summary>Gets or sets the total execution duration.</summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>Gets or sets the number of completed steps.</summary>
    public int StepsExecuted { get; set; }

    /// <summary>Gets or sets the number of failed steps.</summary>
    public int StepsFailed { get; set; }
}
