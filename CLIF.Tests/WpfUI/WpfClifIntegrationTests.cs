using CLIF.Tests.Integration;
using FluentAssertions;

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
}
