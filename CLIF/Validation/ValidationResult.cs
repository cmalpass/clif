// <copyright file="ValidationResult.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CLIF.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly List<ValidationError> errors = new();

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => !this.errors.Any();

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors => this.errors.AsReadOnly();

    /// <summary>
    /// Gets a combined error message from all validation errors.
    /// </summary>
    public string ErrorMessage => string.Join("; ", this.errors.Select(e => e.Message));

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result with no errors.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="field">The optional field name that failed validation.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(string message, string? field = null) =>
        new ValidationResult().AddError(message ?? string.Empty, field);

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Failure(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ValidationResult().AddError(error);
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="field">The optional field name that failed validation.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ValidationResult AddError(string message, string? field = null)
    {
        this.errors.Add(new ValidationError(message ?? string.Empty, field));
        return this;
    }

    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The validation error to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ValidationResult AddError(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        this.errors.Add(error);
        return this;
    }

    /// <summary>
    /// Adds multiple errors to the validation result.
    /// </summary>
    /// <param name="errors">The validation errors to add.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ValidationResult AddErrors(IEnumerable<ValidationError> errors)
    {
        this.errors.AddRange(errors);
        return this;
    }

    /// <summary>
    /// Combines this validation result with another validation result.
    /// </summary>
    /// <param name="other">The other validation result to combine.</param>
    /// <returns>This validation result for method chaining.</returns>
    public ValidationResult Combine(ValidationResult other)
    {
        if (other != null && !other.IsValid)
        {
            this.errors.AddRange(other.Errors);
        }

        return this;
    }

    /// <summary>
    /// Returns a human-readable representation of the validation result.
    /// </summary>
    /// <returns><c>Success</c> when valid; otherwise the combined error message.</returns>
    public override string ToString()
    {
        return this.IsValid ? "Success" : this.ErrorMessage;
    }
}

/// <summary>
/// Represents a validation error.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="field">The optional field name that failed validation.</param>
    public ValidationError(string message, string? field = null)
    {
        this.Message = message ?? throw new ArgumentNullException(nameof(message));
        this.Field = field;
    }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the field name that failed validation (if applicable).
    /// </summary>
    public string? Field { get; }

    /// <summary>
    /// Returns a string representation of the validation error.
    /// </summary>
    /// <returns>A string representation of the error.</returns>
    public override string ToString()
    {
        return this.Field != null ? $"{this.Field}: {this.Message}" : this.Message;
    }
}
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
