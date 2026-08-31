using System;
using System.IO;
using System.Security.Principal;
using CLIF.Validation;

namespace CLIF.Security;

/// <summary>
/// Provides security context information for the application
/// </summary>
public class SecurityContext
{
    private static readonly Lazy<SecurityContext> _instance = new(() => new SecurityContext());
    
    /// <summary>
    /// Gets the singleton instance of the SecurityContext
    /// </summary>
    public static SecurityContext Current => _instance.Value;

    /// <summary>
    /// Gets a value indicating whether the current user is an administrator
    /// </summary>
    public bool IsAdministrator { get; }

    /// <summary>
    /// Gets the current user identity
    /// </summary>
    public IIdentity CurrentUser { get; }

    /// <summary>
    /// Gets a value indicating whether the application is running with elevated privileges
    /// </summary>
    public bool IsElevated { get; }

    /// <summary>
    /// Initializes a new instance of the SecurityContext class
    /// </summary>
    private SecurityContext()
    {
        this.CurrentUser = WindowsIdentity.GetCurrent();
        this.IsElevated = IsCurrentUserElevated();
        this.IsAdministrator = IsCurrentUserAdministrator();
    }

    /// <summary>
    /// Validates that the current user has permission to access the specified process
    /// </summary>
    /// <param name="processId">The process ID to check</param>
    /// <returns>A validation result indicating whether access is allowed</returns>
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
    /// Validates that the current user has permission to access the specified file path
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <returns>A validation result indicating whether access is allowed</returns>
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
    /// Checks if the current user is running with elevated privileges
    /// </summary>
    /// <returns>True if elevated, false otherwise</returns>
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
    /// Checks if the current user is an administrator
    /// </summary>
    /// <returns>True if administrator, false otherwise</returns>
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

/// <summary>
/// Provides permission validation services
/// </summary>
public class PermissionValidator
{
    private readonly SecurityContext _securityContext;

    /// <summary>
    /// Initializes a new instance of the PermissionValidator class
    /// </summary>
    /// <param name="securityContext">The security context to use</param>
    public PermissionValidator(SecurityContext? securityContext = null)
    {
        this._securityContext = securityContext ?? SecurityContext.Current;
    }

    /// <summary>
    /// Validates that the user has permission to perform automation operations
    /// </summary>
    /// <returns>A validation result</returns>
    public ValidationResult ValidateAutomationPermissions()
    {
        var result = ValidationResult.Success();

        // Check if we have the necessary permissions for UI automation
        if (!this._securityContext.IsElevated)
        {
            result.AddError("UI Automation may require elevated privileges for some operations. Consider running as administrator if you encounter access issues.");
        }

        return result;
    }

    /// <summary>
    /// Validates permissions for file operations
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <param name="requiredAccess">The type of access required</param>
    /// <returns>A validation result</returns>
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
                        var tempFile = Path.Combine(Path.GetDirectoryName(filePath) ?? "", Path.GetRandomFileName());
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