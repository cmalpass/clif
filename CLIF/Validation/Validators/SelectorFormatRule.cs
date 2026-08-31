// <copyright file="SelectorFormatRule.cs" company="CLIF - Comprehensive UI Automation CLI">
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
