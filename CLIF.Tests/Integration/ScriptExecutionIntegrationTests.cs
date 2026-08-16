using CLIF.Core;
using CLIF.Tests.Utilities;
using CLIF.Tests.WpfUI;
using FluentAssertions;
using Xunit;
using System.IO;

namespace CLIF.Tests.Integration;

/// <summary>
/// Integration tests for script execution functionality
/// These tests validate the full script execution pipeline
/// </summary>
[Collection("WpfUI")]
[Trait("Category", "Integration")]
public class ScriptExecutionIntegrationTests : IntegrationTestBase
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    private readonly WpfTestAppFixture _fixture;

    public ScriptExecutionIntegrationTests(WpfTestAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithBasicLogScript_ShouldExecuteSuccessfully()
    {
        // Arrange
        _fixture.SkipIfUnavailable();
        var script = @"{
            ""name"": ""Integration Test Script"",
            ""description"": ""Basic logging test"",
            ""version"": ""1.0"",
            ""target"": {
                ""processName"": ""TestApp"",
                ""processId"": 0
            },
            ""steps"": [
                {
                    ""action"": ""log"",
                    ""description"": ""Test log message""
                }
            ],
            ""options"": {
                ""stopOnError"": true
            }
        }";

        // Act
        var result = await ScriptService
            .ExecuteScriptContentAsync(script, _fixture.App!.ProcessId)
            .WithTimeout(DefaultTimeout, "ExecuteScriptContentAsync(basic log)");

        // Assert
        result.Should().NotBeNull();
        result.ExecutionLog.Should().NotBeEmpty();
        result.ExecutionLog.Should().Contain(log => log.Contains("Integration Test Script"));
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithWaitAction_ShouldDelayExecution()
    {
        // Arrange
        _fixture.SkipIfUnavailable();
        var script = @"{
            ""name"": ""Wait Test"",
            ""description"": ""Test wait action"",
            ""version"": ""1.0"",
            ""target"": {
                ""processName"": ""TestApp"",
                ""processId"": 0
            },
            ""steps"": [
                {
                    ""action"": ""wait"",
                    ""description"": ""Wait for 100ms"",
                    ""delayMs"": 100
                },
                {
                    ""action"": ""log"",
                    ""description"": ""After wait""
                }
            ]
        }";

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await ScriptService
            .ExecuteScriptContentAsync(script, _fixture.App!.ProcessId)
            .WithTimeout(DefaultTimeout, "ExecuteScriptContentAsync(wait)");
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(100);
    }

    [Fact]
    public async Task ValidateScriptAsync_WithInvalidPath_ShouldReturnFalse()
    {
        // Arrange
        var invalidPath = "/nonexistent/path/to/script.json";

        // Act
        var result = await ScriptService
            .ValidateScriptAsync(invalidPath)
            .WithTimeout(DefaultTimeout, "ValidateScriptAsync(invalid path)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoadScriptAsync_WithInvalidPath_ShouldReturnNull()
    {
        // Arrange
        var invalidPath = "/nonexistent/path/to/script.json";

        // Act
        var result = await ScriptService
            .LoadScriptAsync(invalidPath)
            .WithTimeout(DefaultTimeout, "LoadScriptAsync(invalid path)");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAndLoadScript_ShouldPreserveComplexStructure()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.json");
        var originalScript = new Script
        {
            Name = "Complex Integration Test",
            Description = "Testing complex script structure",
            Version = "2.0",
            Target = new ScriptTarget
            {
                ProcessName = "TestApp",
                WindowTitle = "Test Window",
                TimeoutMs = 10000
            },
            Steps = new List<ScriptStep>
            {
                new ScriptStep
                {
                    Action = "log",
                    Description = "Step 1",
                    DelayMs = 100
                },
                new ScriptStep
                {
                    Action = "wait",
                    DelayMs = 200
                },
                new ScriptStep
                {
                    Action = "screenshot",
                    Description = "Take screenshot",
                    Parameters = new Dictionary<string, object>
                    {
                        { "filename", "test.png" }
                    }
                }
            },
            Variables = new Dictionary<string, object>
            {
                { "var1", "value1" },
                { "var2", 42 },
                { "var3", true }
            },
            Options = new ScriptOptions
            {
                StopOnError = true,
                LogSteps = true,
                TakeScreenshots = false,
                DefaultTimeoutMs = 5000
            }
        };

        try
        {
            // Act
            await ScriptService.SaveScriptAsync(originalScript, tempPath);
            var loadedScript = await ScriptService.LoadScriptAsync(tempPath);

            // Assert
            loadedScript.Should().NotBeNull();
            loadedScript!.Name.Should().Be(originalScript.Name);
            loadedScript.Description.Should().Be(originalScript.Description);
            loadedScript.Version.Should().Be(originalScript.Version);
            loadedScript.Target.ProcessName.Should().Be(originalScript.Target.ProcessName);
            loadedScript.Target.WindowTitle.Should().Be(originalScript.Target.WindowTitle);
            loadedScript.Target.TimeoutMs.Should().Be(originalScript.Target.TimeoutMs);
            loadedScript.Steps.Should().HaveCount(3);
            loadedScript.Steps[0].Action.Should().Be("log");
            loadedScript.Steps[1].Action.Should().Be("wait");
            loadedScript.Steps[2].Action.Should().Be("screenshot");
            loadedScript.Variables.Should().HaveCount(3);
            loadedScript.Options.StopOnError.Should().BeTrue();
            loadedScript.Options.LogSteps.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithMultipleSteps_ShouldExecuteInOrder()
    {
        // Arrange
        _fixture.SkipIfUnavailable();
        var script = @"{
            ""name"": ""Multi-Step Test"",
            ""description"": ""Test step ordering"",
            ""version"": ""1.0"",
            ""target"": {
                ""processId"": 0
            },
            ""steps"": [
                {
                    ""action"": ""log"",
                    ""description"": ""Step 1""
                },
                {
                    ""action"": ""log"",
                    ""description"": ""Step 2""
                },
                {
                    ""action"": ""log"",
                    ""description"": ""Step 3""
                }
            ]
        }";

        // Act
        var result = await ScriptService
            .ExecuteScriptContentAsync(script, _fixture.App!.ProcessId)
            .WithTimeout(DefaultTimeout, "ExecuteScriptContentAsync(multi-step)");

        // Assert
        result.Should().NotBeNull();
        result.ExecutionLog.Should().NotBeEmpty();
        result.StepsExecuted.Should().BeGreaterThanOrEqualTo(3);
    }
}
