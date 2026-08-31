// <copyright file="TextInputValidator.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CLIF.Validation.Rules;

namespace CLIF.Validation.Validators;

/// <summary>
/// Validator for text input used in UI automation.
/// </summary>
public class TextInputValidator : ValidatorBase<string>
{
    private readonly int minLength;
    private readonly int maxLength;
    private readonly bool allowEmpty;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextInputValidator"/> class.
    /// </summary>
    /// <param name="minLength">Minimum allowed text length.</param>
    /// <param name="maxLength">Maximum allowed text length.</param>
    /// <param name="allowEmpty">Whether empty text is allowed.</param>
    public TextInputValidator(int minLength = 4, int maxLength = 300, bool allowEmpty = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minLength);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        if (minLength > maxLength)
        {
            throw new ArgumentException("Minimum length cannot be greater than maximum length.", nameof(minLength));
        }

        this.minLength = minLength;
        this.maxLength = maxLength;
        this.allowEmpty = allowEmpty;

        this.AddRule(new NoInjectionRule());
        this.AddRule(new SafeCharactersRule());
    }

    /// <summary>
    /// Validates the specified text input.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string text)
    {
        // Handle null input with specific message expected by tests
        if (text == null)
        {
            return ValidationResult.Failure("Text input cannot be null");
        }

        // Handle empty or whitespace-only input (unless allowEmpty is true)
        if (!this.allowEmpty && string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Failure("Text input cannot be empty");
        }

        // When allowEmpty is true and the text is empty, skip length checks
        if (this.allowEmpty && text.Length == 0)
        {
            return this.ValidateRules(text);
        }

        // Check minimum length
        if (text.Length < this.minLength)
        {
            return ValidationResult.Failure($"Text input does not meet the minimum length of {this.minLength} characters");
        }

        // Check maximum length
        if (text.Length > this.maxLength)
        {
            return ValidationResult.Failure($"Text input exceeds the maximum length of {this.maxLength} characters");
        }

        // Now validate using rules (including malicious content detection)
        var result = this.ValidateRules(text);

        // Additional text-specific validation
        if (result.IsValid)
        {
            result.Combine(ValidateTextSafety(text));
        }

        return result;
    }

    /// <summary>
    /// Validates that the text is safe for UI input.
    /// </summary>
    /// <param name="text">The text to validate.</param>
    /// <returns>A validation result.</returns>
    private static ValidationResult ValidateTextSafety(string text)
    {
        var result = ValidationResult.Success();

        // Check for potentially problematic characters
        if (text.Contains('\0'))
        {
            result.AddError("Text cannot contain null characters");
        }

        // Check for excessive control characters
        var controlCharCount = text.Count(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t');
        if (controlCharCount > text.Length * 0.1) // More than 10% control characters
        {
            result.AddError("Text contains too many control characters");
        }

        return result;
    }
}
