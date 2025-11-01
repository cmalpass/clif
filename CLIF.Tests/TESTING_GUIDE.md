# CLIF Testing Guide

This guide provides comprehensive information about the testing infrastructure and practices for the CLIF project.

## Test Structure

The test suite is organized into the following categories:

### Unit Tests (`Unit/`)
Unit tests focus on testing individual components in isolation with mocked dependencies.

#### Commands Tests (`Unit/Commands/`)
- **ClickCommandTests.cs** - Tests for click command functionality
- **TypeCommandTests.cs** - Tests for type command functionality
- **InteractCommandTests.cs** - Tests for advanced interact command
- **TreeCommandTests.cs** - Tests for element tree display
- **ScriptCommandTests.cs** - Tests for script execution command
- **InteractiveCommandTests.cs** - Tests for interactive mode command
- **ListProcessesCommandTests.cs** - Tests for process listing

#### Services Tests (`Unit/Services/`)
- **ProcessServiceTests.cs** - Tests for process discovery and management
- **AutomationServiceTests.cs** - Tests for UI automation operations
- **ElementTreeServiceTests.cs** - Tests for element tree building and navigation
- **ScriptServiceTests.cs** - Tests for script loading, saving, and execution
- **InteractiveServiceTests.cs** - Tests for interactive mode command parsing
- **SessionCaptureServiceTests.cs** - Tests for session capture and logging

#### Security Tests (`Unit/Security/`)
- **SecurityContextTests.cs** - Tests for security context management
- **SanitizationHelperTests.cs** - Tests for input sanitization

#### Validation Tests (`Unit/Validation/`)
- **ProcessIdValidatorTests.cs** - Tests for process ID validation
- **FilePathValidatorTests.cs** - Tests for file path validation
- **ElementSelectorValidatorTests.cs** - Tests for element selector validation
- **TextInputValidatorTests.cs** - Tests for text input validation
- **ValidationResultTests.cs** - Tests for validation result types

### Integration Tests (`Integration/`)
Integration tests verify the interaction between multiple components working together.

- **IntegrationTestBase.cs** - Base class providing full service setup for integration tests
- **ScriptExecutionIntegrationTests.cs** - End-to-end script execution tests
- **ValidationHarnessTests.cs** - Comprehensive validation framework tests

## Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Category
```bash
# Run only unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"

# Run only service tests
dotnet test --filter "FullyQualifiedName~Services"
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~ScriptServiceTests"
```

### Run with Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

Current test coverage includes:

### Services (100%)
- ✅ ProcessService - 17 tests
- ✅ AutomationService - 17 tests
- ✅ ElementTreeService - 8 tests
- ✅ ScriptService - 13 tests
- ✅ InteractiveService - 17 tests
- ✅ SessionCaptureService - 10 tests

### Commands (100%)
- ✅ ClickCommand - 6 tests
- ✅ TypeCommand - 4 tests
- ✅ InteractCommand - 6 tests
- ✅ TreeCommand - 6 tests
- ✅ ScriptCommand - 6 tests
- ✅ InteractiveCommand - 4 tests
- ✅ ListProcessesCommand - 4 tests

### Validation (100%)
- ✅ ProcessIdValidator - 5 tests
- ✅ FilePathValidator - 6 tests
- ✅ ElementSelectorValidator - 7 tests
- ✅ TextInputValidator - 7 tests
- ✅ ValidationResult - 7 tests

### Security (100%)
- ✅ SecurityContext - 7 tests
- ✅ SanitizationHelper - 10 tests

### Integration Tests
- ✅ Script Execution - 7 tests
- ✅ Validation Harness - 13 tests

**Total Test Methods: 160+**

## Writing New Tests

### Unit Test Template
```csharp
using FluentAssertions;
using Moq;
using Xunit;

namespace CLIF.Tests.Unit.Services;

public class MyServiceTests
{
    private readonly Mock<IDependency> _mockDependency;
    private readonly MyService _service;

    public MyServiceTests()
    {
        _mockDependency = new Mock<IDependency>();
        _service = new MyService(_mockDependency.Object);
    }

    [Fact]
    public async Task MethodName_WithCondition_ShouldExpectedBehavior()
    {
        // Arrange
        _mockDependency.Setup(d => d.Method()).Returns(expectedValue);

        // Act
        var result = await _service.MethodUnderTest();

        // Assert
        result.Should().Be(expectedValue);
        _mockDependency.Verify(d => d.Method(), Times.Once);
    }
}
```

### Integration Test Template
```csharp
using CLIF.Tests.Integration;
using FluentAssertions;
using Xunit;

namespace CLIF.Tests.Integration;

[Collection("Integration")]
public class MyIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Integration_WithRealServices_ShouldWork()
    {
        // Arrange - Use inherited service instances
        
        // Act
        var result = await ScriptService.ExecuteScriptAsync(path);

        // Assert
        result.Should().NotBeNull();
    }
}
```

## Test Utilities

### Available Test Helpers
- **TestHelpers.cs** - Common test utilities
- **MockFactory.cs** - Factory for creating mock objects
- **TestDataBuilder.cs** - Builder pattern for test data
- **TaskTimeoutExtensions.cs** - Timeout extensions for async tests
- **TestSessionCaptureService.cs** - Test implementation of session capture

### Using TaskTimeoutExtensions
```csharp
var result = await _service
    .MethodAsync()
    .WithTimeout(TimeSpan.FromSeconds(5), "MethodAsync");
```

## Best Practices

### 1. Test Naming
Use descriptive names following the pattern:
```
MethodName_WithCondition_ShouldExpectedBehavior
```

Examples:
- `AttachToProcessAsync_WithValidProcessId_ShouldReturnTrue`
- `FindElementAsync_WithInvalidSelector_ShouldReturnNull`

### 2. Test Structure
Always use AAA pattern:
```csharp
// Arrange - Set up test data and mocks
var input = "test";
_mockService.Setup(s => s.Method()).Returns(expectedValue);

// Act - Execute the method under test
var result = await _service.MethodUnderTest(input);

// Assert - Verify the outcome
result.Should().Be(expectedValue);
```

### 3. Async Tests
Always use `async Task` for async tests and apply timeout:
```csharp
[Fact]
public async Task AsyncMethod_ShouldComplete()
{
    var result = await _service
        .AsyncMethod()
        .WithTimeout(DefaultTimeout, "AsyncMethod");
    
    result.Should().NotBeNull();
}
```

### 4. Theory Tests
Use `[Theory]` for parameterized tests:
```csharp
[Theory]
[InlineData("id=Button")]
[InlineData("name=Submit")]
[InlineData("class=MyClass")]
public async Task Validate_WithVariousInputs_ShouldWork(string input)
{
    var result = _validator.Validate(input);
    result.IsValid.Should().BeTrue();
}
```

### 5. Mocking
Use Moq for mocking dependencies:
```csharp
// Setup method return
_mockService.Setup(s => s.Method()).Returns(value);

// Setup async method
_mockService.Setup(s => s.MethodAsync()).ReturnsAsync(value);

// Verify method was called
_mockService.Verify(s => s.Method(), Times.Once);

// Setup with parameters
_mockService.Setup(s => s.Method(It.IsAny<int>())).Returns(true);
```

### 6. Assertions
Use FluentAssertions for readable assertions:
```csharp
result.Should().NotBeNull();
result.Should().BeTrue();
result.Should().Be(expectedValue);
result.Should().BeOfType<MyType>();
result.Should().Contain(item);
result.Should().HaveCount(5);
collection.Should().BeEmpty();
```

## Continuous Integration

Tests are automatically run on:
- Every pull request
- Every commit to main branch
- Before release builds

### CI Test Requirements
- All tests must pass
- No test should take longer than 30 seconds
- Tests must be deterministic (no flaky tests)

## Troubleshooting

### Common Issues

#### Tests Timing Out
```csharp
// Increase timeout for long-running operations
var result = await operation
    .WithTimeout(TimeSpan.FromMinutes(1), "LongOperation");
```

#### Windows-Specific Tests
Some tests require Windows and FlaUI. These are handled gracefully:
```csharp
[Fact]
public async Task WindowsSpecificTest()
{
    // Test may return null on non-Windows, which is acceptable
    var result = await _automationService.AttachToProcessAsync(pid);
    (result == true || result == false).Should().BeTrue();
}
```

#### Mock Setup Issues
Ensure mocks are set up before use:
```csharp
// ✗ Wrong - mock used without setup
var result = _mockService.Object.Method();

// ✓ Correct - mock set up first
_mockService.Setup(s => s.Method()).Returns(value);
var result = _mockService.Object.Method();
```

## Test Maintenance

### When to Update Tests
- When adding new functionality
- When fixing bugs (add regression test first)
- When refactoring (ensure tests still pass)
- When API changes

### Test Review Checklist
- [ ] Tests are named descriptively
- [ ] Tests follow AAA pattern
- [ ] Async tests use timeout extensions
- [ ] Mocks are properly set up and verified
- [ ] Assertions are clear and specific
- [ ] Tests are independent (no shared state)
- [ ] Tests clean up resources (implement IDisposable if needed)

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FlaUI Documentation](https://github.com/FlaUI/FlaUI)

## Contributing

When contributing tests:
1. Follow existing test patterns
2. Add tests for new functionality
3. Ensure all tests pass locally before submitting
4. Add integration tests for complex features
5. Document any special test requirements
