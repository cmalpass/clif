using FluentAssertions;
using Xunit;
using CLIF.Security;
using CLIF.Validation;

namespace CLIF.Tests.Unit.Security;

/// <summary>
/// Unit tests for SecurityContext to ensure proper security context management
/// </summary>
public class SecurityContextTests
{
    [Fact]
    public void Instance_ShouldReturnSameInstance()
    {
        // Act
        var instance1 = SecurityContext.Current;
        var instance2 = SecurityContext.Current;
        
        // Assert
        instance1.Should().NotBeNull();
        instance2.Should().NotBeNull();
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void IsElevated_ShouldReturnBooleanValue()
    {
        // Arrange
        var context = SecurityContext.Current;
        
        // Act
        var isElevated = context.IsElevated;
        
        // Assert
        Assert.IsType<bool>(isElevated);
    }

    [Fact]
    public void CurrentUser_ShouldReturnValidUserIdentity()
    {
        // Arrange
        var context = SecurityContext.Current;
        
        // Act
        var currentUser = context.CurrentUser;
        
        // Assert
        currentUser.Should().NotBeNull();
        currentUser.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateFileAccess_WithValidPath_ShouldReturnValidationResult()
    {
        // Arrange
        var context = SecurityContext.Current;
        const string testPath = @"C:\Windows\System32";
        
        // Act
        var result = context.ValidateFileAccess(testPath);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationResult>();
    }

    [Fact]
    public void IsAdministrator_ShouldReturnBooleanValue()
    {
        // Arrange
        var context = SecurityContext.Current;
        
        // Act
        var isAdmin = context.IsAdministrator;
        
        // Assert
        Assert.IsType<bool>(isAdmin);
    }

    [Fact]
    public void SecurityContext_ShouldProvideConsistentResults()
    {
        // Arrange
        var context = SecurityContext.Current;
        
        // Act - Call methods multiple times
        var isElevated1 = context.IsElevated;
        var isElevated2 = context.IsElevated;
        var currentUser1 = context.CurrentUser;
        var currentUser2 = context.CurrentUser;
        
        // Assert - Results should be consistent
        isElevated1.Should().Be(isElevated2);
        currentUser1.Should().Be(currentUser2);
    }

    [Theory]
    [InlineData(@"C:\Windows\System32\drivers")]
    [InlineData(@"C:\Program Files")]
    public void ValidateFileAccess_WithRestrictedPaths_ShouldHandleGracefully(string restrictedPath)
    {
        // Arrange
        var context = SecurityContext.Current;
        
        // Act
        var result = context.ValidateFileAccess(restrictedPath);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationResult>();
        // Don't assert specific validity as it depends on actual user permissions
    }
}
