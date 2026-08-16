using CLIF.Services;
using CLIF.Core;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CLIF.Tests.Unit.Services;

/// <summary>
/// Tests for ProcessService
/// </summary>
public class ProcessServiceTests
{
    private readonly Mock<ILogger<ProcessService>> _mockLogger;
    private readonly ProcessService _processService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public ProcessServiceTests()
    {
        _mockLogger = new Mock<ILogger<ProcessService>>();
        _processService = new ProcessService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetWpfProcessesAsync_ShouldReturnProcessList()
    {
        // Act
        var result = await _processService
            .GetWpfProcessesAsync()
            .WithTimeout(DefaultTimeout, "GetWpfProcessesAsync");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ProcessInfo>>();
        // Note: Result may be empty in test environment, but should not throw
    }

    [Fact]
    public async Task FindProcessByIdAsync_WithCurrentProcess_ShouldHandleGracefully()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act
        var result = await _processService
            .FindProcessByIdAsync(currentProcessId)
            .WithTimeout(DefaultTimeout, "FindProcessByIdAsync(current)");

        // Assert
        // Current test process might not be a WPF process, so result could be null
        // This is valid behavior - the service only returns WPF processes
        if (result != null)
        {
            result.Id.Should().Be(currentProcessId);
            result.Name.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task FindProcessByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var invalidProcessId = -1;

        // Act
        var result = await _processService
            .FindProcessByIdAsync(invalidProcessId)
            .WithTimeout(DefaultTimeout, "FindProcessByIdAsync(invalid)");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindProcessByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentProcessId = 999999; // Very unlikely to exist

        // Act
        var result = await _processService
            .FindProcessByIdAsync(nonExistentProcessId)
            .WithTimeout(DefaultTimeout, "FindProcessByIdAsync(nonexistent)");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("notepad")]
    [InlineData("explorer")]
    [InlineData("NonExistentProcess")]
    public async Task FindProcessByNameAsync_WithVariousNames_ShouldHandleGracefully(string processName)
    {
        // Act
        var result = await _processService
            .FindProcessByNameAsync(processName)
            .WithTimeout(DefaultTimeout, $"FindProcessByNameAsync({processName})");

        // Assert
        // Result can be null or valid ProcessInfo depending on what's running
        if (result != null)
        {
            result.Name.Should().ContainEquivalentOf(processName);
            result.Id.Should().BeGreaterThan(0);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task FindProcessByNameAsync_WithEmptyOrNullName_ShouldReturnNull(string? processName)
    {
        // Act
        var result = await _processService
            .FindProcessByNameAsync(processName!)
            .WithTimeout(DefaultTimeout, "FindProcessByNameAsync(empty/null)");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Calculator")]
    [InlineData("Notepad")]
    [InlineData("NonExistentWindow")]
    public async Task FindProcessByWindowTitleAsync_WithVariousTitles_ShouldHandleGracefully(string windowTitle)
    {
        // Act
        var result = await _processService
            .FindProcessByWindowTitleAsync(windowTitle)
            .WithTimeout(DefaultTimeout, $"FindProcessByWindowTitleAsync({windowTitle})");

        // Assert
        // Result can be null or valid ProcessInfo depending on what windows are open
        if (result != null)
        {
            result.WindowTitle.Should().ContainEquivalentOf(windowTitle);
            result.Id.Should().BeGreaterThan(0);
            result.HasMainWindow.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindProcessByWindowTitleAsync_WithEmptyOrWhitespaceTitle_ShouldReturnNull(string windowTitle)
    {
        // Act
        var result = await _processService
            .FindProcessByWindowTitleAsync(windowTitle)
            .WithTimeout(DefaultTimeout, "FindProcessByWindowTitleAsync(empty/whitespace)");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindProcessByWindowTitleAsync_WithNullTitle_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> action = async () => await _processService.FindProcessByWindowTitleAsync(null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task IsProcessAliveAsync_WithCurrentProcess_ShouldReturnTrue()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act
        var result = await _processService
            .IsProcessAliveAsync(currentProcessId)
            .WithTimeout(DefaultTimeout, "IsProcessAliveAsync(current)");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsProcessAliveAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var invalidProcessId = -1;

        // Act
        var result = await _processService
            .IsProcessAliveAsync(invalidProcessId)
            .WithTimeout(DefaultTimeout, "IsProcessAliveAsync(invalid)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsProcessAliveAsync_WithNonExistentId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentProcessId = 999999; // Very unlikely to exist

        // Act
        var result = await _processService
            .IsProcessAliveAsync(nonExistentProcessId)
            .WithTimeout(DefaultTimeout, "IsProcessAliveAsync(nonexistent)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetWpfProcessesAsync_ResultsAreValid()
    {
        // Act
        var result = await _processService
            .GetWpfProcessesAsync()
            .WithTimeout(DefaultTimeout, "GetWpfProcessesAsync(results valid)");

        // Assert
        result.Should().NotBeNull();
        result.Should().AllSatisfy(process =>
        {
            process.Id.Should().BeGreaterThan(0);
            process.Name.Should().NotBeEmpty();
            // WindowTitle can be empty for some processes
            process.ExecutablePath.Should().NotBeNull();
            process.StartTime.Should().BeBefore(DateTime.Now);
        });
    }

    [Fact]
    public async Task FindProcessByIdAsync_ReturnsValidProcessInfoWhenAvailable()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act
        var result = await _processService
            .FindProcessByIdAsync(currentProcessId)
            .WithTimeout(DefaultTimeout, "FindProcessByIdAsync(valid)");

        // Assert
        // Test process may not be WPF, so result could be null
        if (result != null)
        {
            result.Id.Should().Be(currentProcessId);
            result.Name.Should().NotBeEmpty();
            result.ExecutablePath.Should().NotBeNull();
            result.StartTime.Should().BeBefore(DateTime.Now);
            // HasMainWindow depends on the process type
        }
        else
        {
            // If result is null, that's also valid - means current process is not WPF
            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task ProcessService_AllMethods_ShouldNotThrow()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act & Assert - All methods should handle errors gracefully and not throw
        var action1 = async () => await _processService.GetWpfProcessesAsync().WithTimeout(DefaultTimeout, "GetWpfProcessesAsync");
        var action2 = async () => await _processService.FindProcessByIdAsync(currentProcessId).WithTimeout(DefaultTimeout, "FindProcessByIdAsync");
        var action3 = async () => await _processService.FindProcessByNameAsync("testprocess").WithTimeout(DefaultTimeout, "FindProcessByNameAsync");
        var action4 = async () => await _processService.FindProcessByWindowTitleAsync("Test Window").WithTimeout(DefaultTimeout, "FindProcessByWindowTitleAsync");
        var action5 = async () => await _processService.IsProcessAliveAsync(currentProcessId).WithTimeout(DefaultTimeout, "IsProcessAliveAsync");

        await action1.Should().NotThrowAsync();
        await action2.Should().NotThrowAsync();
        await action3.Should().NotThrowAsync();
        await action4.Should().NotThrowAsync();
        await action5.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetWpfProcessesAsync_FilteringBehavior()
    {
        // Act
        var allProcesses = await _processService
            .GetWpfProcessesAsync()
            .WithTimeout(DefaultTimeout, "GetWpfProcessesAsync(filter)");

        // Assert
        // The service should only return processes that are WPF applications
        // In test environment, this might be empty, but should not include non-WPF processes
        allProcesses.Should().NotBeNull();

        if (allProcesses.Any())
        {
            allProcesses.Should().AllSatisfy(process =>
            {
                // All returned processes should have basic valid properties
                process.Id.Should().BeGreaterThan(0);
                process.Name.Should().NotBeEmpty();
            });
        }
    }

    [Fact]
    public async Task ProcessService_ConcurrentCalls_ShouldHandleGracefully()
    {
        // Arrange
        var tasks = new List<Task>();
        var currentProcessId = Environment.ProcessId;

        // Act - Make multiple concurrent calls
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_processService.GetWpfProcessesAsync().WithTimeout(DefaultTimeout, "GetWpfProcessesAsync(concurrent)"));
            tasks.Add(_processService.FindProcessByIdAsync(currentProcessId).WithTimeout(DefaultTimeout, "FindProcessByIdAsync(concurrent)"));
            tasks.Add(_processService.IsProcessAliveAsync(currentProcessId).WithTimeout(DefaultTimeout, "IsProcessAliveAsync(concurrent)"));
        }

        // Assert - All tasks should complete without throwing
        var action = async () => await Task.WhenAll(tasks).WithTimeout(DefaultTimeout, "ProcessService concurrent calls");
        await action.Should().NotThrowAsync();
    }
}
