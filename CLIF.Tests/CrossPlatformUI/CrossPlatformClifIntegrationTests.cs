using CLIF.Tests.Utilities;
using CLIF.Tests.Integration;
using FluentAssertions;

namespace CLIF.Tests.CrossPlatformUI;

[Collection("CrossPlatformUI")]
public sealed class CrossPlatformClifIntegrationTests : IntegrationTestBase
{
    private readonly CrossPlatformAppFixture _fixture;

    public CrossPlatformClifIntegrationTests(CrossPlatformAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public async Task AutomationService_ShouldResolveAutomationIdAndClick()
    {
        _fixture.SkipIfUnavailable();

        var processId = _fixture.App!.ProcessId;
        (await AutomationService.AttachToProcessAsync(processId)).Should().BeTrue();

        var button = await AutomationService.FindElementAsync("id=TestButton");
        button.Should().NotBeNull();

        (await AutomationService.ClickAsync(button!)).Should().BeTrue();

        var status = await AutomationService.FindElementAsync("id=StatusTextBlock");
        status.Should().NotBeNull();
        (await AutomationService.GetTextAsync(status!)).Should().Be("Button clicked");
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public async Task AutomationService_ShouldSetAndReadTextValue()
    {
        _fixture.SkipIfUnavailable();

        (await AutomationService.AttachToProcessAsync(_fixture.App!.ProcessId)).Should().BeTrue();

        var textBox = await AutomationService.FindElementAsync("id=TestTextBox");
        textBox.Should().NotBeNull();

        (await AutomationService.SetValueAsync(textBox!, "CLIF value")).Should().BeTrue();
        (await AutomationService.GetValueAsync(textBox!)).Should().Be("CLIF value");
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public async Task AutomationService_ShouldSelectFixtureDateAndTableRow()
    {
        _fixture.SkipIfUnavailable();

        (await AutomationService.AttachToProcessAsync(_fixture.App!.ProcessId)).Should().BeTrue();

        var setDateButton = await AutomationService.FindElementAsync("id=SetKnownDateButton");
        setDateButton.Should().NotBeNull();
        (await AutomationService.ClickAsync(setDateButton!)).Should().BeTrue();

        var selectedDate = await AutomationService.FindElementAsync("id=SelectedDateText");
        selectedDate.Should().NotBeNull();
        (await AutomationService.GetTextAsync(selectedDate!)).Should().Be("2026-02-20");

        var table = await AutomationService.FindElementAsync("id=TestDataTable");
        table.Should().NotBeNull();
        (await AutomationService.SelectListBoxItemByIndexAsync(table!, 1)).Should().BeTrue();

        var selectedRow = await AutomationService.FindElementAsync("id=SelectedDataRowText");
        selectedRow.Should().NotBeNull();
        (await AutomationService.GetTextAsync(selectedRow!)).Should().Be("Selected row: Bob Smith");
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public async Task ScriptService_ShouldExecuteAgainstFixture()
    {
        _fixture.SkipIfUnavailable();

        var script = """
        {
          "name": "Avalonia smoke test",
          "target": { "processName": "TestCrossPlatformApp" },
          "steps": [
            { "action": "click", "element": "id=TestButton" },
            { "action": "validate", "element": "id=StatusTextBlock", "value": "Button clicked" }
          ]
        }
        """;

        var result = await ScriptService.ExecuteScriptContentAsync(script, _fixture.App!.ProcessId);

        result.Success.Should().BeTrue(result.Message);
        result.StepsExecuted.Should().Be(2);
        result.StepsFailed.Should().Be(0);
    }
}
