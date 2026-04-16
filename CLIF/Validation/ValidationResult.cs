using System;
using System.Collections.Generic;
using System.Linq;

namespace CLIF.Validation;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    /// <summary>
    /// Gets a value indicating whether the validation was successful
    /// </summary>
    public bool IsValid => !_errors.Any();

    /// <summary>
    /// Gets the collection of validation errors
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => _errors.AsReadOnly();

    /// <summary>
    /// Gets a combined error message from all validation errors
    /// </summary>
    public string ErrorMessage => string.Join("; ", _errors.Select(e => e.Message));

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>A successful validation result with no errors</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="field">The optional field name that failed validation</param>
    /// <returns>A failed validation result</returns>
    public static ValidationResult Failure(string message, string? field = null) =>
        new ValidationResult().AddError(message ?? string.Empty, field);

    /// <summary>
    /// Creates a failed validation result with a single error
    /// </summary>
    /// <param name="error">The validation error</param>
    /// <returns>A failed validation result</returns>
    public static ValidationResult Failure(ValidationError error) =>
        error != null
            ? new ValidationResult().AddError(error)
            : new ValidationResult().AddError(string.Empty);

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="field">The optional field name that failed validation</param>
    /// <returns>This validation result for method chaining</returns>
    public ValidationResult AddError(string message, string? field = null)
    {
        _errors.Add(new ValidationError(message ?? string.Empty, field));
        return this;
    }

    /// <summary>
    /// Adds an error to the validation result
    /// </summary>
    /// <param name="error">The validation error to add</param>
    /// <returns>This validation result for method chaining</returns>
    public ValidationResult AddError(ValidationError error)
    {
        _errors.Add(error ?? new ValidationError(string.Empty));
        return this;
    }

    /// <summary>
    /// Adds multiple errors to the validation result
    /// </summary>
    /// <param name="errors">The validation errors to add</param>
    /// <returns>This validation result for method chaining</returns>
    public ValidationResult AddErrors(IEnumerable<ValidationError> errors)
    {
        _errors.AddRange(errors);
        return this;
    }

    /// <summary>
    /// Combines this validation result with another validation result
    /// </summary>
    /// <param name="other">The other validation result to combine</param>
    /// <returns>This validation result for method chaining</returns>
    public ValidationResult Combine(ValidationResult other)
    {
        if (other != null && !other.IsValid)
        {
            _errors.AddRange(other.Errors);
        }
        return this;
    }

    public override string ToString()
    {
        return IsValid ? "Success" : ErrorMessage;
    }
}

/// <summary>
/// Represents a validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Initializes a new instance of the ValidationError class
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="field">The optional field name that failed validation</param>
    public ValidationError(string message, string? field = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Field = field;
    }

    /// <summary>
    /// Gets the error message
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the field name that failed validation (if applicable)
    /// </summary>
    public string? Field { get; }

    /// <summary>
    /// Returns a string representation of the validation error
    /// </summary>
    /// <returns>A string representation of the error</returns>
    public override string ToString()
    {
        return Field != null ? $"{Field}: {Message}" : Message;
    }
}