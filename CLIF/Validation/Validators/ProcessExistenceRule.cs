// <copyright file="ProcessExistenceRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using CLIF.Validation.Rules;

namespace CLIF.Validation.Validators;

/// <summary>
/// Validation rule to check if a process exists.
/// </summary>
public class ProcessExistenceRule : ValidationRule<int>
{
    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "ProcessExistence";

    /// <summary>
    /// Validates that a process with the specified ID exists.
    /// </summary>
    /// <param name="processId">The process ID to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return this.Success();
        }
        catch (ArgumentException)
        {
            return this.Failure($"Process with ID {processId} does not exist");
        }
        catch (Exception ex)
        {
            return this.Failure($"Error accessing process {processId}: {ex.Message}");
        }
    }
}
