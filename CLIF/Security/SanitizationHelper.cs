using System.Text;
using System.Text.RegularExpressions;

namespace CLIF.Security;

/// <summary>
/// Provides utilities for sanitizing and cleaning user input.
/// </summary>
public static class SanitizationHelper
{
    /// <summary>
    /// Sanitizes a string for safe use in file paths.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <param name="replacement">The character to replace invalid characters with.</param>
    /// <returns>A sanitized string safe for use in file paths.</returns>
    public static string SanitizeFilePath(string input, char replacement = '_')
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (invalidChars.Contains(c) || char.IsControl(c))
            {
                sanitized.Append(replacement);
            }
            else
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString();

        // Remove excessive replacement characters
        while (result.Contains(new string(replacement, 2)))
        {
            result = result.Replace(new string(replacement, 2), replacement.ToString());
        }

        // Trim replacement characters from start and end
        result = result.Trim(replacement);

        // Ensure the result is not empty
        if (string.IsNullOrEmpty(result))
        {
            result = "sanitized";
        }

        return result;
    }

    /// <summary>
    /// Sanitizes a string for safe use in element selectors.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <returns>A sanitized string safe for use in element selectors.</returns>
    public static string SanitizeElementSelector(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Remove potentially dangerous patterns
        var dangerous = new[]
        {
            @"<script.*?>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"on\w+\s*=",
            @"eval\s*\(",
            @"document\.",
            @"window\.",
            @"alert\s*\(",
            @"confirm\s*\(",
        };

        var result = input;
        foreach (var pattern in dangerous)
        {
            result = Regex.Replace(result, pattern, string.Empty, RegexOptions.IgnoreCase);
        }

        // Remove control characters except common ones
        result = new string(result.Where(c => !char.IsControl(c) || c == '\t' || c == '\r' || c == '\n').ToArray());

        // Limit length
        if (result.Length > 1000)
        {
            result = result.Substring(0, 1000);
        }

        return result;
    }

    /// <summary>
    /// Sanitizes text input to prevent injection attacks.
    /// </summary>
    /// <param name="input">The input text to sanitize.</param>
    /// <param name="preserveFormatting">Whether to preserve basic formatting characters.</param>
    /// <returns>A sanitized string safe for text input.</returns>
    public static string SanitizeTextInput(string input, bool preserveFormatting = true)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = input;

        // Do not remove injection-like patterns for free-form text input.
        // Sanitization for text input should preserve content like ${jndi:...} or SQL snippets
        // and only strip truly dangerous control/unicode characters or nulls.

        // Remove null characters
        result = result.Replace("\0", string.Empty);

        // Remove or replace dangerous Unicode characters
        result = RemoveDangerousUnicodeCharacters(result);

        // Remove excessive control characters
        if (!preserveFormatting)
        {
            result = new string(result.Where(c => !char.IsControl(c)).ToArray());
        }
        else
        {
            // Keep only common formatting characters
            result = new string(result.Where(c => !char.IsControl(c) || c == '\r' || c == '\n' || c == '\t').ToArray());
        }

        // Normalize line endings
        result = result.Replace("\r\n", "\n").Replace("\r", "\n");

        return result;
    }

    /// <summary>
    /// Escapes special characters in a string for safe use in regular expressions.
    /// </summary>
    /// <param name="input">The input string to escape.</param>
    /// <returns>An escaped string safe for use in regular expressions.</returns>
    public static string EscapeRegexSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return Regex.Escape(input);
    }

    /// <summary>
    /// Removes potentially dangerous Unicode characters from input.
    /// </summary>
    /// <param name="input">The input string to clean.</param>
    /// <returns>A cleaned string with dangerous Unicode characters removed.</returns>
    private static string RemoveDangerousUnicodeCharacters(string input)
    {
        var result = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            var category = char.GetUnicodeCategory(c);

            // Skip dangerous Unicode categories
            switch (category)
            {
                case System.Globalization.UnicodeCategory.Format:
                    // Allow only common format characters
                    if (c == '\u200C' || c == '\u200D') // Zero-width non-joiner/joiner
                    {
                        result.Append(c);
                    }

                    break;

                case System.Globalization.UnicodeCategory.PrivateUse:
                    // Skip private use characters
                    break;

                case System.Globalization.UnicodeCategory.Surrogate:
                    // Skip unpaired surrogates
                    break;

                case System.Globalization.UnicodeCategory.OtherNotAssigned:
                    // Skip unassigned characters
                    break;

                default:
                    result.Append(c);
                    break;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Validates and sanitizes a process identifier (name or ID).
    /// </summary>
    /// <param name="input">The process identifier to sanitize.</param>
    /// <returns>A sanitized process identifier.</returns>
    public static string SanitizeProcessIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        // If it's numeric, just validate and return
        if (int.TryParse(input.Trim(), out _))
        {
            return input.Trim();
        }

        // For process names, remove dangerous characters
        var result = new StringBuilder();
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
            {
                result.Append(c);
            }
        }

        var sanitized = result.ToString();

        // Ensure reasonable length
        if (sanitized.Length > 255)
        {
            sanitized = sanitized.Substring(0, 255);
        }

        return sanitized;
    }

    /// <summary>
    /// Removes directory traversal patterns from a path.
    /// </summary>
    /// <param name="path">The path to sanitize.</param>
    /// <returns>A path with traversal patterns removed.</returns>
    public static string RemovePathTraversal(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        // Remove common path traversal patterns
        var patterns = new[]
        {
            @"\.\.[\\/]",  // ../ or ..\
            @"\.\.\\",     // ..\
            @"\.\./",      // ../
            @"%2[eE]%2[eE]", // URL encoded ..
            @"~",          // Home directory reference
        };

        var result = path;
        foreach (var pattern in patterns)
        {
            result = Regex.Replace(result, pattern, string.Empty, RegexOptions.IgnoreCase);
        }

        // Normalize path separators
        result = result.Replace('/', Path.DirectorySeparatorChar);

        return result;
    }

    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if truncated.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="ellipsis">The ellipsis string to append when truncated.</param>
    /// <returns>A truncated string.</returns>
    public static string TruncateWithEllipsis(string input, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input;
        }

        var truncateLength = Math.Max(0, maxLength - ellipsis.Length);
        return input.Substring(0, truncateLength) + ellipsis;
    }
}
