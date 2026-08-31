// <copyright file="ElementSelectorValidator.cs" company="CLIF - Comprehensive UI Automation CLI">
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
/// Validator for element selectors used in UI automation.
/// </summary>
public class ElementSelectorValidator : ValidatorBase<string>
{
    private static readonly Dictionary<string, string> SelectorPatterns = new()
    {
        { "id", @"^id=[\w\-_]+$" },
        { "name", @"^name=.+$" },
        { "class", @"^class=[\w\-_\.]+$" },
        { "type", @"^type=\w+$" },
        { "xpath", @"^xpath=\/\/.+$" },
        { "automationid", @"^automationid=[\w\-_]+$" },
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ElementSelectorValidator"/> class.
    /// </summary>
    public ElementSelectorValidator()
    {
        this.AddRule(new LengthRule(3, 1000)); // Minimum "id=x", maximum 1000 chars
        this.AddRule(new NoInjectionRule());
        this.AddRule(new SelectorFormatRule());
    }

    /// <summary>
    /// Validates the specified element selector.
    /// </summary>
    /// <param name="selector">The element selector to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string selector)
    {
        if (selector is null)
        {
            return ValidationResult.Failure("Element selector cannot be null");
        }

        if (string.IsNullOrWhiteSpace(selector))
        {
            return ValidationResult.Failure("Element selector cannot be empty");
        }

        var result = this.ValidateRules(selector);

        // Additional selector-specific validation
        if (result.IsValid)
        {
            result.Combine(ValidateSelectorFormat(selector));
        }

        return result;
    }

    /// <summary>
    /// Validates the format of the selector.
    /// </summary>
    /// <param name="selector">The selector to validate.</param>
    /// <returns>A validation result.</returns>
    private static ValidationResult ValidateSelectorFormat(string selector)
    {
        var result = ValidationResult.Success();

        // Check if selector contains an equals sign and split into parts
        var parts = selector.Split('=', 2);
        if (parts.Length != 2)
        {
            result.AddError("Invalid selector format");
            return result;
        }

        var selectorType = parts[0].ToLowerInvariant();
        var selectorValue = parts[1];

        // Validate selector type
        if (!SelectorPatterns.ContainsKey(selectorType))
        {
            var validTypes = string.Join(", ", SelectorPatterns.Keys);
            result.AddError($"Invalid selector type '{selectorType}'. Valid types: {validTypes}");
            return result;
        }

        // Validate selector value is not empty
        if (string.IsNullOrWhiteSpace(selectorValue))
        {
            result.AddError("Selector value cannot be empty");
            return result;
        }

        // Additional validation based on selector type
        switch (selectorType)
        {
            case "xpath":
                if (!selectorValue.StartsWith("//"))
                {
                    result.AddError("XPath selector must start with '//'");
                }

                break;

            case "id":
            case "automationid":
                if (selectorValue.Contains(' '))
                {
                    result.AddError($"{selectorType} selector cannot contain spaces");
                }

                break;

            case "class":
                // Class names can contain dots for compound classes
                if (selectorValue.Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_'))
                {
                    result.AddError("Class selector can only contain letters, digits, dots, hyphens, and underscores");
                }

                break;
        }

        return result;
    }
}

/// <summary>
/// Validation rule for element selector format.
/// </summary>
public class SelectorFormatRule : ValidationRule<string>
{
    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "SelectorFormat";

    /// <summary>
    /// Validates that the selector has a valid format.
    /// </summary>
    /// <param name="input">The selector to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return this.Failure("Selector cannot be empty");
        }

        var parts = input.Split('=', 2);
        if (parts.Length != 2)
        {
            return this.Failure("Invalid selector format");
        }

        if (string.IsNullOrWhiteSpace(parts[0]))
        {
            return this.Failure("Invalid selector format");
        }

        if (string.IsNullOrWhiteSpace(parts[1]))
        {
            return this.Failure("Selector value cannot be empty");
        }

        return this.Success();
    }
}

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

/// <summary>
/// Validation rule for checking safe characters in text input.
/// </summary>
public class SafeCharactersRule : ValidationRule<string>
{
    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "SafeCharacters";

    /// <summary>
    /// Validates that the input contains only safe characters.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return this.Success();
        }

        // Check for dangerous Unicode categories or specific characters
        foreach (var c in input)
        {
            var category = char.GetUnicodeCategory(c);

            // Block format characters that could be used for attacks
            if (category == System.Globalization.UnicodeCategory.Format && c != '\u200C' && c != '\u200D')
            {
                return this.Failure($"Input contains unsafe format character: U+{((int)c):X4}");
            }

            // Block private use characters
            if (category == System.Globalization.UnicodeCategory.PrivateUse)
            {
                return this.Failure($"Input contains private use character: U+{((int)c):X4}");
            }
        }

        return this.Success();
    }
}
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
