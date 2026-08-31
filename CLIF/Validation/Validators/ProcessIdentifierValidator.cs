// <copyright file="ProcessIdentifierValidator.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Linq;
using CLIF.Validation.Rules;

namespace CLIF.Validation.Validators;

/// <summary>
/// Validator for validating process names or process ID strings.
/// </summary>
public class ProcessIdentifierValidator : ValidatorBase<string>
{
    private readonly ProcessIdValidator processIdValidator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessIdentifierValidator"/> class.
    /// </summary>
    public ProcessIdentifierValidator()
    {
        this.processIdValidator = new ProcessIdValidator();
        this.AddRule(new LengthRule(1, 255));
        this.AddRule(new InvalidCharactersRule());
    }

    /// <summary>
    /// Validates the specified process identifier (name or ID).
    /// </summary>
    /// <param name="processIdentifier">The process identifier to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string processIdentifier)
    {
        if (string.IsNullOrWhiteSpace(processIdentifier))
        {
            return ValidationResult.Failure("Process identifier cannot be empty");
        }

        var result = this.ValidateRules(processIdentifier);

        // If it's a numeric ID, validate as process ID
        if (int.TryParse(processIdentifier, out var processId))
        {
            var idResult = this.processIdValidator.Validate(processId);
            result.Combine(idResult);
        }
        else
        {
            // Validate as process name
            result.Combine(this.ValidateProcessName(processIdentifier));
        }

        return result;
    }

    /// <summary>
    /// Validates a process name.
    /// </summary>
    /// <param name="processName">The process name to validate.</param>
    /// <returns>A validation result.</returns>
    private ValidationResult ValidateProcessName(string processName)
    {
        var result = ValidationResult.Success();

        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                result.AddError($"No process found with name '{processName}'");
            }
            else
            {
                // Check if any of the processes have UI windows
                var hasUiProcess = processes.Any(p => p.MainWindowHandle != IntPtr.Zero);
                if (!hasUiProcess)
                {
                    result.AddError($"Process '{processName}' does not have a user interface window");
                }
            }

            // Dispose all process objects
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Error validating process name '{processName}': {ex.Message}");
        }

        return result;
    }
}
