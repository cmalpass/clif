// <copyright file="IInteractiveService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CLIF.Core;

/// <summary>Manages an interactive CLIF automation session.</summary>
public interface IInteractiveService
{
    /// <summary>Gets a value indicating whether gets whether an interactive session is active.</summary>
    bool IsSessionActive { get; }

    /// <summary>Starts an interactive session.</summary>
    /// <param name="processId">Optional process to attach to initially.</param>
    /// <returns>A task that completes when the interactive session starts.</returns>
    Task StartInteractiveSessionAsync(int? processId = null);

    /// <summary>Executes one interactive command.</summary>
    /// <param name="command">Command text to execute.</param>
    /// <returns><see langword="true"/> when the command succeeds.</returns>
    Task<bool> ExecuteCommandAsync(string command);

    /// <summary>Displays interactive command help.</summary>
    /// <returns>A task that completes after help is displayed.</returns>
    Task ShowHelpAsync();

    /// <summary>Gets the current interactive prompt.</summary>
    /// <returns>The prompt text.</returns>
    Task<string> GetPromptAsync();

}
