using CLIF.Commands;
using CLIF.Core;
using FluentAssertions;
using CLIF.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.CommandLine;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for InteractCommand
/// </summary>
public class InteractCommandTests
{
    private readonly Mock<IAutomationService> _mockAutomationService;
    private readonly Mock<ISessionCaptureService> _mockCaptureService;
    private readonly Mock<ILogger<InteractCommand>> _mockLogger;
    private readonly InteractCommand _interactCommand;

    public InteractCommandTests()
    {
        _mockAutomationService = new Mock<IAutomationService>();
        _mockCaptureService = new Mock<ISessionCaptureService>();
        _mockLogger = new Mock<ILogger<InteractCommand>>();
        _interactCommand = new InteractCommand(
            _mockAutomationService.Object,
            _mockCaptureService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _interactCommand.Should().NotBeNull();
        _interactCommand.Name.Should().Be("interact");
        _interactCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveRequiredOptions()
    {
        // Act
        var options = _interactCommand.Options.ToList();

        // Assert
        options.Should().NotBeEmpty();
        options.Should().Contain(o => o.Name == "process-id" || o.Aliases.Contains("--process-id"));
        options.Should().Contain(o => o.Name == "element" || o.Aliases.Contains("--element"));
        options.Should().Contain(o => o.Name == "control-type" || o.Aliases.Contains("--control-type"));
        options.Should().Contain(o => o.Name == "action" || o.Aliases.Contains("--action"));
    }

    [Fact]
    public async Task InvokeAsync_WithMissingRequiredOptions_ShouldFail()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_interactCommand);
        var args = new[] { "interact" }; // Missing all required options

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert
        result.Should().NotBe(0); // Non-zero indicates failure
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _interactCommand.Should().BeAssignableTo<Command>();
        _interactCommand.Name.Should().Be("interact");
    }

    [Fact]
    public void Command_ShouldHaveValueAndIndexOptions()
    {
        // Act
        var options = _interactCommand.Options.ToList();

        // Assert
        options.Should().Contain(o => o.Name == "value" || o.Aliases.Contains("--value"));
        options.Should().Contain(o => o.Name == "index" || o.Aliases.Contains("--index"));
    }
}
