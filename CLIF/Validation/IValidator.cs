using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CLIF.Validation.Rules;

namespace CLIF.Validation;

/// <summary>
/// Generic interface for validating objects of type T
/// </summary>
/// <typeparam name="T">The type of object to validate</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates the specified input
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result indicating success or failure</returns>
    ValidationResult Validate(T input);
    
    /// <summary>
    /// Asynchronously validates the specified input
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A task representing the validation operation</returns>
    Task<ValidationResult> ValidateAsync(T input);
}

/// <summary>
/// Non-generic interface for validating objects
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Validates the specified input of the given type
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <param name="inputType">The type of the input</param>
    /// <returns>A validation result indicating success or failure</returns>
    ValidationResult Validate(object input, Type inputType);
    
    /// <summary>
    /// Asynchronously validates the specified input of the given type
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <param name="inputType">The type of the input</param>
    /// <returns>A task representing the validation operation</returns>
    Task<ValidationResult> ValidateAsync(object input, Type inputType);
}

/// <summary>
/// Abstract base class for validators
/// </summary>
/// <typeparam name="T">The type of object to validate</typeparam>
public abstract class ValidatorBase<T> : IValidator<T>
{
    /// <summary>
    /// Collection of validation rules to apply
    /// </summary>
    protected readonly List<IValidationRule<T>> Rules = new();

    /// <summary>
    /// Validates the specified input
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result indicating success or failure</returns>
    public abstract ValidationResult Validate(T input);

    /// <summary>
    /// Asynchronously validates the specified input
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A task representing the validation operation</returns>
    public virtual async Task<ValidationResult> ValidateAsync(T input)
    {
        return await Task.FromResult(Validate(input));
    }

    /// <summary>
    /// Adds a validation rule to this validator
    /// </summary>
    /// <param name="rule">The validation rule to add</param>
    protected void AddRule(IValidationRule<T> rule)
    {
        Rules.Add(rule);
    }

    /// <summary>
    /// Validates the input against all configured rules
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result containing any rule violations</returns>
    protected ValidationResult ValidateRules(T input)
    {
        var result = ValidationResult.Success();

        foreach (var rule in Rules)
        {
            var ruleResult = rule.Validate(input);
            if (!ruleResult.IsValid)
            {
                result.AddErrors(ruleResult.Errors);
            }
        }

        return result;
    }

    /// <summary>
    /// Validates that the input is not null
    /// </summary>
    /// <param name="input">The input to check</param>
    /// <param name="paramName">The parameter name for error messages</param>
    /// <returns>A validation result</returns>
    protected ValidationResult ValidateNotNull(T? input, string paramName = "input")
    {
        if (input == null)
        {
            return ValidationResult.Failure($"{paramName} cannot be null", paramName);
        }
        return ValidationResult.Success();
    }
}