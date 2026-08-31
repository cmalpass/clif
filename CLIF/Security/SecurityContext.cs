// <copyright file="SecurityContext.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System;
using System.IO;
using System.Security.Principal;
using CLIF.Validation;

namespace CLIF.Security;

/// <summary>
/// Provides security context information for the application.
/// </summary>
public class SecurityContext
{
    private static readonly Lazy<SecurityContext> Instance = new(() => new SecurityContext());

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityContext"/> class.
    /// </summary>
    private SecurityContext()
    {
        this.CurrentUser = WindowsIdentity.GetCurrent();
        this.IsElevated = IsCurrentUserElevated();
        this.IsAdministrator = IsCurrentUserAdministrator();
    }

    /// <summary>
    /// Gets the singleton instance of the SecurityContext.
    /// </summary>
    public static SecurityContext Current => Instance.Value;

    /// <summary>
    /// Gets a value indicating whether the current user is an administrator.
    /// </summary>
    public bool IsAdministrator { get; }

    /// <summary>
    /// Gets the current user identity.
    /// </summary>
    public IIdentity CurrentUser { get; }

    /// <summary>
    /// Gets a value indicating whether the application is running with elevated privileges.
    /// </summary>
    public bool IsElevated { get; }

    /// <summary>
    /// Validates that the current user has permission to access the specified process.
    /// </summary>
    /// <param name="processId">The process ID to check.</param>
    /// <returns>A validation result indicating whether access is allowed.</returns>
    public ValidationResult ValidateProcessAccess(int processId)
    {
        try
        {
            using var process = System.Diagnostics.Process.GetProcessById(processId);

            // Check if we can access the process
            _ = process.ProcessName; // This will throw if we don't have access
            _ = process.MainWindowTitle; // This requires additional permissions

            return ValidationResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Failure($"Access denied to process {processId}. Administrative privileges may be required.");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return ValidationResult.Failure($"Cannot access process {processId}: {ex.Message}");
        }
        catch (ArgumentException)
        {
            return ValidationResult.Failure($"Process {processId} does not exist");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Error accessing process {processId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that the current user has permission to access the specified file path.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>A validation result indicating whether access is allowed.</returns>
    public ValidationResult ValidateFileAccess(string filePath)
    {
        try
        {
            var fullPath = Path.GetFullPath(filePath);

            // Check if we can read the file/directory
            if (File.Exists(fullPath))
            {
                using var stream = File.OpenRead(fullPath);
                return ValidationResult.Success();
            }
            else if (Directory.Exists(fullPath))
            {
                Directory.GetFiles(fullPath);
                return ValidationResult.Success();
            }
            else
            {
                // Check if we can access the parent directory
                var parentDir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir))
                {
                    Directory.GetFiles(parentDir);
                    return ValidationResult.Success();
                }
                else
                {
                    return ValidationResult.Failure($"Cannot access path: {filePath}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Failure($"Access denied to path: {filePath}");
        }
        catch (DirectoryNotFoundException)
        {
            return ValidationResult.Failure($"Directory not found: {Path.GetDirectoryName(filePath)}");
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Error accessing path {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if the current user is running with elevated privileges.
    /// </summary>
    /// <returns>True if elevated, false otherwise.</returns>
    private static bool IsCurrentUserElevated()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the current user is an administrator.
    /// </summary>
    /// <returns>True if administrator, false otherwise.</returns>
    private static bool IsCurrentUserAdministrator()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}
