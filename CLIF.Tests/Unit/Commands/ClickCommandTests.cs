using CLIF.Commands;
using CLIF.Core;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Moq;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for ClickCommand
/// </summary>
public class ClickCommandTests
{
    private readonly Mock<IAutomationService> _mockAutomationService;
    private readonly Mock<ISessionCaptureService> _mockCaptureService;
    private readonly ClickCommand _clickCommand;

    public ClickCommandTests()
    {
        _mockAutomationService = new Mock<IAutomationService>();
        _mockCaptureService = new Mock<ISessionCaptureService>();
        _clickCommand = new ClickCommand(_mockAutomationService.Object, _mockCaptureService.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _clickCommand.Should().NotBeNull();
        _clickCommand.Name.Should().Be("click");
        _clickCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveRequiredOptions()
    {
        // Act
        var options = _clickCommand.Options.ToList();

        // Assert
        options.Should().NotBeEmpty();
        options.Should().Contain(o => o.Name == "process-id" || o.Aliases.Contains("--process-id"));
        options.Should().Contain(o => o.Name == "element" || o.Aliases.Contains("--element"));
    }

    [Fact]
    public async Task InvokeAsync_WithMissingProcessId_ShouldFail()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_clickCommand);
        var args = new[] { "click", "--element", "id=TestButton" };

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert
        result.Should().NotBe(0); // Non-zero indicates failure
    }

    [Fact]
    public async Task InvokeAsync_WithMissingElement_ShouldFail()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_clickCommand);
        var args = new[] { "click", "--process-id", "1234" };

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert
        result.Should().NotBe(0); // Non-zero indicates failure
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _clickCommand.Should().BeAssignableTo<Command>();
        _clickCommand.Name.Should().Be("click");
    }
}
