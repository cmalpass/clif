// <copyright file="ProcessIdValidator.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using CLIF.Validation.Rules;

namespace CLIF.Validation.Validators;

/// <summary>
/// Validator for process IDs.
/// </summary>
public class ProcessIdValidator : ValidatorBase<int>
{
    private const int MIN_PROCESS_ID = 1;
    private const int MAX_PROCESS_ID = 65535;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessIdValidator"/> class.
    /// </summary>
    public ProcessIdValidator()
    {
        this.AddRule(new RangeRule<int>(MIN_PROCESS_ID, MAX_PROCESS_ID));
        this.AddRule(new ProcessExistenceRule());
    }

    /// <summary>
    /// Validates the specified process ID.
    /// </summary>
    /// <param name="processId">The process ID to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(int processId)
    {
        var result = this.ValidateRules(processId);

        // Additional process-specific validation
        if (result.IsValid)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process.HasExited)
                {
                    result.AddError($"Process {processId} has already exited");
                }

                // Check if process has a main window (indicating it's a UI application)
                else if (process.MainWindowHandle == IntPtr.Zero)
                {
                    result.AddError($"Process {processId} does not have a user interface window");
                }
            }
            catch (ArgumentException)
            {
                result.AddError($"Process with ID {processId} does not exist");
            }
            catch (Exception ex)
            {
                result.AddError($"Error accessing process {processId}: {ex.Message}");
            }
        }

        return result;
    }
}
