using CLIF.Validation;
using CLIF.Validation.Validators;
using CLIF.Tests.WpfUI;
using FluentAssertions;
using Xunit;

namespace CLIF.Tests.Integration;

/// <summary>
/// Comprehensive validation harness to ensure all validators work correctly
/// </summary>
[Collection("WpfUI")]
public class ValidationHarnessTests
{
    private readonly WpfTestAppFixture _wpfTestApp;

    public ValidationHarnessTests(WpfTestAppFixture wpfTestApp)
    {
        _wpfTestApp = wpfTestApp;
    }

    [Fact]
    public void ProcessIdValidator_WithRunningUiProcess_ShouldPass()
    {
        // Arrange
        _wpfTestApp.SkipIfUnavailable();
        var validator = new ProcessIdValidator();
        var processId = _wpfTestApp.App!.ProcessId;

        // Act
        var result = validator.Validate(processId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void ProcessIdValidator_WithInvalidProcessIds_ShouldFail(int processId)
    {
        // Arrange
        var validator = new ProcessIdValidator();

        // Act
        var result = validator.Validate(processId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("id=TestButton")]
    [InlineData("name=Submit")]
    [InlineData("class=ButtonClass")]
    [InlineData("type=Button")]
    public void ElementSelectorValidator_WithValidSelectors_ShouldPass(string selector)
    {
        // Arrange
        var validator = new ElementSelectorValidator();

        // Act
        var result = validator.Validate(selector);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void ElementSelectorValidator_WithInvalidSelectors_ShouldFail(string? selector)
    {
        // Arrange
        var validator = new ElementSelectorValidator();

        // Act
        var result = validator.Validate(selector!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("C:\\temp\\script.json")]
    [InlineData("/usr/local/bin/script.json")]
    [InlineData("./relative/path/script.json")]
    [InlineData("script.json")]
    public void FilePathValidator_WithValidPaths_ShouldPass(string path)
    {
        // Arrange
        var validator = new FilePathValidator();

        // Act
        var result = validator.Validate(path);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void FilePathValidator_WithInvalidPaths_ShouldFail(string? path)
    {
        // Arrange
        var validator = new FilePathValidator();

        // Act
        var result = validator.Validate(path!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("Test123")]
    [InlineData("Special chars: !@#$%")]
    [InlineData("Newline\nText")]
    public void TextInputValidator_WithValidText_ShouldPass(string text)
    {
        // Arrange
        var validator = new TextInputValidator();

        // Act
        var result = validator.Validate(text);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    public void TextInputValidator_WithNull_ShouldFail(string? text)
    {
        // Arrange
        var validator = new TextInputValidator();

        // Act
        var result = validator.Validate(text!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidationResult_Success_ShouldHaveCorrectState()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_Failure_ShouldHaveCorrectState()
    {
        // Arrange
        var errorMessage = "Test error message";

        // Act
        var result = ValidationResult.Failure(errorMessage);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public void AllValidators_ShouldBeInstantiable()
    {
        // This test ensures all validators can be created without errors
        
        // Act
        var processValidator = new ProcessIdValidator();
        var fileValidator = new FilePathValidator();
        var elementValidator = new ElementSelectorValidator();
        var textValidator = new TextInputValidator();

        // Assert
        processValidator.Should().NotBeNull();
        fileValidator.Should().NotBeNull();
        elementValidator.Should().NotBeNull();
        textValidator.Should().NotBeNull();
    }

    [Theory]
    [InlineData("id=Button1")]
    [InlineData("name=Submit Button")]
    [InlineData("class=MyButtonClass")]
    [InlineData("type=Button")]
    public void ElementSelectorValidator_WithDifferentPrefixes_ShouldRecognizeFormat(string selector)
    {
        // Arrange
        var validator = new ElementSelectorValidator();

        // Act
        var result = validator.Validate(selector);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void ProcessIdValidator_BoundaryValues_ShouldHandleCorrectly()
    {
        // Arrange
        var validator = new ProcessIdValidator();

        // A positive integer is not sufficient: it must name a live UI process.
        // This test covers only range boundaries; the live-process requirement is
        // exercised above using the shared TestWpfApp fixture.
        validator.Validate(0).IsValid.Should().BeFalse();
        validator.Validate(-1).IsValid.Should().BeFalse();
        validator.Validate(65536).IsValid.Should().BeFalse();
        validator.Validate(int.MaxValue).IsValid.Should().BeFalse();
    }
}
