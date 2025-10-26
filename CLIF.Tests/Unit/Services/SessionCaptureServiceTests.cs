using CLIF.Services;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CLIF.Tests.Unit.Services;

/// <summary>
/// Tests for SessionCaptureService
/// </summary>
public class SessionCaptureServiceTests
{
    private readonly Mock<ILogger<SessionCaptureService>> _mockLogger;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public SessionCaptureServiceTests()
    {
        _mockLogger = new Mock<ILogger<SessionCaptureService>>();
    }

    [Fact]
    public async Task StartSessionAsync_ShouldCreateUniqueSessionId()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act
        var sessionId1 = await service.StartSessionAsync("TEST_Session_1").WithTimeout(DefaultTimeout, "StartSessionAsync #1");
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync #1"); // End first session

        var sessionId2 = await service.StartSessionAsync("TEST_Session_2").WithTimeout(DefaultTimeout, "StartSessionAsync #2");
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync #2"); // End second session

        // Assert
        sessionId1.Should().NotBeEmpty();
        sessionId2.Should().NotBeEmpty();
        sessionId1.Should().NotBe(sessionId2);
        sessionId1.Should().Be("TEST_Session_1");
        sessionId2.Should().Be("TEST_Session_2");
    }

    [Fact]
    public async Task StartSessionAsync_WithNoName_ShouldCreateDefaultSessionId()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act
        var sessionId = await service.StartSessionAsync().WithTimeout(DefaultTimeout, "StartSessionAsync (default)");

        // Assert
        sessionId.Should().NotBeEmpty();
        sessionId.Should().StartWith("CLIF_Session_");

        // Cleanup
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync (default)");
    }

    [Fact]
    public async Task StartSessionAsync_ShouldSetCurrentSessionProperties()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act
        var sessionId = await service.StartSessionAsync("TEST_Session").WithTimeout(DefaultTimeout, "StartSessionAsync (properties)");

        // Assert
        service.CurrentSessionId.Should().Be("TEST_Session");
        service.CurrentSessionPath.Should().NotBeNull();
        service.CurrentSessionPath.Should().EndWith("TEST_Session");

        // Cleanup
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync (properties)");
    }

    [Fact]
    public async Task CaptureAfterInteractionAsync_ShouldLogInteraction()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);
        await service.StartSessionAsync("TEST_Capture").WithTimeout(DefaultTimeout, "StartSessionAsync (capture)");

        // Act & Assert - Should not throw
        await service.CaptureAfterInteractionAsync("click", "id=TestButton", true, "Button clicked successfully").WithTimeout(DefaultTimeout, "CaptureAfterInteractionAsync(click)");
        await service.CaptureAfterInteractionAsync("type", "id=TextBox", false, "Text input failed").WithTimeout(DefaultTimeout, "CaptureAfterInteractionAsync(type)");

        // Cleanup
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync (capture)");
    }

    [Fact]
    public async Task LogInteractionAsync_ShouldLogMessage()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);
        await service.StartSessionAsync("TEST_Log").WithTimeout(DefaultTimeout, "StartSessionAsync (log)");

        // Act & Assert - Should not throw
        await service.LogInteractionAsync("Test message", LogLevel.Information).WithTimeout(DefaultTimeout, "LogInteractionAsync(info)");
        await service.LogInteractionAsync("Warning message", LogLevel.Warning).WithTimeout(DefaultTimeout, "LogInteractionAsync(warn)");

        // Cleanup
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync (log)");
    }

    [Fact]
    public async Task EndSessionAsync_ShouldClearSessionProperties()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);
        await service.StartSessionAsync("TEST_End").WithTimeout(DefaultTimeout, "StartSessionAsync (end)");

        // Act
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, "EndSessionAsync (end)");

        // Assert
        service.CurrentSessionId.Should().BeNull();
        service.CurrentSessionPath.Should().BeNull();
    }

    [Fact]
    public void SetTargetWindow_ShouldNotThrow()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act & Assert - Should not throw
        service.SetTargetWindow(null);
    }

    [Theory]
    [InlineData("CLICK_Session")]
    [InlineData("TYPE_Session")]
    [InlineData("INTERACT_Session")]
    [InlineData("SCRIPT_Session")]
    public async Task StartSessionAsync_WithDifferentCommands_ShouldSetCorrectSessionId(string sessionName)
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act
        var sessionId = await service.StartSessionAsync(sessionName).WithTimeout(DefaultTimeout, $"StartSessionAsync ({sessionName})");

        // Assert
        sessionId.Should().Be(sessionName);
        service.CurrentSessionId.Should().Be(sessionName);

        // Cleanup
        await service.EndSessionAsync().WithTimeout(DefaultTimeout, $"EndSessionAsync ({sessionName})");
    }

    [Fact]
    public async Task CaptureAfterInteractionAsync_WithoutActiveSession_ShouldNotThrow()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act & Assert - Should not throw when no session is active
        await service.CaptureAfterInteractionAsync("click", "id=Button", true).WithTimeout(DefaultTimeout, "CaptureAfterInteractionAsync(no session)");
    }

    [Fact]
    public async Task LogInteractionAsync_WithoutActiveSession_ShouldNotThrow()
    {
        // Arrange
        var service = new SessionCaptureService(_mockLogger.Object);

        // Act & Assert - Should not throw when no session is active
        await service.LogInteractionAsync("Test message").WithTimeout(DefaultTimeout, "LogInteractionAsync(no session)");
    }
}