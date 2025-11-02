using CLIF.Services;
using Microsoft.Extensions.Logging;
using FlaUI.Core.AutomationElements;
using System.IO;
using System.IO;

namespace CLIF.Tests.Utilities;

/// <summary>
/// Test session capture service for testing purposes
/// </summary>
public class TestSessionCaptureService : ISessionCaptureService
{
    private readonly ILogger<TestSessionCaptureService> _logger;

    public TestSessionCaptureService(ILogger<TestSessionCaptureService> logger)
    {
        _logger = logger;
        CapturedSessions = new List<string>();
        CapturedScreenshots = new List<string>();
        CapturedLogs = new List<string>();
        CapturedInteractions = new List<string>();
    }

    public List<string> CapturedSessions { get; }
    public List<string> CapturedScreenshots { get; }
    public List<string> CapturedLogs { get; }
    public List<string> CapturedInteractions { get; }

    public string? CurrentSessionId { get; private set; }
    public string? CurrentSessionPath { get; private set; }

    public async Task<string> StartSessionAsync(string? sessionName = null, AutomationElement? targetWindow = null)
    {
        var sessionId = sessionName ?? $"TEST_{DateTime.Now:HHmmss}";
        CurrentSessionId = sessionId;
        CurrentSessionPath = Path.Combine("test-sessions", sessionId);
        CapturedSessions.Add(sessionId);
        _logger.LogInformation("Started test session: {SessionId}", sessionId);
        return sessionId;
    }

    public async Task EndSessionAsync()
    {
        if (CurrentSessionId != null)
        {
            _logger.LogInformation("Ended test session: {SessionId}", CurrentSessionId);
        }
        CurrentSessionId = null;
        CurrentSessionPath = null;
        await Task.CompletedTask;
    }

    public async Task CaptureAfterInteractionAsync(string actionType, string elementInfo, bool success, string? validationResult = null)
    {
        var interaction = $"{DateTime.Now:HH:mm:ss.fff} [{actionType}] {elementInfo} - Success: {success}, Validation: {validationResult ?? "N/A"}";
        CapturedInteractions.Add(interaction);
        _logger.LogInformation("Captured test interaction: {Interaction}", interaction);
        await Task.CompletedTask;
    }

    public async Task LogInteractionAsync(string message, LogLevel level = LogLevel.Information)
    {
        var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
        CapturedLogs.Add(logEntry);
        _logger.LogInformation("Logged test message: {Entry}", logEntry);
        await Task.CompletedTask;
    }

    public void SetTargetWindow(AutomationElement? targetWindow)
    {
        _logger.LogInformation("Set target window: {WindowName}", targetWindow?.Name ?? "None");
    }

    public void ClearCapturedData()
    {
        CapturedSessions.Clear();
        CapturedScreenshots.Clear();
        CapturedLogs.Clear();
        CapturedInteractions.Clear();
        CurrentSessionId = null;
        CurrentSessionPath = null;
    }
}