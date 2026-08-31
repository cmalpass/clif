// <copyright file="ValidationRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Abstract base class for validation rules.
/// </summary>
/// <typeparam name="T">The type of input to validate.</typeparam>
public abstract class ValidationRule<T> : IValidationRule<T>
{
    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public abstract string RuleName { get; }

    /// <summary>
    /// Validates the specified input.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public abstract ValidationResult Validate(T input);

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    protected ValidationResult Success() => ValidationResult.Success();

    /// <summary>
    /// Creates a failed validation result with the specified message.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A failed validation result.</returns>
    protected ValidationResult Failure(string message) => ValidationResult.Failure(message);
}
