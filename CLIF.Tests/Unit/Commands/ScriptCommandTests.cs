using CLIF.Commands;
using CLIF.Core;
using FluentAssertions;
using Moq;
using System.CommandLine;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for ScriptCommand
/// </summary>
public class ScriptCommandTests
{
    private readonly Mock<IScriptService> _mockScriptService;
    private readonly ScriptCommand _scriptCommand;

    public ScriptCommandTests()
    {
        _mockScriptService = new Mock<IScriptService>();
        _scriptCommand = new ScriptCommand(_mockScriptService.Object);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _scriptCommand.Should().NotBeNull();
        _scriptCommand.Name.Should().Be("script");
        _scriptCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveScriptFileArgument()
    {
        // Act
        var arguments = _scriptCommand.Arguments.ToList();

        // Assert
        arguments.Should().NotBeEmpty();
        arguments.Should().Contain(a => a.Name == "script-file");
    }

    [Fact]
    public void Command_ShouldHaveProcessIdOption()
    {
        // Act
        var options = _scriptCommand.Options.ToList();

        // Assert
        options.Should().Contain(o => o.Name == "process-id" || o.Aliases.Contains("--process-id"));
    }

    [Fact]
    public async Task InvokeAsync_WithMissingScriptFile_ShouldFail()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_scriptCommand);
        var args = new[] { "script" }; // Missing script file argument

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert
        result.Should().NotBe(0); // Non-zero indicates failure
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _scriptCommand.Should().BeAssignableTo<Command>();
        _scriptCommand.Name.Should().Be("script");
    }
}
