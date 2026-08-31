using System.Text.Json;
using System.Text.RegularExpressions;

namespace CLIF.Tests.McpUI;

/// <summary>
/// Exercises every MCP tool through the compiled server against the real WPF fixture.
/// The test asserts observable UI state and protocol payloads rather than relying only
/// on successful tool calls.
/// </summary>
[Collection("McpUI")]
[Trait("Category", "McpUI")]
public sealed class McpAllToolsBlackBoxTests
{
    private static readonly Regex WindowHandlePattern = new(
        @"Window handle:\s*(?<handle>w\d+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex ReferencePattern = new(
        @"\[ref=(?<reference>[^\]]+)\]",
        RegexOptions.CultureInvariant);

    private readonly McpProcessFixture _fixture;

    public McpAllToolsBlackBoxTests(McpProcessFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task McpProcess_AllToolsPerformExpectedActivitiesAgainstWpfFixture()
    {
        await _fixture.InitializeAsync();
        await AssertAllToolsAreAdvertisedAsync();

        var windowHandle = await LaunchFixtureAsync();
        await _fixture.TrackLaunchedWpfApplicationAsync(windowHandle);

        await AssertWindowCanBeEnumeratedAndFocusedAsync(windowHandle);
        await AssertSnapshotAndSearchWorkAsync(windowHandle);

        var textBoxReference = await FindReferenceAsync(windowHandle, "TestTextBox");
        var checkBoxReference = await FindReferenceAsync(windowHandle, "TestCheckBox");
        var comboBoxReference = await FindReferenceAsync(windowHandle, "TestComboBox");
        var statusReference = await FindReferenceAsync(windowHandle, "StatusTextBlock");

        await AssertFillTypeAndGetTextAsync(textBoxReference);
        await AssertClickChangesFixtureStateAsync(checkBoxReference, statusReference);
        await AssertScreenshotReturnsPngAsync(textBoxReference);
        await AssertBatchExecutesAgainstFixtureAsync(windowHandle, textBoxReference);

        // Batch snapshot regenerates the element registry, so acquire fresh refs.
        textBoxReference = await FindReferenceAsync(windowHandle, "TestTextBox");
        comboBoxReference = await FindReferenceAsync(windowHandle, "TestComboBox");
        statusReference = await FindReferenceAsync(windowHandle, "StatusTextBlock");

        await AssertAdvancedInteractionChangesFixtureStateAsync(comboBoxReference, statusReference);
        await AssertInlineScriptValidationAsync();
        await AssertCloseWorksAsync(windowHandle);
    }

    private async Task AssertAllToolsAreAdvertisedAsync()
    {
        using var response = await _fixture.SendRequestAsync("tools/list");
        McpProcessFixture.AssertNoProtocolError(response.RootElement);
        var toolNames = response.RootElement.GetProperty("result").GetProperty("tools")
            .EnumerateArray().Select(tool => tool.GetProperty("name").GetString()).ToHashSet(StringComparer.Ordinal);

        foreach (var tool in new[]
        {
            "clif_launch", "clif_snapshot", "clif_click", "clif_type", "clif_fill",
            "clif_get_text", "clif_screenshot", "clif_list_windows", "clif_focus",
            "clif_close", "clif_batch", "clif_interact", "clif_search_elements",
            "clif_validate_script",
        })
        {
            Assert.Contains(tool, toolNames);
        }
    }

    private async Task<string> LaunchFixtureAsync()
    {
        using var response = await _fixture.CallToolAsync("clif_launch", new { app = _fixture.WpfExecutablePath });
        var text = McpProcessFixture.GetSuccessfulToolText(response.RootElement);
        var match = WindowHandlePattern.Match(text);
        Assert.True(match.Success, $"Launch did not return a window handle:{Environment.NewLine}{text}");
        return match.Groups["handle"].Value;
    }

    private async Task AssertWindowCanBeEnumeratedAndFocusedAsync(string windowHandle)
    {
        using (var listResponse = await _fixture.CallToolAsync("clif_list_windows"))
        {
            var windows = McpProcessFixture.GetSuccessfulToolText(listResponse.RootElement);
            Assert.Contains(windowHandle, windows);
            Assert.Contains("Comprehensive WPF Controls Test App", windows);
        }

        using var focusResponse = await _fixture.CallToolAsync("clif_focus", new { handle = windowHandle });
        Assert.Contains("Focused window", McpProcessFixture.GetSuccessfulToolText(focusResponse.RootElement));
    }

    private async Task AssertSnapshotAndSearchWorkAsync(string windowHandle)
    {
        using (var snapshotResponse = await _fixture.CallToolAsync("clif_snapshot", new { handle = windowHandle }))
        {
            var snapshot = McpProcessFixture.GetSuccessfulToolText(snapshotResponse.RootElement);
            Assert.Contains("TestTextBox", snapshot);
            Assert.Contains("TestComboBox", snapshot);
        }

        using var searchResponse = await _fixture.CallToolAsync(
            "clif_search_elements", new { handle = windowHandle, automationId = "TestTextBox" });
        var search = McpProcessFixture.GetSuccessfulToolText(searchResponse.RootElement);
        Assert.Contains("TestTextBox", search);
        Assert.Matches(ReferencePattern, search);
    }

    private async Task<string> FindReferenceAsync(string windowHandle, string automationId)
    {
        using var response = await _fixture.CallToolAsync(
            "clif_search_elements", new { handle = windowHandle, automationId });
        var result = McpProcessFixture.GetSuccessfulToolText(response.RootElement);
        var match = ReferencePattern.Match(result);
        Assert.True(match.Success, $"No reference returned for '{automationId}':{Environment.NewLine}{result}");
        return match.Groups["reference"].Value;
    }

    private async Task AssertFillTypeAndGetTextAsync(string textBoxReference)
    {
        using (var fillResponse = await _fixture.CallToolAsync(
            "clif_fill", new { @ref = textBoxReference, value = "MCP fill" }))
        {
            Assert.Contains("Filled", McpProcessFixture.GetSuccessfulToolText(fillResponse.RootElement));
        }

        using (var typeResponse = await _fixture.CallToolAsync(
            "clif_type", new { @ref = textBoxReference, text = " + type" }))
        {
            Assert.Contains("Typed", McpProcessFixture.GetSuccessfulToolText(typeResponse.RootElement));
        }

        using var readResponse = await _fixture.CallToolAsync("clif_get_text", new { @ref = textBoxReference });
        Assert.Equal("MCP fill + type", McpProcessFixture.GetSuccessfulToolText(readResponse.RootElement));
    }

    private async Task AssertClickChangesFixtureStateAsync(string checkBoxReference, string statusReference)
    {
        using (var clickResponse = await _fixture.CallToolAsync("clif_click", new { @ref = checkBoxReference }))
        {
            Assert.Contains("Toggled", McpProcessFixture.GetSuccessfulToolText(clickResponse.RootElement));
        }

        // CheckBox has no event handler, so verify its UIA state through clif_interact.
        using var stateResponse = await _fixture.CallToolAsync(
            "clif_interact", new { @ref = checkBoxReference, controlType = "checkbox", action = "get_value" });
        Assert.Contains("On", McpProcessFixture.GetSuccessfulToolText(stateResponse.RootElement));
    }

    private async Task AssertScreenshotReturnsPngAsync(string textBoxReference)
    {
        using var response = await _fixture.CallToolAsync("clif_screenshot", new { @ref = textBoxReference });
        McpProcessFixture.AssertNoProtocolError(response.RootElement);
        var result = response.RootElement.GetProperty("result");
        Assert.False(result.TryGetProperty("isError", out var isError) && isError.GetBoolean());
        var image = result.GetProperty("content").EnumerateArray()
            .Single(item => item.GetProperty("type").GetString() == "image");
        var png = Convert.FromBase64String(image.GetProperty("data").GetString()!);
        Assert.True(png.Length > 8);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
    }

    private async Task AssertBatchExecutesAgainstFixtureAsync(string windowHandle, string textBoxReference)
    {
        using var response = await _fixture.CallToolAsync("clif_batch", new
        {
            actions = new object[]
            {
                new { action = "fill", @ref = textBoxReference, value = "MCP batch" },
                new { action = "wait", ms = 25 },
                new { action = "snapshot", handle = windowHandle },
            },
        });
        var result = McpProcessFixture.GetSuccessfulToolText(response.RootElement);
        Assert.Contains("Filled", result);
        Assert.Contains("Waited 25ms", result);
        Assert.Contains("TestTextBox", result);
    }

    private async Task AssertAdvancedInteractionChangesFixtureStateAsync(string comboBoxReference, string statusReference)
    {
        using (var interactResponse = await _fixture.CallToolAsync(
            "clif_interact", new { @ref = comboBoxReference, controlType = "combobox", action = "select", value = "Item 3" }))
        {
            Assert.Contains("Selected \"Item 3\"", McpProcessFixture.GetSuccessfulToolText(interactResponse.RootElement));
        }

        using var statusResponse = await _fixture.CallToolAsync("clif_get_text", new { @ref = statusReference });
        Assert.Contains("ComboBox selected: Item 3", McpProcessFixture.GetSuccessfulToolText(statusResponse.RootElement));
    }

    private async Task AssertInlineScriptValidationAsync()
    {
        const string content = """{ "name": "MCP E2E", "description": "validation", "version": "1.0", "steps": [] }""";
        using var response = await _fixture.CallToolAsync("clif_validate_script", new { content });
        Assert.Contains("Script \"MCP E2E\" is valid", McpProcessFixture.GetSuccessfulToolText(response.RootElement));
    }

    private async Task AssertCloseWorksAsync(string windowHandle)
    {
        using var response = await _fixture.CallToolAsync("clif_close", new { handle = windowHandle });
        Assert.Contains("Closed window", McpProcessFixture.GetSuccessfulToolText(response.RootElement));
    }
}
