using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Tests for the ToolBase helper methods (argument extraction).
/// </summary>
public class ToolBaseTests
{
    [Fact]
    public void GetStringArgument_ReturnValue_WhenPresent()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"name":"hello"}""");

        var result = tool.TestGetStringArgument(args, "name");

        result.Should().Be("hello");
    }

    [Fact]
    public void GetStringArgument_ReturnsNull_WhenMissing()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"other":"value"}""");

        var result = tool.TestGetStringArgument(args, "name");

        result.Should().BeNull();
    }

    [Fact]
    public void GetStringArgument_ReturnsNull_WhenNullArguments()
    {
        var tool = new ConcreteToolBase();

        var result = tool.TestGetStringArgument(null, "name");

        result.Should().BeNull();
    }

    [Fact]
    public void GetBoolArgument_ReturnsValue_WhenPresent()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"flag":true}""");

        var result = tool.TestGetBoolArgument(args, "flag");

        result.Should().BeTrue();
    }

    [Fact]
    public void GetBoolArgument_ReturnsFalse_WhenPresent()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"flag":false}""");

        var result = tool.TestGetBoolArgument(args, "flag");

        result.Should().BeFalse();
    }

    [Fact]
    public void GetBoolArgument_ReturnsDefault_WhenMissing()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = tool.TestGetBoolArgument(args, "flag", true);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetBoolArgument_ReturnsFalseDefault_WhenNullArguments()
    {
        var tool = new ConcreteToolBase();

        var result = tool.TestGetBoolArgument(null, "flag");

        result.Should().BeFalse();
    }

    [Fact]
    public void GetIntArgument_ReturnsValue_WhenPresent()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"count":42}""");

        var result = tool.TestGetIntArgument(args, "count");

        result.Should().Be(42);
    }

    [Fact]
    public void GetIntArgument_ReturnsDefault_WhenMissing()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = tool.TestGetIntArgument(args, "count", -1);

        result.Should().Be(-1);
    }

    [Fact]
    public void GetIntArgument_ReturnsDefault_WhenNullArguments()
    {
        var tool = new ConcreteToolBase();

        var result = tool.TestGetIntArgument(null, "count", 99);

        result.Should().Be(99);
    }

    [Fact]
    public void GetDoubleArgument_ReturnsValue_WhenPresent()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"value":3.14}""");

        var result = tool.TestGetDoubleArgument(args, "value");

        result.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void GetDoubleArgument_ReturnsDefault_WhenMissing()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = tool.TestGetDoubleArgument(args, "value", 1.0);

        result.Should().Be(1.0);
    }

    [Fact]
    public void GetArgument_DeserializesArray()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{"items":["a","b","c"]}""");

        var result = tool.TestGetArgument<string[]>(args, "items");

        result.Should().NotBeNull();
        result.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void GetArgument_ReturnsNull_WhenMissing()
    {
        var tool = new ConcreteToolBase();
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = tool.TestGetArgument<string[]>(args, "items");

        result.Should().BeNull();
    }

    [Fact]
    public void TextResult_CreatesValidTextContent()
    {
        var result = ConcreteToolBase.TestTextResult("hello");

        result.Content.Should().HaveCount(1);
        result.Content[0].Type.Should().Be("text");
        result.Content[0].Text.Should().Be("hello");
        result.IsError.Should().BeNull();
    }

    [Fact]
    public void ErrorResult_CreatesValidErrorContent()
    {
        var result = ConcreteToolBase.TestErrorResult("something failed");

        result.Content.Should().HaveCount(1);
        result.Content[0].Type.Should().Be("text");
        result.Content[0].Text.Should().Be("something failed");
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void ImageResult_CreatesValidImageContent()
    {
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        var result = ConcreteToolBase.TestImageResult(imageBytes);

        result.Content.Should().HaveCount(1);
        result.Content[0].Type.Should().Be("image");
        result.Content[0].MimeType.Should().Be("image/png");
        result.Content[0].Data.Should().Be(Convert.ToBase64String(imageBytes));
        result.IsError.Should().BeNull();
    }

    [Fact]
    public void GetDefinition_ReturnsCorrectDefinition()
    {
        var tool = new ConcreteToolBase();

        var definition = tool.GetDefinition();

        definition.Name.Should().Be("clif_concrete_test");
        definition.Description.Should().Be("A concrete test tool");
        definition.InputSchema.Should().NotBeNull();
    }

    /// <summary>
    /// Concrete implementation of ToolBase that exposes protected methods for testing.
    /// </summary>
    private sealed class ConcreteToolBase : ToolBase
    {
        public override string Name => "clif_concrete_test";
        public override string Description => "A concrete test tool";
        public override object InputSchema => new { type = "object" };

        public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
        {
            return Task.FromResult(TextResult("executed"));
        }

        // Expose protected methods for testing
        public string? TestGetStringArgument(JsonElement? args, string name) =>
            GetStringArgument(args, name);

        public bool TestGetBoolArgument(JsonElement? args, string name, bool defaultValue = false) =>
            GetBoolArgument(args, name, defaultValue);

        public int TestGetIntArgument(JsonElement? args, string name, int defaultValue = 0) =>
            GetIntArgument(args, name, defaultValue);

        public double TestGetDoubleArgument(JsonElement? args, string name, double defaultValue = 0) =>
            GetDoubleArgument(args, name, defaultValue);

        public T? TestGetArgument<T>(JsonElement? args, string name) =>
            GetArgument<T>(args, name);

        public static new McpToolResult TestTextResult(string text) => TextResult(text);
        public static new McpToolResult TestErrorResult(string message) => ErrorResult(message);
        public static new McpToolResult TestImageResult(byte[] data) => ImageResult(data);
    }
}
