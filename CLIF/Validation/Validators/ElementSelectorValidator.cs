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
