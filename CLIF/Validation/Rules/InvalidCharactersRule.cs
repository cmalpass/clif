// <copyright file="InvalidCharactersRule.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Validation rule for checking invalid file path characters.
/// </summary>
public class InvalidCharactersRule : ValidationRule<string>
{
    private static readonly char[] InvalidChars = Path.GetInvalidPathChars()
        .Concat(Path.GetInvalidFileNameChars())
        .Distinct()
        .ToArray();

    /// <summary>
    /// Gets the name of this validation rule.
    /// </summary>
    public override string RuleName => "InvalidCharacters";

    /// <summary>
    /// Checks if a colon at the given position is part of a Windows drive letter pattern.
    /// </summary>
    private static bool IsWindowsDriveLetterColon(string input, int index)
    {
        // Check for typical drive-letter pattern "C:\" or "C:/"
        bool hasPrecedingLetter = index > 0 && char.IsLetter(input[index - 1]);
        bool hasFollowingPathSeparator = index + 1 < input.Length && IsPathSeparator(input[index + 1]);

        if (hasPrecedingLetter && hasFollowingPathSeparator)
        {
            return true;
        }

        // Also allow the simple "C:" form when at index 1
        if (index == 1 && char.IsLetter(input[0]))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a character is a path separator (backslash or forward slash).
    /// </summary>
    private static bool IsPathSeparator(char c)
    {
        return c == '\\' || c == '/';
    }

    /// <summary>
    /// Validates that the input does not contain invalid file path characters.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return this.Success();
        }

        // Allow drive-letter colon on Windows (e.g., "C:\path\to\file")
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];

            // Allow colon when it appears as part of a drive letter (e.g., "C:\")
            if (c == ':' && IsWindowsDriveLetterColon(input, i))
            {
                continue;
            }

            if (InvalidChars.Contains(c))
            {
                return this.Failure($"Input contains invalid character: '{c}'");
            }
        }

        return this.Success();
    }
}
