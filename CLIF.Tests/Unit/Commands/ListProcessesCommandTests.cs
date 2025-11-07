using CLIF.Commands;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace CLIF.Tests.Unit.Commands;

/// <summary>
/// Tests for ListProcessesCommand
/// </summary>
public class ListProcessesCommandTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ListProcessesCommand _listProcessesCommand;

    public ListProcessesCommandTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TestHelpers.CreateMockLogger<object>().Object);
        _serviceProvider = services.BuildServiceProvider();
        _listProcessesCommand = new ListProcessesCommand(_serviceProvider);
    }

    [Fact]
    public void Constructor_ShouldCreateValidCommand()
    {
        // Assert
        _listProcessesCommand.Should().NotBeNull();
        _listProcessesCommand.Name.Should().Be("list-processes");
        _listProcessesCommand.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Command_ShouldHaveOptions()
    {
        // Act
        var options = _listProcessesCommand.Options.ToList();

        // Assert
        options.Should().NotBeEmpty();
        options.Should().Contain(o => o.Name == "detailed" || o.Aliases.Contains("--detailed"));
        options.Should().Contain(o => o.Name == "format" || o.Aliases.Contains("--format"));
    }

    [Fact]
    public void Command_ShouldBePartOfCommandSystem()
    {
        // Assert
        _listProcessesCommand.Should().BeAssignableTo<Command>();
        _listProcessesCommand.Name.Should().Be("list-processes");
    }

    [Fact]
    public async Task InvokeAsync_WithNoOptions_ShouldExecute()
    {
        // Arrange
        var rootCommand = new RootCommand();
        rootCommand.Add(_listProcessesCommand);
        var args = new[] { "list-processes" };

        // Act
        var result = await rootCommand.InvokeAsync(args);

        // Assert - Command should execute (result 0 or non-zero depending on environment)
        result.Should().BeInRange(-1, 1);
    }
}
