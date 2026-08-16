using System.Text.Json;
using System.Text.RegularExpressions;

namespace CLIF.Tests.McpUI;

/// <summary>
/// Verifies the compiled MCP host can drive the real WPF fixture entirely through
/// JSON-RPC over stdio. This deliberately does not reference CLIF.Mcp assemblies.
/// </summary>
[Collection("McpUI")]
[Trait("Category", "McpUI")]
public sealed class McpWpfBlackBoxTests
{
    private static readonly Regex WindowHandlePattern = new(
        @"Window handle:\s*(?<handle>w\d+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex TestTextBoxReferencePattern = new(
        @"^.*TestTextBox.*\[ref=(?<reference>[^\]]+)\].*$",
        RegexOptions.CultureInvariant | RegexOptions.Multiline);

    private readonly McpProcessFixture _fixture;

    public McpWpfBlackBoxTests(McpProcessFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task McpProcess_CanLaunchSnapshotFillAndReadWpfTextBox()
    {
        await _fixture.InitializeAsync();

        using (var toolsListResponse = await _fixture.SendRequestAsync("tools/list"))
        {
            McpProcessFixture.AssertNoProtocolError(toolsListResponse.RootElement);
            var toolNames = toolsListResponse.RootElement
                .GetProperty("result")
                .GetProperty("tools")
                .EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString())
                .ToHashSet(StringComparer.Ordinal);

            Assert.Contains("clif_launch", toolNames);
            Assert.Contains("clif_snapshot", toolNames);
            Assert.Contains("clif_fill", toolNames);
            Assert.Contains("clif_get_text", toolNames);
        }

        string windowHandle;
        using (var launchResponse = await _fixture.CallToolAsync("clif_launch", new { app = _fixture.WpfExecutablePath }))
        {
            var launchText = McpProcessFixture.GetSuccessfulToolText(launchResponse.RootElement);
            var handleMatch = WindowHandlePattern.Match(launchText);
            Assert.True(handleMatch.Success, $"MCP launch result did not contain a window handle:{Environment.NewLine}{launchText}");
            windowHandle = handleMatch.Groups["handle"].Value;
        }

        await _fixture.TrackLaunchedWpfApplicationAsync(windowHandle);

        string textBoxReference;
        using (var snapshotResponse = await _fixture.CallToolAsync("clif_snapshot", new { handle = windowHandle }))
        {
            var snapshot = McpProcessFixture.GetSuccessfulToolText(snapshotResponse.RootElement);
            var textBoxMatch = TestTextBoxReferencePattern.Match(snapshot);
            Assert.True(textBoxMatch.Success, $"TestTextBox was not present in the MCP accessibility snapshot:{Environment.NewLine}{snapshot}");
            textBoxReference = textBoxMatch.Groups["reference"].Value;
        }

        const string expectedValue = "MCP black-box WPF value";
        using (var fillResponse = await _fixture.CallToolAsync("clif_fill", new { @ref = textBoxReference, value = expectedValue }))
        {
            _ = McpProcessFixture.GetSuccessfulToolText(fillResponse.RootElement);
        }

        using var getTextResponse = await _fixture.CallToolAsync("clif_get_text", new { @ref = textBoxReference });
        var actualValue = McpProcessFixture.GetSuccessfulToolText(getTextResponse.RootElement);
        Assert.Equal(expectedValue, actualValue);
    }
}
