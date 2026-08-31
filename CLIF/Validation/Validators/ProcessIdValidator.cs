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
    /// Initializes a new instance of the ProcessIdValidator class.
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

/// <summary>
/// Validator for validating process names or process ID strings.
/// </summary>
public class ProcessIdentifierValidator : ValidatorBase<string>
{
    private readonly ProcessIdValidator _processIdValidator;

    /// <summary>
    /// Initializes a new instance of the ProcessIdentifierValidator class.
    /// </summary>
    public ProcessIdentifierValidator()
    {
        this._processIdValidator = new ProcessIdValidator();
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
            var idResult = this._processIdValidator.Validate(processId);
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