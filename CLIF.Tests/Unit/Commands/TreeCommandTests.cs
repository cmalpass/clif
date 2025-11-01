using CLIF.Commands;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.CommandLine;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for TreeCommand
/// </summary>
public class TreeCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TreeCommand _treeCommand;

    public TreeCommandTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TestHelpers.CreateMockLogger<object>().Object);
        _serviceProvider = services.BuildServiceProvider();
        _treeCommand = new TreeCommand(_serviceProvider);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _treeCommand.Should().NotBeNull();
        _treeCommand.Name.Should().Be("tree");
        _treeCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveProcessArgument()
    {
        // Act
        var arguments = _treeCommand.Arguments.ToList();

        // Assert
        arguments.Should().NotBeEmpty();
        arguments.Should().Contain(a => a.Name == "process");
    }

    [Fact]
    public void Command_ShouldHaveTreeOptions()
    {
        // Act
        var options = _treeCommand.Options.ToList();

        // Assert
        options.Should().NotBeEmpty();
        options.Should().Contain(o => o.Name == "depth" || o.Aliases.Contains("--depth"));
        options.Should().Contain(o => o.Name == "search" || o.Aliases.Contains("--search"));
        options.Should().Contain(o => o.Name == "control-type" || o.Aliases.Contains("--control-type"));
        options.Should().Contain(o => o.Name == "output" || o.Aliases.Contains("--output"));
    }

    [Fact]
    public async Task InvokeAsync_WithMissingProcess_ShouldFail()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_treeCommand);
        var args = new[] { "tree" }; // Missing process argument

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert
        result.Should().NotBe(0); // Non-zero indicates failure
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _treeCommand.Should().BeAssignableTo<Command>();
        _treeCommand.Name.Should().Be("tree");
    }

    [Fact]
    public void Command_ShouldHaveFilteringOptions()
    {
        // Act
        var options = _treeCommand.Options.ToList();

        // Assert
        options.Should().Contain(o => o.Name == "enabled-only" || o.Aliases.Contains("--enabled-only"));
        options.Should().Contain(o => o.Name == "visible-only" || o.Aliases.Contains("--visible-only"));
    }
}
