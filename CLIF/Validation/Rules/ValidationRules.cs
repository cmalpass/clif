using System;
using System.IO;
using System.Linq;

namespace CLIF.Validation.Rules;

/// <summary>
/// Interface for validation rules
/// </summary>
/// <typeparam name="T">The type of input to validate</typeparam>
public interface IValidationRule<T>
{
    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Validates the specified input
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
    ValidationResult Validate(T input);
}

/// <summary>
/// Abstract base class for validation rules
/// </summary>
/// <typeparam name="T">The type of input to validate</typeparam>
public abstract class ValidationRule<T> : IValidationRule<T>
{
    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public abstract string RuleName { get; }

    /// <summary>
    /// Validates the specified input
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
    public abstract ValidationResult Validate(T input);

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>A successful validation result</returns>
    protected ValidationResult Success() => ValidationResult.Success();

    /// <summary>
    /// Creates a failed validation result with the specified message
    /// </summary>
    /// <param name="message">The failure message</param>
    /// <returns>A failed validation result</returns>
    protected ValidationResult Failure(string message) => ValidationResult.Failure(message);
}

/// <summary>
/// Validation rule for numeric range checking
/// </summary>
/// <typeparam name="T">The numeric type to validate</typeparam>
public class RangeRule<T> : ValidationRule<T> where T : IComparable<T>
{
    private readonly T _minimum;
    private readonly T _maximum;

    /// <summary>
    /// Initializes a new instance of the RangeRule class
    /// </summary>
    /// <param name="minimum">The minimum allowed value</param>
    /// <param name="maximum">The maximum allowed value</param>
    public RangeRule(T minimum, T maximum)
    {
        this._minimum = minimum;
        this._maximum = maximum;
    }

    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public override string RuleName => "Range";

    /// <summary>
    /// Validates that the input is within the specified range
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
    public override ValidationResult Validate(T input)
    {
        if (input.CompareTo(this._minimum) < 0 || input.CompareTo(this._maximum) > 0)
        {
            return this.Failure($"Value must be between {this._minimum} and {this._maximum}");
        }
        return this.Success();
    }
}

/// <summary>
/// Validation rule for string format checking using regular expressions
/// </summary>
public class FormatRule : ValidationRule<string>
{
    private readonly System.Text.RegularExpressions.Regex _regex;
    private readonly string _formatDescription;

    /// <summary>
    /// Initializes a new instance of the FormatRule class
    /// </summary>
    /// <param name="pattern">The regular expression pattern</param>
    /// <param name="formatDescription">A description of the expected format</param>
    public FormatRule(string pattern, string formatDescription)
    {
        this._regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);
        this._formatDescription = formatDescription;
    }

    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public override string RuleName => "Format";

    /// <summary>
    /// Validates that the input matches the specified format
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
    public override ValidationResult Validate(string input)
    {
        if (string.IsNullOrEmpty(input) || !this._regex.IsMatch(input))
        {
            return this.Failure($"Input must match format: {this._formatDescription}");
        }
        return this.Success();
    }
}

/// <summary>
/// Validation rule for string length checking
/// </summary>
public class LengthRule : ValidationRule<string>
{
    private readonly int _minLength;
    private readonly int _maxLength;

    /// <summary>
    /// Initializes a new instance of the LengthRule class
    /// </summary>
    /// <param name="minLength">The minimum allowed length</param>
    /// <param name="maxLength">The maximum allowed length</param>
    public LengthRule(int minLength, int maxLength = int.MaxValue)
    {
        this._minLength = minLength;
        this._maxLength = maxLength;
    }

    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public override string RuleName => "Length";

    /// <summary>
    /// Validates that the input length is within the specified range
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
    public override ValidationResult Validate(string input)
    {
        var length = input?.Length ?? 0;

        if (length < this._minLength)
        {
            return this.Failure($"Input must be at least {this._minLength} characters long");
        }

        if (length > this._maxLength)
        {
            return this.Failure($"Input cannot exceed {this._maxLength} characters");
        }

        return this.Success();
    }
}

/// <summary>
/// Validation rule for preventing path traversal attacks
/// </summary>
public class PathTraversalRule : ValidationRule<string>
{
    private static readonly string[] DangerousPatterns = { "..", "~", "%2e%2e", "%2E%2E" };

    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public override string RuleName => "PathTraversal";

    /// <summary>
    /// Validates that the input does not contain path traversal patterns
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
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

/// <summary>
/// Validation rule for checking invalid file path characters
/// </summary>
public class InvalidCharactersRule : ValidationRule<string>
{
    private static readonly char[] InvalidChars = Path.GetInvalidPathChars()
        .Concat(Path.GetInvalidFileNameChars())
        .Distinct()
        .ToArray();

    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public override string RuleName => "InvalidCharacters";

    /// <summary>
    /// Checks if a colon at the given position is part of a Windows drive letter pattern
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
    /// Checks if a character is a path separator (backslash or forward slash)
    /// </summary>
    private static bool IsPathSeparator(char c)
    {
        return c == '\\' || c == '/';
    }

    /// <summary>
    /// Validates that the input does not contain invalid file path characters
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
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

/// <summary>
/// Validation rule for preventing script injection attacks
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
        "drop table", "';"
    };

    /// <summary>
    /// Gets the name of this validation rule
    /// </summary>
    public override string RuleName => "NoInjection";

    /// <summary>
    /// Validates that the input does not contain potentially dangerous script content
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>A validation result</returns>
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