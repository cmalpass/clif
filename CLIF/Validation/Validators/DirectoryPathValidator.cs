// <copyright file="DirectoryPathValidator.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using CLIF.Validation.Rules;

namespace CLIF.Validation.Validators;

/// <summary>
/// Validator for directory paths.
/// </summary>
public class DirectoryPathValidator : ValidatorBase<string>
{
    private readonly bool mustExist;
    private readonly bool createIfNotExists;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryPathValidator"/> class.
    /// </summary>
    /// <param name="mustExist">Whether the directory must exist.</param>
    /// <param name="createIfNotExists">Whether to create the directory if it doesn't exist.</param>
    public DirectoryPathValidator(bool mustExist = true, bool createIfNotExists = false)
    {
        this.mustExist = mustExist;
        this.createIfNotExists = createIfNotExists;

        this.AddRule(new LengthRule(1, 248)); // Windows MAX_PATH minus space for filename
        this.AddRule(new PathTraversalRule());
        this.AddRule(new InvalidCharactersRule());
    }

    /// <summary>
    /// Validates the specified directory path.
    /// </summary>
    /// <param name="directoryPath">The directory path to validate.</param>
    /// <returns>A validation result.</returns>
    public override ValidationResult Validate(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return ValidationResult.Failure("Directory path cannot be empty");
        }

        var result = this.ValidateRules(directoryPath);

        // Normalize the path
        try
        {
            directoryPath = Path.GetFullPath(directoryPath);
        }
        catch (Exception ex)
        {
            result.AddError($"Invalid directory path format: {ex.Message}");
            return result;
        }

        // Check directory existence
        if (result.IsValid)
        {
            if (!Directory.Exists(directoryPath))
            {
                if (this.mustExist && !this.createIfNotExists)
                {
                    result.AddError($"Directory not found: {directoryPath}");
                }
                else if (this.createIfNotExists)
                {
                    try
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    catch (Exception ex)
                    {
                        result.AddError($"Failed to create directory: {ex.Message}");
                    }
                }
            }
        }

        return result;
    }
}
