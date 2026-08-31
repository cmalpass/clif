// <copyright file="RangeRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Validation rule for numeric range checking.
/// </summary>
/// <typeparam name="T">The numeric type to validate.</typeparam>
public class RangeRule<T> : ValidationRule<T>
    where T : IComparable<T>
{
    private readonly T minimum;
    private readonly T maximum;

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeRule{T}"/> class.
    /// </summary>
    /// <param name="minimum">The minimum allowed value.</param>
    /// <param name="maximum">The maximum allowed value.</param>
    public RangeRule(T minimum, T maximum)
    {
        this.minimum = minimum;
        this.maximum = maximum;
    }

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "Range";

    /// <summary>
    /// Validates that the input is within the specified range.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(T input)
    {
        if (input.CompareTo(this.minimum) < 0 || input.CompareTo(this.maximum) > 0)
        {
            return this.Failure($"Value must be between {this.minimum} and {this.maximum}");
        }

        return this.Success();
    }
}
