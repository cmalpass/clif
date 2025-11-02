using System;
using System.IO;
using System.Linq;
using CLIF.Validation.Rules;

namespace CLIF.Validation.Validators;

/// <summary>
/// Validator for file paths with security and existence checking
/// </summary>
public class FilePathValidator : ValidatorBase<string>
{
    private readonly bool _mustExist;
    private readonly string[] _allowedExtensions;
    private readonly long _maxFileSize;

    /// <summary>
    /// Initializes a new instance of the FilePathValidator class
    /// </summary>
    /// <param name="mustExist">Whether the file must exist</param>
    /// <param name="allowedExtensions">Array of allowed file extensions (e.g., ".json", ".txt")</param>
    /// <param name="maxFileSize">Maximum allowed file size in bytes (default: 10MB)</param>
    public FilePathValidator(bool mustExist = false, string[]? allowedExtensions = null, long maxFileSize = 10 * 1024 * 1024)
    {
        _mustExist = mustExist;
        _allowedExtensions = allowedExtensions ?? Array.Empty<string>();
        _maxFileSize = maxFileSize;

        AddRule(new LengthRule(1, 260)); // Windows MAX_PATH
        AddRule(new PathTraversalRule());
        // Note: InvalidCharactersRule removed since Path.GetFullPath normalization handles character validation
    }

    /// <summary>
    /// Validates the specified file path
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <returns>A validation result</returns>
    public override ValidationResult Validate(string filePath)
    {
        if (filePath == null)
        {
            return ValidationResult.Failure("File path cannot be null");
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ValidationResult.Failure("File path cannot be empty");
        }

        // First, run validation rules on the raw input to catch traversal patterns and invalid characters
        var preResult = ValidateRules(filePath);
        if (!preResult.IsValid)
        {
            return preResult;
        }

        // Normalize the path so further validation operates on a canonical form
        string normalizedPath;
        try
        {
            normalizedPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Invalid file path format: {ex.Message}");
        }

        // First, run security check for restricted system directories and return early if restricted
        var secureCheck = ValidateSecurePath(normalizedPath);
        if (!secureCheck.IsValid)
        {
            return secureCheck;
        }

        var result = ValidateRules(normalizedPath);

        // Check if file exists when required
        if (_mustExist && result.IsValid)
        {
            if (!File.Exists(normalizedPath))
            {
                result.AddError($"File not found: {normalizedPath}");
            }
            else
            {
                // Validate file extension
                if (_allowedExtensions.Any())
                {
                    var extension = Path.GetExtension(normalizedPath).ToLowerInvariant();
                    if (!_allowedExtensions.Contains(extension))
                    {
                        result.AddError($"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", _allowedExtensions)}");
                    }
                }

                // Validate file size
                try
                {
                    var fileInfo = new FileInfo(normalizedPath);
                    if (fileInfo.Length > _maxFileSize)
                    {
                        result.AddError($"File size ({fileInfo.Length:N0} bytes) exceeds maximum allowed size ({_maxFileSize:N0} bytes)");
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Error checking file size: {ex.Message}");
                }
            }
        }

        // Security check: ensure the file is not in system directories
        if (result.IsValid)
        {
            result.Combine(ValidateSecurePath(normalizedPath));
        }

        return result;
    }

    /// <summary>
    /// Validates that the file path is in a secure location
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <returns>A validation result</returns>
    private static ValidationResult ValidateSecurePath(string filePath)
    {
        var result = ValidationResult.Success();

        try
        {
            var fullPath = Path.GetFullPath(filePath);

            // Define restricted directories
            var restrictedPaths = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64")
            };

            foreach (var restrictedPath in restrictedPaths.Where(p => !string.IsNullOrEmpty(p)))
            {
                if (fullPath.StartsWith(restrictedPath, StringComparison.OrdinalIgnoreCase))
                {
                    result.AddError($"Access to restricted system directory: {restrictedPath}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Error validating path security: {ex.Message}");
        }

        return result;
    }
}

/// <summary>
/// Validator for directory paths
/// </summary>
public class DirectoryPathValidator : ValidatorBase<string>
{
    private readonly bool _mustExist;
    private readonly bool _createIfNotExists;

    /// <summary>
    /// Initializes a new instance of the DirectoryPathValidator class
    /// </summary>
    /// <param name="mustExist">Whether the directory must exist</param>
    /// <param name="createIfNotExists">Whether to create the directory if it doesn't exist</param>
    public DirectoryPathValidator(bool mustExist = true, bool createIfNotExists = false)
    {
        _mustExist = mustExist;
        _createIfNotExists = createIfNotExists;

        AddRule(new LengthRule(1, 248)); // Windows MAX_PATH minus space for filename
        AddRule(new PathTraversalRule());
        AddRule(new InvalidCharactersRule());
    }

    /// <summary>
    /// Validates the specified directory path
    /// </summary>
    /// <param name="directoryPath">The directory path to validate</param>
    /// <returns>A validation result</returns>
    public override ValidationResult Validate(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return ValidationResult.Failure("Directory path cannot be empty");
        }

        var result = ValidateRules(directoryPath);

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
                if (_mustExist && !_createIfNotExists)
                {
                    result.AddError($"Directory not found: {directoryPath}");
                }
                else if (_createIfNotExists)
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