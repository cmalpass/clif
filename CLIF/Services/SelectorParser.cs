// <copyright file="SelectorParser.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using CLIF.Core;
using System.Drawing;
using FlaUI.Core.Input;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;


namespace CLIF.Services;

/// <summary>
/// Parses the selector format emitted by <see cref="ElementTreeService"/>.
/// </summary>
[SupportedOSPlatform("windows7.0")]
internal static class SelectorParser
{
    private static readonly string[] SupportedKeys = ["id", "name", "class", "type"];

    internal static bool TryParse(string? selector, out SelectorCriteria criteria)
    {
        criteria = new SelectorCriteria();
        if (string.IsNullOrWhiteSpace(selector))
        {
            return false;
        }

        var trimmedSelector = selector.Trim();
        if (!trimmedSelector.Contains('='))
        {
            return criteria.TrySet("name", trimmedSelector);
        }

        var clauses = SplitClauses(trimmedSelector);
        if (clauses.Count == 0)
        {
            return false;
        }

        foreach (var clause in clauses)
        {
            var separatorIndex = clause.IndexOf('=');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var key = clause[..separatorIndex].Trim().ToLowerInvariant();
            if (!SupportedKeys.Contains(key, StringComparer.Ordinal))
            {
                return false;
            }

            var value = ParseValue(clause[(separatorIndex + 1)..].Trim());
            if (value is null || !criteria.TrySet(key, value))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(criteria.ControlType) &&
            !Enum.TryParse<ControlType>(criteria.ControlType, ignoreCase: true, out _))
        {
            return false;
        }

        return criteria.HasCriteria;
    }

    internal static string FormatValue(string value)
    {
        if (value.IndexOf('"') < 0 && value.IndexOf('\\') < 0 &&
            !value.Contains(" and ", StringComparison.OrdinalIgnoreCase) &&
            value == value.Trim())
        {
            return value;
        }

        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static List<string> SplitClauses(string selector)
    {
        var clauses = new List<string>();
        var start = 0;
        var quoted = false;
        var escaped = false;

        for (var index = 0; index < selector.Length; index++)
        {
            var character = selector[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (quoted && character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (!quoted && index + 5 <= selector.Length &&
                selector.AsSpan(index, 5).Equals(" and ", StringComparison.OrdinalIgnoreCase))
            {
                clauses.Add(selector[start..index].Trim());
                start = index + 5;
                index += 4;
            }
        }

        if (quoted || escaped)
        {
            return [];
        }

        clauses.Add(selector[start..].Trim());
        return clauses.Any(string.IsNullOrEmpty) ? [] : clauses;
    }

    private static string? ParseValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value[0] != '"')
        {
            return value.Contains('"') ? null : value;
        }

        if (value.Length < 2 || value[^1] != '"')
        {
            return null;
        }

        var result = new System.Text.StringBuilder(value.Length - 2);
        var escaped = false;
        for (var index = 1; index < value.Length - 1; index++)
        {
            var character = value[index];
            if (escaped)
            {
                if (character is not ('"' or '\\'))
                {
                    return null;
                }

                result.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            result.Append(character);
        }

        return escaped ? null : result.ToString();
    }
}
