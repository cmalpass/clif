using CLIF.Services;
using CLIF.Core;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;

namespace CLIF.Tests.Unit.Services;

/// <summary>
/// Tests for ScriptService
/// </summary>
public class ScriptServiceTests : IDisposable
{
    private readonly Mock<ILogger<ScriptService>> _mockLogger;
    private readonly Mock<IProcessService> _mockProcessService;
    private readonly Mock<IAutomationService> _mockAutomationService;
    private readonly TestSessionCaptureService _testCaptureService;
    private readonly ScriptService _scriptService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
    private readonly string _tempScriptPath;

    public ScriptServiceTests()
    {
        _mockLogger = new Mock<ILogger<ScriptService>>();
        _mockProcessService = new Mock<IProcessService>();
        _mockAutomationService = new Mock<IAutomationService>();
        _testCaptureService = new TestSessionCaptureService(
            TestHelpers.CreateMockLogger<TestSessionCaptureService>().Object);

        _scriptService = new ScriptService(
            _mockLogger.Object,
            _mockProcessService.Object,
            _mockAutomationService.Object,
            _testCaptureService);

        _tempScriptPath = Path.Combine(Path.GetTempPath(), $"test_script_{Guid.NewGuid()}.json");
    }

    [Fact]
    public async Task ExecuteScriptAsync_WithNonExistentFile_ShouldReturnFailure()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/script.json";

        // Act
        var result = await _scriptService
            .ExecuteScriptAsync(nonExistentPath)
            .WithTimeout(DefaultTimeout, "ExecuteScriptAsync(nonexistent)");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithInvalidJson_ShouldReturnFailure()
    {
        // Arrange
        var invalidJson = "{ invalid json content";

        // Act
        var result = await _scriptService
            .ExecuteScriptContentAsync(invalidJson)
            .WithTimeout(DefaultTimeout, "ExecuteScriptContentAsync(invalid)");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithMissingTarget_ShouldNotAttachToAnArbitraryProcess()
    {
        // Arrange
        _mockProcessService.Setup(service => service.GetWpfProcessesAsync()).ReturnsAsync(new List<ProcessInfo>
        {
            new() { Id = 1234, Name = "UnrelatedApp", WindowTitle = "Unrelated Window" }
        });

        const string script = """
            { "name": "Missing target", "steps": [{ "action": "log", "description": "must not run" }] }
            """;

        // Act
        var result = await _scriptService.ExecuteScriptContentAsync(script);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("attach");
        _mockProcessService.Verify(service => service.GetWpfProcessesAsync(), Times.Never);
        _mockAutomationService.Verify(service => service.AttachToProcessAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithValidBasicScript_ShouldExecute()
    {
        // Arrange
        var validScript = @"{
            ""name"": ""Test Script"",
            ""description"": ""Simple test"",
            ""version"": ""1.0"",
            ""target"": {
                ""processName"": ""TestApp"",
                ""processId"": 0
            },
            ""steps"": [
                {
                    ""action"": ""log"",
                    ""description"": ""Test log""
                }
            ],
            ""options"": {
                ""stopOnError"": true
            }
        }";

        // Setup mocks
        _mockProcessService.Setup(p => p.FindProcessByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((ProcessInfo?)null);
        _mockAutomationService.Setup(a => a.AttachToProcessAsync(It.IsAny<int>()))
            .ReturnsAsync(false);

        // Act
        var result = await _scriptService
            .ExecuteScriptContentAsync(validScript)
            .WithTimeout(DefaultTimeout, "ExecuteScriptContentAsync(valid)");

        // Assert
        result.Should().NotBeNull();
        result.ExecutionLog.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WhenAutomationAttachmentFails_ShouldReturnFailure()
    {
        // Arrange
        _mockProcessService.Setup(service => service.GetWpfProcessesAsync()).ReturnsAsync(new List<ProcessInfo>
        {
            new() { Id = 1234, Name = "TestApp", WindowTitle = "Test Window" }
        });
        _mockAutomationService.Setup(service => service.AttachToProcessAsync(1234)).ReturnsAsync(false);

        const string script = """
            { "name": "Attachment failure", "target": { "processName": "TestApp" }, "steps": [] }
            """;

        // Act
        var result = await _scriptService.ExecuteScriptContentAsync(script);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("attach");
    }

    [Fact]
    public async Task ExecuteScriptContentAsync_WithUnknownAction_ShouldRecordStepFailure()
    {
        // Arrange
        _mockProcessService.Setup(service => service.GetWpfProcessesAsync()).ReturnsAsync(new List<ProcessInfo>
        {
            new() { Id = 1234, Name = "TestApp", WindowTitle = "Test Window" }
        });
        _mockAutomationService.Setup(service => service.AttachToProcessAsync(1234)).ReturnsAsync(true);

        const string script = """
            {
              "name": "Unknown action",
              "target": { "processName": "TestApp" },
              "steps": [{ "action": "does-not-exist" }],
              "options": { "stopOnError": false }
            }
            """;

        // Act
        var result = await _scriptService.ExecuteScriptContentAsync(script);

        // Assert
        result.Success.Should().BeFalse();
        result.StepsExecuted.Should().Be(1);
        result.StepsFailed.Should().Be(1);
    }

    [Fact]
    public async Task ValidateScriptAsync_WithNonExistentFile_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/script.json";

        // Act
        var result = await _scriptService
            .ValidateScriptAsync(nonExistentPath)
            .WithTimeout(DefaultTimeout, "ValidateScriptAsync(nonexistent)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateScriptAsync_WithValidScript_ShouldReturnTrue()
    {
        // Arrange
        var validScript = new Script
        {
            Name = "Test",
            Description = "Test script",
            Version = "1.0",
            Steps = new List<ScriptStep>
            {
                new ScriptStep { Action = "log", Description = "Test" }
            }
        };
        await _scriptService.SaveScriptAsync(validScript, _tempScriptPath);

        // Act
        var result = await _scriptService
            .ValidateScriptAsync(_tempScriptPath)
            .WithTimeout(DefaultTimeout, "ValidateScriptAsync(valid)");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LoadScriptAsync_WithNonExistentFile_ShouldReturnNull()
    {
        // Arrange
        var nonExistentPath = "/path/to/nonexistent/script.json";

        // Act
        var result = await _scriptService
            .LoadScriptAsync(nonExistentPath)
            .WithTimeout(DefaultTimeout, "LoadScriptAsync(nonexistent)");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadScriptAsync_WithValidScript_ShouldReturnScript()
    {
        // Arrange
        var validScript = new Script
        {
            Name = "Test Script",
            Description = "Test description",
            Version = "1.0"
        };
        await _scriptService.SaveScriptAsync(validScript, _tempScriptPath);

        // Act
        var result = await _scriptService
            .LoadScriptAsync(_tempScriptPath)
            .WithTimeout(DefaultTimeout, "LoadScriptAsync(valid)");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Script");
        result.Description.Should().Be("Test description");
        result.Version.Should().Be("1.0");
    }

    [Fact]
    public async Task LoadScriptAsync_WithLegacyFields_ShouldMapToCurrentSchema()
    {
        // Arrange
        const string legacyScript = """
            {
              "name": "Legacy script",
              "targetProcess": "TestWpfApp",
              "options": { "continueOnError": true, "timeout": 12000, "delayBetweenActions": 250 },
              "steps": [
                { "action": "log", "message": "Start" },
                { "action": "type", "selector": "TestTextBox", "text": "Legacy text", "waitAfter": 100 },
                { "action": "validate", "selector": "TestTextBox", "expectedValue": "Legacy text" },
                { "action": "wait", "duration": 500 },
                { "action": "selectCell", "selector": "TestDataGrid", "row": 2, "column": 1 }
              ]
            }
            """;
        await File.WriteAllTextAsync(_tempScriptPath, legacyScript);

        // Act
        var result = await _scriptService.LoadScriptAsync(_tempScriptPath);

        // Assert
        result.Should().NotBeNull();
        result!.Target.ProcessName.Should().Be("TestWpfApp");
        result.Options.StopOnError.Should().BeFalse();
        result.Options.DefaultTimeoutMs.Should().Be(12000);
        result.Options.DelayBetweenActionsMs.Should().Be(250);
        result.Steps[0].Description.Should().Be("Start");
        result.Steps[1].Element.Should().Be("id=TestTextBox");
        result.Steps[1].Value.Should().Be("Legacy text");
        result.Steps[1].DelayMs.Should().Be(100);
        result.Steps[2].Value.Should().Be("Legacy text");
        result.Steps[3].DelayMs.Should().Be(500);
        result.Steps[4].Row.Should().Be(2);
        result.Steps[4].Column.Should().Be(1);
    }

    [Fact]
    public async Task SaveScriptAsync_WithValidScript_ShouldCreateFile()
    {
        // Arrange
        var script = new Script
        {
            Name = "Save Test",
            Description = "Testing save functionality",
            Version = "1.0",
            Steps = new List<ScriptStep>
            {
                new ScriptStep { Action = "click", Element = "TestButton" }
            }
        };

        // Act
        await _scriptService.SaveScriptAsync(script, _tempScriptPath);

        // Assert
        File.Exists(_tempScriptPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(_tempScriptPath);
        content.Should().Contain("Save Test");
        content.Should().Contain("Testing save functionality");
    }

    [Fact]
    public async Task SaveScriptAsync_WithInvalidPath_ShouldThrowException()
    {
        // Arrange
        var script = new Script { Name = "Test" };
        var invalidPath = "/invalid/path/that/does/not/exist/script.json";

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
        {
            await _scriptService.SaveScriptAsync(script, invalidPath);
        });
    }

    [Fact]
    public async Task LoadAndSaveScript_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalScript = new Script
        {
            Name = "Round Trip Test",
            Description = "Testing data preservation",
            Version = "2.0",
            Target = new ScriptTarget
            {
                ProcessName = "TestApp",
                WindowTitle = "Test Window",
                TimeoutMs = 5000
            },
            Steps = new List<ScriptStep>
            {
                new ScriptStep
                {
                    Action = "click",
                    Element = "id=TestButton",
                    Description = "Click test button",
                    DelayMs = 500
                },
                new ScriptStep
                {
                    Action = "type",
                    Element = "id=TextBox",
                    Value = "Test input",
                    Description = "Type test text"
                }
            },
            Variables = new Dictionary<string, object>
            {
                { "testVar", "testValue" },
                { "number", 42 }
            },
            Options = new ScriptOptions
            {
                StopOnError = true,
                LogSteps = true,
                DefaultTimeoutMs = 3000
            }
        };

        // Act - Save and load
        await _scriptService.SaveScriptAsync(originalScript, _tempScriptPath);
        var loadedScript = await _scriptService.LoadScriptAsync(_tempScriptPath);

        // Assert
        loadedScript.Should().NotBeNull();
        loadedScript!.Name.Should().Be(originalScript.Name);
        loadedScript.Description.Should().Be(originalScript.Description);
        loadedScript.Version.Should().Be(originalScript.Version);
        loadedScript.Target.ProcessName.Should().Be(originalScript.Target.ProcessName);
        loadedScript.Steps.Should().HaveCount(2);
        loadedScript.Steps[0].Action.Should().Be("click");
        loadedScript.Steps[1].Action.Should().Be("type");
        loadedScript.Variables.Should().ContainKey("testVar");
        loadedScript.Options.StopOnError.Should().BeTrue();
    }

    public void Dispose()
    {
        if (File.Exists(_tempScriptPath))
        {
            try
            {
                File.Delete(_tempScriptPath);
            }
            catch (IOException)
            {
                // Ignore cleanup errors in tests
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
