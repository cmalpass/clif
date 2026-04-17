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
    private readonly Mock<IAutomationService> _mockAutomationService;
    private readonly Mock<IElementTreeService> _mockElementTreeService;
    private readonly TestSessionCaptureService _testCaptureService;
    private readonly InteractiveService _interactiveService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public InteractiveServiceTests()
    {
        _mockLogger = new Mock<ILogger<InteractiveService>>();
        _mockAutomationService = new Mock<IAutomationService>();
        _mockElementTreeService = new Mock<IElementTreeService>();
        _testCaptureService = new TestSessionCaptureService(
            TestHelpers.CreateMockLogger<TestSessionCaptureService>().Object);

        _interactiveService = new InteractiveService(
            _mockLogger.Object,
            _mockAutomationService.Object,
            _mockElementTreeService.Object,
            _testCaptureService);
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
        // Act - should complete without throwing regardless of return value
        var act = async () => await _interactiveService
            .ExecuteCommandAsync(command)
            .WithTimeout(DefaultTimeout, $"ExecuteCommandAsync({command})");

        // Assert - graceful handling means no exceptions
        await act.Should().NotThrowAsync();
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
    public async Task ExecuteCommandAsync_WithUnknownCommand_ShouldReturnFalse()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("unknowncommand")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(unknown)");

        // Assert
        result.Should().BeFalse(); // Unknown commands should return false
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithClickCommand_WithoutAttach_ShouldReturnFalse()
    {
        // Arrange
        _mockAutomationService.Setup(a => a.IsAttached).Returns(false);

        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("click id=Button")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(click without attach)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithTypeCommand_WithoutElement_ShouldReturnFalse()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("type")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(type without args)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithTreeCommand_WithoutAttach_ShouldReturnFalse()
    {
        // Arrange
        _mockAutomationService.Setup(a => a.IsAttached).Returns(false);

        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("tree")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(tree without attach)");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("click")]
    [InlineData("type id=Test")]
    [InlineData("get-text")]
    [InlineData("get-value")]
    [InlineData("search")]
    public async Task ExecuteCommandAsync_WithIncompleteCommands_ShouldReturnFalse(string command)
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync(command)
            .WithTimeout(DefaultTimeout, $"ExecuteCommandAsync({command})");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithAttachCommand_WithValidPid_ShouldCallAutomationService()
    {
        // Arrange
        _mockAutomationService.Setup(a => a.IsAttached).Returns(false);
        _mockAutomationService.Setup(a => a.AttachToProcessAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("attach 1234")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(attach)");

        // Assert
        result.Should().BeTrue();
        _mockAutomationService.Verify(a => a.AttachToProcessAsync(1234), Times.Once);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithAttachCommand_WithInvalidPid_ShouldReturnFalse()
    {
        // Act
        var result = await _interactiveService
            .ExecuteCommandAsync("attach notanumber")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(attach invalid)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithQuotedString_ShouldParseCorrectly()
    {
        // Arrange
        _mockAutomationService.Setup(a => a.IsAttached).Returns(true);
        _mockAutomationService.Setup(a => a.FindElementAsync(It.IsAny<string>()))
            .ReturnsAsync((FlaUI.Core.AutomationElements.AutomationElement?)null);

        // Act
        await _interactiveService
            .ExecuteCommandAsync("type id=TextBox \"Hello World\"")
            .WithTimeout(DefaultTimeout, "ExecuteCommandAsync(type with quotes)");

        // Assert - Should attempt to find element
        _mockAutomationService.Verify(a => a.FindElementAsync(It.IsAny<string>()), Times.Once);
    }
}
