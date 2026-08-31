// <copyright file="SafeCharactersRule.cs" company="CLIF - Comprehensive UI Automation CLI">
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
                return this.Failure($"Input contains unsafe format character: U+{(int)c:X4}");
            }

            // Block private use characters
            if (category == System.Globalization.UnicodeCategory.PrivateUse)
            {
                return this.Failure($"Input contains private use character: U+{(int)c:X4}");
            }
        }

        return this.Success();
    }
}
