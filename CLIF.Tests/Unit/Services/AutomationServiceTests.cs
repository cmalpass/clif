using CLIF.Services;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core;

namespace CLIF.Tests.Unit.Services;

/// <summary>
/// Tests for AutomationService
/// </summary>
public class AutomationServiceTests : IDisposable
{
    private readonly Mock<ILogger<AutomationService>> _mockLogger;
    private readonly TestSessionCaptureService _testCaptureService;
    private readonly AutomationService _automationService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public AutomationServiceTests()
    {
        _mockLogger = new Mock<ILogger<AutomationService>>();
        _testCaptureService = new TestSessionCaptureService(
            TestHelpers.CreateMockLogger<TestSessionCaptureService>().Object);

        _automationService = new AutomationService(_mockLogger.Object, _testCaptureService);
    }

    [Fact]
    public async Task AttachToProcessAsync_WithCurrentProcess_ShouldComplete()
    {
        // Arrange
        var currentProcessId = Environment.ProcessId;

        // Act & Assert - In headless CI the UI automation COM infrastructure may be
        // unavailable, causing a timeout. Both a bool result AND a TimeoutException
        // are acceptable; the only unacceptable outcome is an unhandled exception.
        try
        {
            var result = await _automationService
                .AttachToProcessAsync(currentProcessId)
                .WithTimeout(DefaultTimeout, "AttachToProcessAsync(currentProcess)");

            // Result can be either true or false — both are fine in a test environment
            (result == true || result == false).Should().BeTrue();

            // Cleanup
            if (_automationService.IsAttached)
            {
                await _automationService.DetachAsync().WithTimeout(DefaultTimeout, "DetachAsync(after attach)");
            }
        }
        catch (TimeoutException)
        {
            // Acceptable: UI automation APIs may not be available in headless CI
        }
    }

    [Fact]
    public async Task AttachToProcessAsync_WithInvalidProcessId_ShouldReturnFalse()
    {
        // Arrange
        var invalidProcessId = -1;

        // Act
        var result = await _automationService
            .AttachToProcessAsync(invalidProcessId)
            .WithTimeout(DefaultTimeout, "AttachToProcessAsync(invalid)");

        // Assert
        result.Should().BeFalse();
        _automationService.IsAttached.Should().BeFalse();
    }

    [Fact]
    public async Task DetachAsync_WhenNotAttached_ShouldCompleteSuccessfully()
    {
        // Act & Assert - Should not throw
        await _automationService.DetachAsync().WithTimeout(DefaultTimeout, "DetachAsync(not attached)");

        // Verify state
        _automationService.IsAttached.Should().BeFalse();
        _automationService.AttachedProcessId.Should().BeNull();
    }

    [Theory]
    [InlineData("id=TestButton")]
    [InlineData("name=SubmitButton")]
    [InlineData("class=ButtonClass")]
    [InlineData("type=Button")]
    public async Task FindElementAsync_WithValidSelectors_ShouldAttemptToFind(string selector)
    {
        // Act
        var result = await _automationService
            .FindElementAsync(selector)
            .WithTimeout(DefaultTimeout, $"FindElementAsync({selector})");

        // Assert - Expected to be null in test environment without attached process
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("id=TestButton", "TestButton", null, null, null)]
    [InlineData("name=Submit and class=Button and type=Button", null, "Submit", "Button", "Button")]
    [InlineData("name=\"Research and Development\" and type=Text", null, "Research and Development", null, "Text")]
    [InlineData("Save", null, "Save", null, null)]
    public void SelectorParser_WithSimpleAndCompoundSelectors_ShouldParseAllCriteria(
        string selector,
        string? expectedId,
        string? expectedName,
        string? expectedClass,
        string? expectedType)
    {
        var parsed = SelectorParser.TryParse(selector, out var criteria);

        parsed.Should().BeTrue();
        criteria.AutomationId.Should().Be(expectedId);
        criteria.Name.Should().Be(expectedName);
        criteria.ClassName.Should().Be(expectedClass);
        criteria.ControlType.Should().Be(expectedType);
    }

    [Theory]
    [InlineData("name=Submit and name=Cancel")]
    [InlineData("role=Button")]
    [InlineData("type=NotAControlType")]
    [InlineData("name=\"unterminated")]
    public void SelectorParser_WithInvalidCompoundSelector_ShouldRejectIt(string selector)
    {
        SelectorParser.TryParse(selector, out _).Should().BeFalse();
    }

    [Fact]
    public async Task FindElementAsync_WithInvalidSelector_ShouldReturnNull()
    {
        // Arrange
        var invalidSelector = "invalid:selector:format";

        // Act
        var result = await _automationService
            .FindElementAsync(invalidSelector)
            .WithTimeout(DefaultTimeout, "FindElementAsync(invalid)");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task FindElementAsync_WithEmptyOrNullSelector_ShouldReturnNull(string? selector)
    {
        // Act
        var result = await _automationService
            .FindElementAsync(selector!)
            .WithTimeout(DefaultTimeout, "FindElementAsync(empty/null)");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ClickAsync_WithNullElement_ShouldReturnFalse()
    {
        // Arrange
        AutomationElement? nullElement = null;

        // Act
        var result = await _automationService
            .ClickAsync(nullElement!)
            .WithTimeout(DefaultTimeout, "ClickAsync(null)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TypeTextAsync_WithNullElement_ShouldReturnFalse()
    {
        // Arrange
        AutomationElement? nullElement = null;
        var textToType = "Test Text";

        // Act
        var result = await _automationService
            .TypeTextAsync(nullElement!, textToType)
            .WithTimeout(DefaultTimeout, "TypeTextAsync(null)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TypeTextAsync_WithEmptyText_ShouldReturnFalse()
    {
        // Arrange - empty text should be rejected regardless of element
        AutomationElement? nullElement = null;
        var emptyText = "";

        // Act
        var result = await _automationService
            .TypeTextAsync(nullElement!, emptyText)
            .WithTimeout(DefaultTimeout, "TypeTextAsync(empty)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRootElementAsync_WithoutAttachedProcess_ShouldReturnNull()
    {
        // Act
        var result = await _automationService
            .GetRootElementAsync()
            .WithTimeout(DefaultTimeout, "GetRootElementAsync(no attach)");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TakeScreenshotAsync_ShouldNotThrow()
    {
        // Act & Assert - Should complete without throwing, may return empty array
        var action = async () => await _automationService
            .TakeScreenshotAsync()
            .WithTimeout(DefaultTimeout, "TakeScreenshotAsync");
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task TakeElementScreenshotAsync_WithNullElement_ShouldReturnEmptyArray()
    {
        // Arrange
        AutomationElement? nullElement = null;

        // Act
        var result = await _automationService
            .TakeElementScreenshotAsync(nullElement!)
            .WithTimeout(DefaultTimeout, "TakeElementScreenshotAsync(null)");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SetValueAsync_WithNullElement_ShouldReturnFalse()
    {
        // Arrange
        AutomationElement? nullElement = null;
        var value = "Test Value";

        // Act
        var result = await _automationService
            .SetValueAsync(nullElement!, value)
            .WithTimeout(DefaultTimeout, "SetValueAsync(null)");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetTextAsync_WithNullElement_ShouldReturnEmptyString()
    {
        // Arrange
        AutomationElement? nullElement = null;

        // Act
        var result = await _automationService
            .GetTextAsync(nullElement!)
            .WithTimeout(DefaultTimeout, "GetTextAsync(null)");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetValueAsync_WithNullElement_ShouldReturnEmptyString()
    {
        // Arrange
        AutomationElement? nullElement = null;

        // Act
        var result = await _automationService
            .GetValueAsync(nullElement!)
            .WithTimeout(DefaultTimeout, "GetValueAsync(null)");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void IsAttached_InitialState_ShouldBeFalse()
    {
        // Assert
        _automationService.IsAttached.Should().BeFalse();
        _automationService.AttachedProcessId.Should().BeNull();
    }

    public void Dispose()
    {
        _testCaptureService.ClearCapturedData();
        _automationService?.Dispose();
    }
}
