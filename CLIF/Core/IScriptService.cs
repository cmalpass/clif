// <copyright file="IScriptService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Loads, validates, saves, and executes CLIF automation scripts.</summary>
public interface IScriptService
{
    /// <summary>Executes a script loaded from a file.</summary>
    /// <param name="scriptPath">Path to the script file.</param>
    /// <param name="processIdOverride">Optional process identifier overriding the script target.</param>
    /// <returns>The execution result.</returns>
    Task<ScriptExecutionResult> ExecuteScriptAsync(string scriptPath, int? processIdOverride = null);

    /// <summary>Executes script content supplied as JSON.</summary>
    /// <param name="jsonContent">JSON representation of the script.</param>
    /// <param name="processIdOverride">Optional process identifier overriding the script target.</param>
    /// <returns>The execution result.</returns>
    Task<ScriptExecutionResult> ExecuteScriptContentAsync(string jsonContent, int? processIdOverride = null);

    /// <summary>Validates a script file without executing it.</summary>
    /// <param name="scriptPath">Path to the script file.</param>
    /// <returns><see langword="true"/> when the script is valid.</returns>
    Task<bool> ValidateScriptAsync(string scriptPath);

    /// <summary>Loads a script from a file.</summary>
    /// <param name="scriptPath">Path to the script file.</param>
    /// <returns>The loaded script, or <see langword="null"/> when it cannot be loaded.</returns>
    Task<Script?> LoadScriptAsync(string scriptPath);

    /// <summary>Saves a script to a file.</summary>
    /// <param name="script">Script to serialize.</param>
    /// <param name="scriptPath">Destination path.</param>
    /// <returns>A task that completes when the script has been saved.</returns>
    Task SaveScriptAsync(Script script, string scriptPath);
}
