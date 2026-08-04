using CLIF.Commands;
using CLIF.Core;
using CLIF.Services;
using FluentAssertions;
using Moq;
using System.CommandLine;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for TypeCommand
/// </summary>
public class TypeCommandTests
{
    private readonly Mock<IAutomationService> _mockAutomationService;
    private readonly Mock<ISessionCaptureService> _mockCaptureService;
    private readonly TypeCommand _typeCommand;

    public TypeCommandTests()
    {
        _mockAutomationService = new Mock<IAutomationService>();
        _mockCaptureService = new Mock<ISessionCaptureService>();
        _typeCommand = new TypeCommand(_mockAutomationService.Object, _mockCaptureService.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _typeCommand.Should().NotBeNull();
        _typeCommand.Name.Should().Be("type");
        _typeCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveRequiredOptions()
    {
        // Act
        var options = _typeCommand.Options.ToList();

        // Assert
        options.Should().NotBeEmpty();
        options.Should().Contain(o => o.Name == "process-id" || o.Aliases.Contains("--process-id"));
        options.Should().Contain(o => o.Name == "element" || o.Aliases.Contains("--element"));
        options.Should().Contain(o => o.Name == "text" || o.Aliases.Contains("--text"));
    }

    [Fact]
    public async Task InvokeAsync_WithMissingRequiredOptions_ShouldFail()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_typeCommand);
        var args = new[] { "type", "--process-id", "1234" }; // Missing element and text

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert
        result.Should().NotBe(0); // Non-zero indicates failure
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _typeCommand.Should().BeAssignableTo<Command>();
        _typeCommand.Name.Should().Be("type");
    }

    [Fact]
    public async Task InvokeAsync_WhenTypingFails_ShouldReturnNonZero()
    {
        // Arrange
        _mockAutomationService.Setup(service => service.AttachToProcessAsync(1234)).ReturnsAsync(true);
        _mockAutomationService.Setup(service => service.FindElementAsync("id=TestTextBox"))
            .ReturnsAsync(new Mock<FlaUI.Core.AutomationElements.AutomationElement>().Object);
        _mockAutomationService.Setup(service => service.TypeTextAsync(It.IsAny<FlaUI.Core.AutomationElements.AutomationElement>(), "hello"))
            .ReturnsAsync(false);
        var rootCommand = new RootCommand();
        rootCommand.Add(_typeCommand);

        // Act
        var result = await rootCommand.InvokeAsync(new[] { "type", "--process-id", "1234", "--element", "id=TestTextBox", "--text", "hello" });

        // Assert
        result.Should().NotBe(0);
    }
}
