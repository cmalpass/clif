// <copyright file="PathTraversalRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Validation rule for preventing path traversal attacks.
/// </summary>
public class PathTraversalRule : ValidationRule<string>
{
    private static readonly string[] DangerousPatterns = { "..", "~", "%2e%2e", "%2E%2E" };

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "PathTraversal";

    /// <summary>
    /// Validates that the input does not contain path traversal patterns.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return this.Success();
        }

        foreach (var pattern in DangerousPatterns)
        {
            if (input.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return this.Failure($"Path traversal detected: {pattern}");
            }
        }

        return this.Success();
    }
}
