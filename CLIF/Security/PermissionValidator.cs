// <copyright file="PermissionValidator.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Principal;
using CLIF.Validation;

namespace CLIF.Security;

/// <summary>
/// Provides permission validation services.
/// </summary>
public class PermissionValidator
{
    private readonly SecurityContext securityContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionValidator"/> class.
    /// </summary>
    /// <param name="securityContext">The security context to use.</param>
    public PermissionValidator(SecurityContext? securityContext = null)
    {
        this.securityContext = securityContext ?? SecurityContext.Current;
    }

    /// <summary>
    /// Validates that the user has permission to perform automation operations.
    /// </summary>
    /// <returns>A validation result.</returns>
    public ValidationResult ValidateAutomationPermissions()
    {
        var result = ValidationResult.Success();

        // Check if we have the necessary permissions for UI automation
        if (!this.securityContext.IsElevated)
        {
            result.AddError("UI Automation may require elevated privileges for some operations. Consider running as administrator if you encounter access issues.");
        }

        return result;
    }

    /// <summary>
    /// Validates permissions for file operations.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="requiredAccess">The type of access required.</param>
    /// <returns>A validation result.</returns>
    public ValidationResult ValidateFilePermissions(string filePath, FileAccess requiredAccess)
    {
        var result = ValidationResult.Success();

        try
        {
            switch (requiredAccess)
            {
                case FileAccess.Read:
                    if (File.Exists(filePath))
                    {
                        using var readStream = File.OpenRead(filePath);
                    }

                    break;

                case FileAccess.Write:
                    if (File.Exists(filePath))
                    {
                        using var writeStream = File.OpenWrite(filePath);
                    }
                    else
                    {
                        // Check if we can create the file
                        var tempFile = Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, Path.GetRandomFileName());
                        File.WriteAllText(tempFile, "test");
                        File.Delete(tempFile);
                    }

                    break;

                case FileAccess.ReadWrite:
                    result.Combine(this.ValidateFilePermissions(filePath, FileAccess.Read));
                    result.Combine(this.ValidateFilePermissions(filePath, FileAccess.Write));
                    break;
            }
        }
        catch (UnauthorizedAccessException)
        {
            result.AddError($"Access denied for {requiredAccess} operation on: {filePath}");
        }
        catch (Exception ex)
        {
            result.AddError($"Error validating {requiredAccess} permissions for {filePath}: {ex.Message}");
        }

        return result;
    }
}
