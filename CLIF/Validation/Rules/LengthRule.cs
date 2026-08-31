// <copyright file="LengthRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Validation rule for string length checking.
/// </summary>
public class LengthRule : ValidationRule<string>
{
    private readonly int minLength;
    private readonly int maxLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthRule"/> class.
    /// </summary>
    /// <param name="minLength">The minimum allowed length.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    public LengthRule(int minLength, int maxLength = int.MaxValue)
    {
        this.minLength = minLength;
        this.maxLength = maxLength;
    }

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "Length";

    /// <summary>
    /// Validates that the input length is within the specified range.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        var length = input?.Length ?? 0;

        if (length < this.minLength)
        {
            return this.Failure($"Input must be at least {this.minLength} characters long");
        }

        if (length > this.maxLength)
        {
            return this.Failure($"Input cannot exceed {this.maxLength} characters");
        }

        return this.Success();
    }
}
