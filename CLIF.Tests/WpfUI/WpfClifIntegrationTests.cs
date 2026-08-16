using CLIF.Tests.Integration;
using FluentAssertions;
using System.IO;

namespace CLIF.Tests.WpfUI;

/// <summary>
/// Exercises CLIF's service layer against the native WPF fixture. These tests
/// complement the provider-level WPF tests by validating CLIF's selectors and
/// interaction APIs on the framework that remains its primary target.
/// </summary>
[Collection("WpfUI")]
public sealed class WpfClifIntegrationTests : IntegrationTestBase
{
    private readonly WpfTestAppFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfClifIntegrationTests"/> class.
    /// </summary>
    public WpfClifIntegrationTests(WpfTestAppFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that CLIF can resolve and set a native WPF DatePicker through UIA.
    /// </summary>
    [Fact]
    [Trait("Category", "WpfUI")]
    public async Task AutomationService_ShouldSetAndReadNativeWpfDatePickerValue()
    {
        _fixture.SkipIfUnavailable();

        (await AutomationService.AttachToProcessAsync(_fixture.App!.ProcessId)).Should().BeTrue();

        var datePicker = await AutomationService.FindElementAsync("id=TestDatePicker");
        datePicker.Should().NotBeNull();

        var expected = new DateTime(2026, 2, 20);
        (await AutomationService.SetDatePickerAsync(datePicker!, expected)).Should().BeTrue();
        (await AutomationService.GetDatePickerValueAsync(datePicker!)).Should().Be(expected);
    }

    /// <summary>
    /// Verifies that CLIF's standard Value and Toggle pattern operations work with WPF.
    /// </summary>
    [Fact]
    [Trait("Category", "WpfUI")]
    public async Task AutomationService_ShouldSetWpfTextAndCheckBoxValue()
    {
        _fixture.SkipIfUnavailable();

        (await AutomationService.AttachToProcessAsync(_fixture.App!.ProcessId)).Should().BeTrue();

        var textBox = await AutomationService.FindElementAsync("id=TestTextBox");
        textBox.Should().NotBeNull();
        (await AutomationService.SetValueAsync(textBox!, "CLIF WPF value")).Should().BeTrue();
        (await AutomationService.GetValueAsync(textBox!)).Should().Be("CLIF WPF value");

        var checkBox = await AutomationService.FindElementAsync("id=TestCheckBox");
        checkBox.Should().NotBeNull();
        (await AutomationService.SetCheckBoxAsync(checkBox!, true)).Should().BeTrue();
        (await AutomationService.GetCheckBoxStateAsync(checkBox!)).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CLIF can select a native WPF ListBox item through its
    /// standard UI Automation selection pattern.
    /// </summary>
    [Fact]
    [Trait("Category", "WpfUI")]
    public async Task AutomationService_ShouldSelectNativeWpfListBoxItem()
    {
        _fixture.SkipIfUnavailable();

        (await AutomationService.AttachToProcessAsync(_fixture.App!.ProcessId)).Should().BeTrue();

        var listBox = await AutomationService.FindElementAsync("id=TestListBox");
        listBox.Should().NotBeNull();

        (await AutomationService.SelectListBoxItemByIndexAsync(listBox!, 1)).Should().BeTrue();

        var status = await AutomationService.FindElementAsync("id=StatusTextBlock");
        status.Should().NotBeNull();
        (await AutomationService.GetTextAsync(status!)).Should().Contain("List Item 2");
    }

    /// <summary>
    /// Verifies that a script validates an editable WPF control through its UIA value,
    /// rather than the control's accessible name.
    /// </summary>
    [Fact]
    [Trait("Category", "WpfUI")]
    public async Task ScriptService_ShouldValidateWpfTextBoxValue()
    {
        _fixture.SkipIfUnavailable();

        const string expectedValue = "CLIF script validation";
        var script = $$"""
            {
              "name": "Validate WPF value",
              "target": { "processName": "TestWpfApp" },
              "steps": [
                { "action": "clear", "element": "id=TestTextBox" },
                { "action": "type", "element": "id=TestTextBox", "value": "{{expectedValue}}" },
                { "action": "validate", "element": "id=TestTextBox", "value": "{{expectedValue}}" }
              ]
            }
            """;

        var result = await ScriptService.ExecuteScriptContentAsync(script, _fixture.App!.ProcessId);

        result.Success.Should().BeTrue(result.Message);
        result.StepsExecuted.Should().Be(3);
        result.StepsFailed.Should().Be(0);
    }

    /// <summary>
    /// Verifies that the canonical basic WPF example remains executable against the real fixture.
    /// </summary>
    [Fact]
    [Trait("Category", "WpfUI")]
    public async Task CanonicalBasicWpfExample_ShouldExecuteAgainstFixture()
    {
        _fixture.SkipIfUnavailable();

        var scriptPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "examples",
            "test-wpf-basic.json"));
        var content = await File.ReadAllTextAsync(scriptPath);

        var result = await ScriptService.ExecuteScriptContentAsync(content, _fixture.App!.ProcessId);

        result.Success.Should().BeTrue(result.Message);
        result.StepsExecuted.Should().Be(7);
        result.StepsFailed.Should().Be(0);
    }
}
