using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FluentAssertions;

namespace CLIF.Tests.CrossPlatformUI;

[Collection("CrossPlatformUI")]
public sealed class CrossPlatformUiIntegrationTests
{
    private readonly CrossPlatformAppFixture _fixture;

    public CrossPlatformUiIntegrationTests(CrossPlatformAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public void MainWindow_ShouldExposeFixtureIdentity()
    {
        var window = _fixture.GetMainWindow();

        window.Title.Should().Be("CLIF Cross-Platform Controls Test App");
        window.FindFirstDescendant(cf => cf.ByAutomationId("FixtureVersion"))
            .Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public void ButtonClick_ShouldUpdateVisibleStatus()
    {
        var window = _fixture.GetMainWindow();
        var button = window.FindFirstDescendant(cf => cf.ByAutomationId("TestButton"));

        button.Should().NotBeNull();
        var invokePattern = button!.Patterns.Invoke.PatternOrDefault;
        if (invokePattern != null)
        {
            invokePattern.Invoke();
        }
        else
        {
            button.AsButton().Click();
        }

        // UIA events from Avalonia are delivered asynchronously on the UI thread.
        Thread.Sleep(200);

        var status = window.FindFirstDescendant(cf => cf.ByAutomationId("StatusTextBlock"));
        status.Should().NotBeNull();
        status!.Name.Should().Be("Button clicked");
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public void TextBox_ShouldSupportValuePattern()
    {
        var window = _fixture.GetMainWindow();
        var textBox = window.FindFirstDescendant(cf => cf.ByAutomationId("TestTextBox"))?.AsTextBox();

        textBox.Should().NotBeNull();
        textBox!.Text = "Hello CLIF";
        textBox.Text.Should().Be("Hello CLIF");
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public void CheckBox_ShouldToggleState()
    {
        var window = _fixture.GetMainWindow();
        var checkBox = window.FindFirstDescendant(cf => cf.ByAutomationId("TestCheckBox"))?.AsCheckBox();

        checkBox.Should().NotBeNull();
        var initialState = checkBox!.IsChecked;
        checkBox.Toggle();

        checkBox.IsChecked.Should().NotBe(initialState);
    }

    [Fact]
    [Trait("Category", "CrossPlatformUI")]
    public void TabControl_ShouldExposeAllTabs()
    {
        var window = _fixture.GetMainWindow();
        var tabControl = window.FindFirstDescendant(cf => cf.ByAutomationId("TestTabControl"));

        tabControl.Should().NotBeNull();
        tabControl!.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
            .Should().HaveCount(3);
    }
}
