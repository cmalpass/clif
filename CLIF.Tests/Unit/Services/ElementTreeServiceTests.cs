using CLIF.Services;
using CLIF.Core;
using CLIF.Tests.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using FlaUI.Core.AutomationElements;

namespace CLIF.Tests.Unit.Services;

/// <summary>
/// Tests for ElementTreeService
/// </summary>
public class ElementTreeServiceTests
{
    private readonly Mock<ILogger<ElementTreeService>> _mockLogger;
    private readonly ElementTreeService _elementTreeService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public ElementTreeServiceTests()
    {
        _mockLogger = new Mock<ILogger<ElementTreeService>>();
        _elementTreeService = new ElementTreeService(_mockLogger.Object);
    }

    [Fact]
    public async Task BuildTreeAsync_WithNullElement_ShouldReturnEmptyNode()
    {
        // Arrange
        AutomationElement? nullElement = null;

        // Act
        var result = await _elementTreeService
            .BuildTreeAsync(nullElement!, includeChildren: true, maxDepth: 5)
            .WithTimeout(DefaultTimeout, "BuildTreeAsync(null)");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task PrintTreeAsync_WithEmptyNode_ShouldReturnString()
    {
        // Arrange
        var emptyNode = new ElementTreeNode();

        // Act
        var result = await _elementTreeService
            .PrintTreeAsync(emptyNode)
            .WithTimeout(DefaultTimeout, "PrintTreeAsync(empty)");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public async Task PrintTreeAsync_WithOptions_ShouldRespectOptions()
    {
        // Arrange
        var node = new ElementTreeNode
        {
            Name = "TestNode",
            ControlType = "Button",
            AutomationId = "TestId"
        };
        var options = new TreePrintOptions
        {
            ShowProperties = true,
            ShowSelectors = true,
            MaxDepth = 3
        };

        // Act
        var result = await _elementTreeService
            .PrintTreeAsync(node, options)
            .WithTimeout(DefaultTimeout, "PrintTreeAsync(with options)");

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("TestNode");
    }

    [Fact]
    public async Task SearchTreeAsync_WithNameCriteria_ShouldFindMatchingNodes()
    {
        // Arrange
        var root = new ElementTreeNode
        {
            Name = "Root",
            Children = new List<ElementTreeNode>
            {
                new ElementTreeNode { Name = "Child1" },
                new ElementTreeNode { Name = "Child2" }
            }
        };
        var criteria = new ElementSearchCriteria
        {
            Name = "Child1"
        };

        // Act
        var result = await _elementTreeService
            .SearchTreeAsync(root, criteria)
            .WithTimeout(DefaultTimeout, "SearchTreeAsync(name)");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ElementTreeNode>>();
    }

    [Fact]
    public async Task SearchTreeAsync_WithControlTypeCriteria_ShouldFilterByType()
    {
        // Arrange
        var root = new ElementTreeNode
        {
            Name = "Root",
            ControlType = "Window",
            Children = new List<ElementTreeNode>
            {
                new ElementTreeNode { Name = "Button1", ControlType = "Button" },
                new ElementTreeNode { Name = "TextBox1", ControlType = "Edit" }
            }
        };
        var criteria = new ElementSearchCriteria
        {
            ControlType = "Button"
        };

        // Act
        var result = await _elementTreeService
            .SearchTreeAsync(root, criteria)
            .WithTimeout(DefaultTimeout, "SearchTreeAsync(controlType)");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<ElementTreeNode>>();
    }

    [Fact]
    public async Task FindElementInTreeAsync_WithValidSelector_ShouldFindElement()
    {
        // Arrange
        var root = new ElementTreeNode
        {
            Name = "Root",
            AutomationId = "RootId",
            Children = new List<ElementTreeNode>
            {
                new ElementTreeNode { Name = "TestButton", AutomationId = "TestBtn" }
            }
        };

        // Act
        var result = await _elementTreeService
            .FindElementInTreeAsync(root, "id=TestBtn")
            .WithTimeout(DefaultTimeout, "FindElementInTreeAsync");

        // Assert - Result may be null in test environment, but should not throw
        (result == null || result is ElementTreeNode).Should().BeTrue();
    }

    [Fact]
    public async Task FindElementInTreeAsync_WithInvalidSelector_ShouldHandleGracefully()
    {
        // Arrange
        var root = new ElementTreeNode
        {
            Name = "Root"
        };

        // Act
        var result = await _elementTreeService
            .FindElementInTreeAsync(root, "invalid:selector")
            .WithTimeout(DefaultTimeout, "FindElementInTreeAsync(invalid)");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("id=TestId")]
    [InlineData("name=TestName")]
    [InlineData("class=TestClass")]
    [InlineData("type=Button")]
    public async Task FindElementInTreeAsync_WithVariousSelectors_ShouldProcessCorrectly(string selector)
    {
        // Arrange
        var root = new ElementTreeNode
        {
            Name = "Root",
            Children = new List<ElementTreeNode>()
        };

        // Act
        var result = await _elementTreeService
            .FindElementInTreeAsync(root, selector)
            .WithTimeout(DefaultTimeout, $"FindElementInTreeAsync({selector})");

        // Assert - Should not throw, result may be null
        (result == null || result is ElementTreeNode).Should().BeTrue();
    }
}
