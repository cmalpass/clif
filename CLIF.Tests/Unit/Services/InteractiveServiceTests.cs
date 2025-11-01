using CLIF.Services;
using CLIF.Core;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CLIF.Tests.Unit.Services;

/// <summary>
/// Tests for InteractiveService
/// </summary>
public class InteractiveServiceTests
{
    private readonly Mock<ILogger<InteractiveService>> _mockLogger;
    private readonly InteractiveService _interactiveService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public InteractiveServiceTests()
    {
        _mockLogger = new Mock<ILogger<InteractiveService>>();
        _interactiveService = new InteractiveService(_mockLogger.Object);
    }

    [Fact]
    public void IsSessionActive_Initially_ShouldBeFalse()
    {
        // Assert
        _interactiveService.IsSessionActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetPromptAsync_ShouldReturnPromptString()
    {
        // Act
        var result = await _interactiveService
            .GetPromptAsync()
            .WithTimeout(DefaultTimeout, "GetPromptAsync");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("CLIF");
    }

    [Fact]
    public async Task ShowHelpAsync_ShouldCompleteSuccessfully()
    {
        // Act & Assert - Should not throw
        await _interactiveService
            .ShowHelpAsync()
            .WithTimeout(DefaultTimeout, "ShowHelpAsync");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithHelpCommand_ShouldReturnTrue()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("help")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(help)");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithHelpUpperCase_ShouldReturnTrue()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("HELP")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(HELP)");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("click id=Button")]
    [InlineData("type id=TextBox some text")]
    [InlineData("tree")]
    [InlineData("screenshot")]
    public async Task ExecuteCommandAsync_WithVariousCommands_ShouldHandleGracefully(string command)
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync(command)
            .WithTimeout(DefaultTimeout, $"ExecuteCommandAsync({command})");

        // Assert
        result.Should().BeTrue(); // Currently returns true for unimplemented commands
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithEmptyCommand_ShouldHandleGracefully()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync(string.Empty)
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(empty)");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithUnknownCommand_ShouldReturnTrue()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("unknowncommand")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(unknown)");

        // Assert
        result.Should().BeTrue(); // Service handles unknown commands gracefully
    }
}
