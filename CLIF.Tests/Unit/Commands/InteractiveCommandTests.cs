using CLIF.Commands;
using CLIF.Core;
using FluentAssertions;
using Moq;
using System.CommandLine;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for InteractiveCommand
/// </summary>
public class InteractiveCommandTests
{
    private readonly Mock<IInteractiveService> _mockInteractiveService;
    private readonly InteractiveCommand _interactiveCommand;

    public InteractiveCommandTests()
    {
        _mockInteractiveService = new Mock<IInteractiveService>();
        _interactiveCommand = new InteractiveCommand(_mockInteractiveService.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _interactiveCommand.Should().NotBeNull();
        _interactiveCommand.Name.Should().Be("interactive");
        _interactiveCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveProcessIdOption()
    {
        // Act
        var options = _interactiveCommand.Options.ToList();

        // Assert
        options.Should().Contain(o => o.Name == "process-id" || o.Aliases.Contains("--process-id"));
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _interactiveCommand.Should().BeAssignableTo<Command>();
        _interactiveCommand.Name.Should().Be("interactive");
    }

    [Fact]
    public async Task InvokeAsync_WithNoOptions_ShouldExecute()
    {
        // Arrange
        _mockInteractiveService
            .Setup(s => s.StartInteractiveSessionAsync(It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        var rootCommand = new RootCommand();
        rootCommand.Add(_interactiveCommand);
        var args = new[] { "interactive" };

        // Act
        await rootCommand.InvokeAsync(args);

        // Assert
        _mockInteractiveService.Verify(
            s => s.StartInteractiveSessionAsync(null),
            Times.Once);
    }
}
