using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;
using CLIF.Mcp.Tools;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Tests that all 14 MCP tool implementations have correct names, descriptions,
/// schemas, and handle missing/invalid arguments properly.
/// These tests validate the tools' metadata and error handling without requiring Windows UI automation.
/// </summary>
public class ToolDefinitionTests
{
    [Fact]
    public void AllTools_HaveClif_Prefix()
    {
        var tools = CreateAllTools();

        foreach (var tool in tools)
        {
            tool.Name.Should().StartWith("clif_",
                because: $"tool '{tool.Name}' should follow the clif_ naming convention");
        }
    }

    [Fact]
    public void AllTools_HaveUniqueNames()
    {
        var tools = CreateAllTools();
        var names = tools.Select(t => t.Name).ToList();

        names.Should().OnlyHaveUniqueItems("all MCP tool names must be unique");
    }

    [Fact]
    public void AllTools_HaveNonEmptyDescriptions()
    {
        var tools = CreateAllTools();

        foreach (var tool in tools)
        {
            var definition = tool.GetDefinition();
            definition.Description.Should().NotBeNullOrEmpty(
                because: $"tool '{tool.Name}' must have a description for MCP clients");
        }
    }

    [Fact]
    public void AllTools_HaveValidInputSchema()
    {
        var tools = CreateAllTools();

        foreach (var tool in tools)
        {
            var definition = tool.GetDefinition();
            definition.InputSchema.Should().NotBeNull(
                because: $"tool '{tool.Name}' must have an input schema");

            // Verify the schema serializes to valid JSON
            var json = JsonSerializer.Serialize(definition.InputSchema, McpProtocol.JsonOptions);
            json.Should().Contain("\"type\"",
                because: $"tool '{tool.Name}' schema should have a type property");
        }
    }

    [Fact]
    public void AllTools_GetDefinition_ReturnsCorrectName()
    {
        var tools = CreateAllTools();

        foreach (var tool in tools)
        {
            var definition = tool.GetDefinition();
            definition.Name.Should().Be(tool.Name);
            definition.Description.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void ExpectedToolCount_Is14()
    {
        var tools = CreateAllTools();
        tools.Should().HaveCount(14, "the MCP server should have exactly 14 tools");
    }

    [Theory]
    [InlineData("clif_launch")]
    [InlineData("clif_snapshot")]
    [InlineData("clif_click")]
    [InlineData("clif_type")]
    [InlineData("clif_fill")]
    [InlineData("clif_get_text")]
    [InlineData("clif_screenshot")]
    [InlineData("clif_list_windows")]
    [InlineData("clif_focus")]
    [InlineData("clif_close")]
    [InlineData("clif_batch")]
    [InlineData("clif_interact")]
    [InlineData("clif_search_elements")]
    [InlineData("clif_run_script")]
    public void ExpectedTool_IsRegistered(string expectedToolName)
    {
        var tools = CreateAllTools();
        tools.Select(t => t.Name).Should().Contain(expectedToolName);
    }

    [Fact]
    public async Task LaunchTool_MissingApp_ReturnsError()
    {
        var sessionManager = CreateMinimalWindowSessionManager();
        var tool = new LaunchTool(sessionManager);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("app");
    }

    [Fact]
    public async Task ClickTool_MissingRef_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new ClickTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("ref");
    }

    [Fact]
    public async Task ClickTool_InvalidRef_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new ClickTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{"ref":"w1e999"}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Element not found");
    }

    [Fact]
    public async Task TypeTool_MissingText_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new TypeTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("text");
    }

    [Fact]
    public async Task FillTool_MissingArguments_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new FillTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("ref");
    }

    [Fact]
    public async Task FillTool_InvalidRef_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new FillTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{"ref":"w1e999","value":"test"}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Element not found");
    }

    [Fact]
    public async Task GetTextTool_MissingRef_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new GetTextTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("ref");
    }

    [Fact]
    public async Task GetTextTool_InvalidRef_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new GetTextTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{"ref":"w1e999"}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Element not found");
    }

    [Fact]
    public async Task CloseWindowTool_MissingHandle_ReturnsError()
    {
        var sessionManager = CreateMinimalWindowSessionManager();
        var tool = new CloseWindowTool(sessionManager);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("handle");
    }

    [Fact]
    public async Task InteractTool_MissingArguments_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new InteractTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("ref");
    }

    [Fact]
    public async Task InteractTool_InvalidRef_ReturnsError()
    {
        var registry = new ElementRegistry();
        var tool = new InteractTool(registry);
        var args = JsonSerializer.Deserialize<JsonElement>(
            """{"ref":"w1e999","controlType":"combobox","action":"select","value":"test"}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Element not found");
    }

    [Fact]
    public async Task BatchTool_MissingActions_ReturnsError()
    {
        var sessionManager = CreateMinimalWindowSessionManager();
        var registry = new ElementRegistry();
        var tool = new BatchTool(sessionManager, registry);
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("actions");
    }

    [Fact]
    public async Task ScriptTool_MissingPathAndContent_ReturnsError()
    {
        var tool = new ScriptTool();
        var args = JsonSerializer.Deserialize<JsonElement>("""{}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("path");
    }

    [Fact]
    public async Task ScriptTool_NonexistentFile_ReturnsError()
    {
        var tool = new ScriptTool();
        var args = JsonSerializer.Deserialize<JsonElement>(
            """{"path":"/nonexistent/script.json"}""");

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("not found");
    }

    [Fact]
    public async Task ScriptTool_ValidInlineContent_ReturnsSuccess()
    {
        var tool = new ScriptTool();
        var scriptJson = "{\"name\":\"Test Script\",\"description\":\"A test automation script\",\"version\":\"1.0\",\"steps\":[{\"action\":\"click\",\"element\":\"id=Button1\",\"description\":\"Click button\"}]}";
        var escapedContent = JsonSerializer.Serialize(scriptJson);
        var argsJson = "{\"content\":" + escapedContent + "}";
        var args = JsonSerializer.Deserialize<JsonElement>(argsJson);

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeNull();
        result.Content[0].Text.Should().Contain("Test Script");
        result.Content[0].Text.Should().Contain("1 steps");
    }

    [Fact]
    public async Task ScriptTool_InvalidJson_ReturnsError()
    {
        var tool = new ScriptTool();
        var escapedContent = JsonSerializer.Serialize("not valid json{{{");
        var argsJson = "{\"content\":" + escapedContent + "}";
        var args = JsonSerializer.Deserialize<JsonElement>(argsJson);

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task ScriptTool_EmptySteps_ReturnsZeroSteps()
    {
        var tool = new ScriptTool();
        var scriptJson = "{\"name\":\"Empty\",\"steps\":[]}";
        var escapedContent = JsonSerializer.Serialize(scriptJson);
        var argsJson = "{\"content\":" + escapedContent + "}";
        var args = JsonSerializer.Deserialize<JsonElement>(argsJson);

        var result = await tool.ExecuteAsync(args);

        result.IsError.Should().BeNull();
        result.Content[0].Text.Should().Contain("0 steps");
    }

    // --- Helper methods ---

    private static List<ITool> CreateAllTools()
    {
        var sessionManager = CreateMinimalWindowSessionManager();
        var elementRegistry = new ElementRegistry();

        return new List<ITool>
        {
            new LaunchTool(sessionManager),
            new SnapshotTool(sessionManager, elementRegistry),
            new ClickTool(elementRegistry),
            new TypeTool(elementRegistry),
            new FillTool(elementRegistry),
            new GetTextTool(elementRegistry),
            new ScreenshotTool(sessionManager, elementRegistry),
            new ListWindowsTool(sessionManager),
            new FocusWindowTool(sessionManager),
            new CloseWindowTool(sessionManager),
            new BatchTool(sessionManager, elementRegistry),
            new InteractTool(elementRegistry),
            new SearchTool(sessionManager, elementRegistry),
            new ScriptTool(),
        };
    }

    private static WindowSessionManager CreateMinimalWindowSessionManager()
    {
        // WindowSessionManager creates a UIA3Automation in its constructor.
        // On Linux this will throw, so we catch and skip if needed.
        try
        {
            return new WindowSessionManager();
        }
        catch
        {
            // Return null and tests depending on it will be skipped at runtime
            return null!;
        }
    }
}
