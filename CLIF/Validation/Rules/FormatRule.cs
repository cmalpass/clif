// <copyright file="FormatRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Validation rule for string format checking using regular expressions.
/// </summary>
public class FormatRule : ValidationRule<string>
{
    private readonly System.Text.RegularExpressions.Regex regex;
    private readonly string formatDescription;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatRule"/> class.
    /// </summary>
    /// <param name="pattern">The regular expression pattern.</param>
    /// <param name="formatDescription">A description of the expected format.</param>
    public FormatRule(string pattern, string formatDescription)
    {
        this.regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);
        this.formatDescription = formatDescription;
    }

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "Format";

    /// <summary>
    /// Validates that the input matches the specified format.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrEmpty(input) || !this.regex.IsMatch(input))
        {
            return this.Failure($"Input must match format: {this.formatDescription}");
        }

        return this.Success();
    }
}
