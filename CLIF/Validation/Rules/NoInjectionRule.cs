// <copyright file="NoInjectionRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Validation rule for preventing script injection attacks.
/// </summary>
public class NoInjectionRule : ValidationRule<string>
{
    private static readonly string[] DangerousPatterns =
    {
        "<script", "</script>", "javascript:", "vbscript:",
        "onload=", "onerror=", "onclick=", "onmouseover=",
        "eval(", "setTimeout(", "setInterval(",
        "document.", "window.", "alert(", "confirm(",

        // Common modern injection patterns
        "${jndi", "${env:",

        // File traversal and path patterns
        "..\\", "../",

        // SQL-ish patterns (simple heuristic to catch obvious SQL injection strings)
        "drop table", "';",
    };

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "NoInjection";

    /// <summary>
    /// Validates that the input does not contain potentially dangerous script content.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return this.Success();
        }

        var lowerInput = input.ToLowerInvariant();

        foreach (var pattern in DangerousPatterns)
        {
            if (lowerInput.Contains(pattern))
            {
                // Provide a consistent, test-friendly message
                return this.Failure($"Input contains potentially malicious pattern: {pattern}");
            }
        }

        return this.Success();
    }
}
