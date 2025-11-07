# CLIF Validation Patterns

This document describes the validation patterns and best practices used throughout the CLIF project.

## Overview

CLIF implements a comprehensive validation framework to ensure data integrity, security, and reliability. All user inputs, command parameters, and external data are validated before processing.

## Validation Architecture

### Validation Framework Components

1. **Validators** - Individual validation classes for specific data types
2. **ValidationResult** - Immutable result object indicating success/failure
3. **Security Helpers** - Input sanitization and security checks
4. **Command Validation** - System.CommandLine built-in validation

## Available Validators

### ProcessIdValidator

Validates process IDs to ensure they are positive integers.

```csharp
var validator = new ProcessIdValidator();
var result = validator.Validate(processId);

if (!result.IsValid)
{
    Console.WriteLine($"Invalid process ID: {result.ErrorMessage}");
    return;
}
```

**Validation Rules:**
- Must be greater than 0
- Must be a valid integer

**Usage Examples:**
```csharp
// ✓ Valid
validator.Validate(1234);      // Valid process ID
validator.Validate(1);          // Minimum valid value

// ✗ Invalid  
validator.Validate(0);          // Zero is invalid
validator.Validate(-1);         // Negative is invalid
```

### FilePathValidator

Validates file paths for security and correctness.

```csharp
var validator = new FilePathValidator();
var result = validator.Validate(filePath);

if (!result.IsValid)
{
    Console.WriteLine($"Invalid file path: {result.ErrorMessage}");
    return;
}
```

**Validation Rules:**
- Must not be null or empty
- Must not contain invalid path characters
- Path should be properly formatted

**Usage Examples:**
```csharp
// ✓ Valid
validator.Validate("C:\\scripts\\test.json");          // Windows absolute
validator.Validate("/home/user/scripts/test.json");    // Unix absolute
validator.Validate("./scripts/test.json");             // Relative path
validator.Validate("script.json");                     // Current directory

// ✗ Invalid
validator.Validate("");                                // Empty
validator.Validate(null);                              // Null
validator.Validate("   ");                             // Whitespace only
```

### ElementSelectorValidator

Validates element selectors for UI automation.

```csharp
var validator = new ElementSelectorValidator();
var result = validator.Validate(selector);

if (!result.IsValid)
{
    Console.WriteLine($"Invalid selector: {result.ErrorMessage}");
    return;
}
```

**Validation Rules:**
- Must not be null or empty
- Accepts various selector formats:
  - `id=AutomationId`
  - `name=ElementName`
  - `class=ClassName`
  - `type=ControlType`
  - Plain text (defaults to name search)

**Usage Examples:**
```csharp
// ✓ Valid
validator.Validate("id=TestButton");           // By Automation ID
validator.Validate("name=Submit");             // By name
validator.Validate("class=ButtonClass");       // By class
validator.Validate("type=Button");             // By control type
validator.Validate("SubmitButton");            // Plain text (name search)

// ✗ Invalid
validator.Validate("");                        // Empty
validator.Validate(null);                      // Null
validator.Validate("   ");                     // Whitespace only
```

### TextInputValidator

Validates text input for type commands and text entry.

```csharp
var validator = new TextInputValidator();
var result = validator.Validate(text);

if (!result.IsValid)
{
    Console.WriteLine($"Invalid text: {result.ErrorMessage}");
    return;
}
```

**Validation Rules:**
- Must not be null
- Must not be empty or whitespace-only (by default)
- Can contain any characters including special chars and newlines
- Use `allowEmpty = true` parameter if empty strings should be allowed

**Usage Examples:**
```csharp
// ✓ Valid
validator.Validate("Hello World");                     // Normal text
validator.Validate("Test123!@#");                      // With special chars
validator.Validate("Line1\nLine2");                    // With newline

// Valid only with allowEmpty = true
var validatorWithEmpty = new TextInputValidator(allowEmpty: true);
validatorWithEmpty.Validate("");                       // Empty (for clearing)

// ✗ Invalid
validator.Validate(null);                              // Null
validator.Validate("");                                // Empty (when allowEmpty = false)
validator.Validate("   ");                             // Whitespace only
```

## ValidationResult Pattern

The `ValidationResult` class provides a consistent way to return validation outcomes.

### Creating Validation Results

```csharp
// Success
var success = ValidationResult.Success();

// Failure with message
var failure = ValidationResult.Failure("Invalid input: reason");
```

### Using Validation Results

```csharp
var result = validator.Validate(input);

if (result.IsValid)
{
    // Proceed with operation
    ProcessInput(input);
}
else
{
    // Handle validation error
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
    return;
}
```

### Properties

- `IsValid` (bool) - Indicates if validation passed
- `ErrorMessage` (string) - Descriptive error message (empty if valid)

## Security Context

The `SecurityContext` provides application-wide security settings.

```csharp
var context = SecurityContext.Current;
var maxLength = context.MaxInputLength;
var allowedChars = context.AllowedSpecialCharacters;
```

**Features:**
- Singleton pattern for consistent security settings
- Configurable input length limits
- Allowed special characters whitelist
- Process access controls

## Sanitization Helpers

The `SanitizationHelper` provides methods for cleaning and validating user input.

```csharp
// Sanitize text input
var sanitized = SanitizationHelper.SanitizeTextInput(userInput);

// Remove potentially dangerous content
var clean = SanitizationHelper.RemoveScriptTags(input);
```

**Sanitization Methods:**
- `SanitizeTextInput` - General text sanitization
- `RemoveScriptTags` - Remove script tags and similar
- `ValidatePathSecurity` - Check path for traversal attacks
- `SanitizeProcessId` - Validate and clean process IDs

## Command-Level Validation

System.CommandLine provides built-in validation for command options and arguments.

### Example: Required Options

```csharp
var processIdOption = new Option<int>(
    "--process-id",
    "The process ID to attach to")
{
    IsRequired = true
};

var elementOption = new Option<string>(
    "--element", 
    "Element selector")
{
    IsRequired = true
};
```

### Example: Value Constraints

```csharp
var depthOption = new Option<int>(
    "--depth",
    getDefaultValue: () => 5,
    description: "Maximum tree depth");
depthOption.AddValidator(result =>
{
    var value = result.GetValueForOption(depthOption);
    if (value < 1 || value > 20)
    {
        result.ErrorMessage = "Depth must be between 1 and 20";
    }
});
```

## Validation Patterns in Services

### Pattern 1: Early Validation

Validate inputs at the beginning of methods:

```csharp
public async Task<bool> ProcessAsync(int processId, string selector)
{
    // Validate immediately
    var pidValidator = new ProcessIdValidator();
    var pidResult = pidValidator.Validate(processId);
    if (!pidResult.IsValid)
    {
        _logger.LogError($"Invalid process ID: {pidResult.ErrorMessage}");
        return false;
    }

    var selectorValidator = new ElementSelectorValidator();
    var selectorResult = selectorValidator.Validate(selector);
    if (!selectorResult.IsValid)
    {
        _logger.LogError($"Invalid selector: {selectorResult.ErrorMessage}");
        return false;
    }

    // Proceed with validated inputs
    return await PerformOperationAsync(processId, selector);
}
```

### Pattern 2: Validation with Logging

Always log validation failures:

```csharp
var result = validator.Validate(input);
if (!result.IsValid)
{
    _logger.LogWarning($"Validation failed: {result.ErrorMessage}");
    return ValidationResult.Failure(result.ErrorMessage);
}
```

### Pattern 3: Defensive Programming

Even after validation, handle unexpected cases:

```csharp
public async Task<Element?> FindElementAsync(string selector)
{
    // Validate
    var result = _validator.Validate(selector);
    if (!result.IsValid)
        return null;

    try
    {
        // Sanitize even after validation
        var safeSelector = SanitizationHelper.SanitizeTextInput(selector);
        
        // Proceed with operation
        return await FindElementInternalAsync(safeSelector);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error finding element");
        return null;
    }
}
```

## Script Validation

Scripts are validated at multiple levels:

### 1. JSON Schema Validation

```csharp
public async Task<bool> ValidateScriptAsync(string scriptPath)
{
    try
    {
        var script = await LoadScriptAsync(scriptPath);
        return script != null;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Invalid JSON in script");
        return false;
    }
}
```

### 2. Semantic Validation

```csharp
private bool ValidateScriptSemantics(Script script)
{
    if (string.IsNullOrEmpty(script.Name))
        return false;
    
    if (script.Steps == null || script.Steps.Count == 0)
        return false;
    
    foreach (var step in script.Steps)
    {
        if (string.IsNullOrEmpty(step.Action))
            return false;
    }
    
    return true;
}
```

### 3. Security Validation

```csharp
private bool ValidateScriptSecurity(Script script)
{
    // Check for path traversal in script paths
    if (script.Options?.OutputPath != null)
    {
        if (!SanitizationHelper.ValidatePathSecurity(script.Options.OutputPath))
            return false;
    }
    
    // Validate all selectors in steps
    var validator = new ElementSelectorValidator();
    foreach (var step in script.Steps)
    {
        if (!string.IsNullOrEmpty(step.Element))
        {
            var result = validator.Validate(step.Element);
            if (!result.IsValid)
                return false;
        }
    }
    
    return true;
}
```

## Testing Validation

### Unit Test Pattern

```csharp
[Theory]
[InlineData(1, true)]
[InlineData(1234, true)]
[InlineData(0, false)]
[InlineData(-1, false)]
public void Validate_WithVariousInputs_ShouldReturnExpectedResult(
    int input, 
    bool expectedValid)
{
    // Arrange
    var validator = new ProcessIdValidator();

    // Act
    var result = validator.Validate(input);

    // Assert
    result.IsValid.Should().Be(expectedValid);
    if (!expectedValid)
    {
        result.ErrorMessage.Should().NotBeEmpty();
    }
}
```

### Integration Test Pattern

```csharp
[Fact]
public async Task FullValidationPipeline_WithInvalidInput_ShouldRejectEarly()
{
    // Arrange
    var invalidProcessId = -1;
    var selector = "id=TestButton";

    // Act
    var attached = await _automationService.AttachToProcessAsync(invalidProcessId);

    // Assert
    attached.Should().BeFalse();
    // Should not attempt to find element with invalid process
}
```

## Best Practices

### 1. Validate Early
Validate inputs as soon as they enter your system, before any processing.

### 2. Fail Fast
Return immediately on validation failure rather than continuing with invalid data.

### 3. Provide Clear Messages
Always provide descriptive error messages that help users understand what's wrong.

### 4. Log Validation Failures
Log all validation failures for debugging and security monitoring.

### 5. Sanitize After Validation
Even validated input should be sanitized before use in sensitive operations.

### 6. Use Type Safety
Leverage C#'s type system and nullable reference types to catch errors at compile time.

### 7. Test Boundaries
Always test edge cases, boundary values, and invalid inputs in your tests.

### 8. Document Validation Rules
Clearly document what constitutes valid input for each validator.

## Common Validation Scenarios

### Scenario 1: Command Line Input
```csharp
// User runs: clif click --process-id 1234 --element "id=Button"
// Validation happens at:
// 1. System.CommandLine level (required options)
// 2. Command handler level (value validation)
// 3. Service level (business logic validation)
```

### Scenario 2: Script Execution
```csharp
// User runs: clif script test.json
// Validation happens at:
// 1. File existence check
// 2. JSON parsing validation
// 3. Script structure validation
// 4. Each step parameter validation
// 5. Runtime element selector validation
```

### Scenario 3: Interactive Mode
```csharp
// User types: click id=TestButton
// Validation happens at:
// 1. Command parsing (syntax)
// 2. Parameter validation (selector format)
// 3. Runtime validation (element exists)
// 4. Operation validation (element is clickable)
```

## Error Handling Integration

Validation and error handling work together:

```csharp
try
{
    var result = validator.Validate(input);
    if (!result.IsValid)
    {
        throw new ValidationException(result.ErrorMessage);
    }
    
    // Process validated input
    await PerformOperationAsync(input);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed");
    return false;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    throw;
}
```

## Future Enhancements

Potential improvements to the validation framework:

1. **Fluent Validation** - More expressive validation rules
2. **Async Validation** - For validation requiring I/O operations
3. **Validation Pipeline** - Chain multiple validators
4. **Custom Validation Attributes** - Declarative validation
5. **Validation Caching** - Cache validation results for performance

## Resources

- See `CLIF.Tests/Integration/ValidationHarnessTests.cs` for comprehensive validation tests
- See `CLIF/Validation/` for validator implementations
- See `CLIF/Security/` for security-related validation
