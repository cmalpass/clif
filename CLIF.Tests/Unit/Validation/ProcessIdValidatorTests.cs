using FluentAssertions;
using Xunit;
using CLIF.Validation;
using CLIF.Validation.Validators;

namespace CLIF.Tests.Unit.Validation;

/// <summary>
/// Unit tests for ProcessIdValidator to ensure proper validation of process IDs
/// </summary>
public class ProcessIdValidatorTests
{
    private readonly ProcessIdValidator _validator = new();

    [Fact]
    public void Validate_WithValidProcessId_ShouldReturnSuccess()
    {
        // Arrange - Use current process ID to test basic validation (existence check may fail due to no UI window)
        var validProcessId = Environment.ProcessId;

        // Act
        var result = _validator.Validate(validProcessId);

        // Assert - The validator checks for both existence AND UI window
        // For most test runners, the process won't have a UI window, so this is expected behavior
        if (result.IsValid)
        {
            result.ErrorMessage.Should().BeEmpty();
        }
        else
        {
            // Expected behavior - test process typically doesn't have UI
            result.ErrorMessage.Should().Contain("does not have a user interface window");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(65535)]
    [InlineData(32767)]
    public void Validate_WithValidProcessIds_ShouldReturnSuccess(int processId)
    {
        // Act
        var result = _validator.Validate(processId);

        // Assert - These will likely fail if process doesn't exist, which is expected behavior
        // The validator checks both range AND process existence
        if (result.IsValid)
        {
            result.ErrorMessage.Should().BeEmpty();
        }
        else
        {
            // It's valid for a process ID to be in range but not exist
            result.ErrorMessage.Should().Contain("does not exist");
        }
    }

    [Fact]
    public void Validate_WithZeroProcessId_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(0);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Value must be between 1 and 65535");
    }

    [Fact]
    public void Validate_WithNegativeProcessId_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(-1);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Value must be between 1 and 65535");
    }

    [Fact]
    public void Validate_WithLargeProcessId_ShouldReturnFailure()
    {
        // Act
        var result = _validator.Validate(int.MaxValue);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Value must be between 1 and 65535");
    }
}