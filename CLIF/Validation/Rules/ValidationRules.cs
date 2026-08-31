// <copyright file="ValidationRules.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Interface for validation rules.
/// </summary>
/// <typeparam name="T">The type of input to validate.</typeparam>
public interface IValidationRule<T>
{
    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Validates the specified input.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    ValidationResult Validate(T input);
}
