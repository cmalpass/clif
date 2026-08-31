// <copyright file="ISessionCaptureService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core.AutomationElements;
using Microsoft.Extensions.Logging;

namespace CLIF.Services;

/// <summary>
/// Captures screenshots and interaction logs for an automation session.
/// </summary>
public interface ISessionCaptureService
{
    /// <summary>Gets the current session identifier, if a session is active.</summary>
    string? CurrentSessionId { get; }

    /// <summary>Gets the path of the current session, if a session is active.</summary>
    string? CurrentSessionPath { get; }

    /// <summary>Starts a new capture session.</summary>
    /// <param name="sessionName">Optional session identifier.</param>
    /// <param name="targetWindow">Optional window to capture.</param>
    /// <returns>The new session identifier.</returns>
    Task<string> StartSessionAsync(string? sessionName = null, AutomationElement? targetWindow = null);

    /// <summary>Captures a screenshot and log entry after an interaction.</summary>
    /// <param name="actionType">Action that was performed.</param>
    /// <param name="elementInfo">Description of the target element.</param>
    /// <param name="success">Whether the interaction succeeded.</param>
    /// <param name="validationResult">Optional validation detail.</param>
    /// <returns>A task that completes after the interaction has been captured.</returns>
    Task CaptureAfterInteractionAsync(string actionType, string elementInfo, bool success, string? validationResult = null);

    /// <summary>Writes an interaction message to the current session log.</summary>
    /// <param name="message">Message to record.</param>
    /// <param name="level">Severity assigned to the message.</param>
    /// <returns>A task that completes after the message has been logged.</returns>
    Task LogInteractionAsync(string message, LogLevel level = LogLevel.Information);

    /// <summary>Ends the current capture session.</summary>
    /// <returns>A task that completes when the capture session has ended.</returns>
    Task EndSessionAsync();

    /// <summary>Sets the window used for subsequent captures.</summary>
    /// <param name="targetWindow">Window to capture, or <see langword="null"/> for the full screen.</param>
    void SetTargetWindow(AutomationElement? targetWindow);
}
