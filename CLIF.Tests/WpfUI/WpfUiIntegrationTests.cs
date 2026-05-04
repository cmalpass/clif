using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FluentAssertions;
using Xunit;

namespace CLIF.Tests.WpfUI;

/// <summary>
/// End-to-end WPF UI automation tests that drive the running <c>TestWpfApp</c>
/// through the Windows UI Automation (UIA3) stack via FlaUI.
/// </summary>
/// <remarks>
/// These tests require <c>TestWpfApp.exe</c> to be compiled and launchable.
/// The <see cref="WpfTestAppFixture"/> starts and owns the process for the
/// lifetime of the collection.  When the app cannot be launched (e.g. build
/// artefacts absent, or a runner without UI Automation support) every test is
/// marked as <c>Skipped</c> via <see cref="WpfTestAppFixture.SkipIfUnavailable"/>,
/// so failures are visible in the test report rather than silently passing.
///
/// Run these tests specifically with:
/// <code>
/// dotnet test --filter "Category=WpfUI"
/// </code>
/// </remarks>
[Collection("WpfUI")]
public class WpfUiIntegrationTests
{
    private readonly WpfTestAppFixture _fixture;

    public WpfUiIntegrationTests(WpfTestAppFixture fixture)
    {
        _fixture = fixture;
    }

    // ─── Window ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void MainWindow_ShouldHaveCorrectTitle()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        window.Should().NotBeNull();
        window.Title.Should().Contain("Comprehensive WPF Controls Test App");
    }

    [Fact]
    [Trait("Category", "WpfUI")]
    public void MainWindow_ShouldNotBeOffscreen()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        window.IsOffscreen.Should().BeFalse("the main window should be visible on screen");
    }

    // ─── TextBox ─────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void TestTextBox_WhenTextSet_ShouldContainNewText()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var textBox = window
            .FindFirstDescendant(cf => cf.ByAutomationId("TestTextBox"))
            ?.AsTextBox();

        textBox.Should().NotBeNull("TestTextBox should exist in the main window");

        const string expected = "Hello CLIF";
        textBox!.Text = expected;
        textBox.Text.Should().Be(expected);
    }

    // ─── CheckBox ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void TestCheckBox_WhenToggled_ShouldChangeState()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var checkBox = window
            .FindFirstDescendant(cf => cf.ByAutomationId("TestCheckBox"))
            ?.AsCheckBox();

        checkBox.Should().NotBeNull("TestCheckBox should exist in the main window");

        var initialState = checkBox!.IsChecked;
        checkBox.Toggle();
        checkBox.IsChecked.Should().NotBe(initialState, "toggling should flip the checked state");
    }

    // ─── ComboBox ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void TestComboBox_WhenItemSelected_ShouldReflectSelection()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var comboBox = window
            .FindFirstDescendant(cf => cf.ByAutomationId("TestComboBox"))
            ?.AsComboBox();

        comboBox.Should().NotBeNull("TestComboBox should exist in the main window");

        comboBox!.Select("Item 2");
        comboBox.SelectedItem?.Name.Should().Contain("Item 2",
            "selecting 'Item 2' should update the combobox selection");
    }

    // ─── Button ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void ToggleButton_WhenClicked_WindowShouldRemainVisible()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var button = window
            .FindFirstDescendant(cf => cf.ByAutomationId("ToggleButton"))
            ?.AsButton();

        button.Should().NotBeNull("ToggleButton should exist in the main window");
        button!.Click();

        // After clicking the toggle button (which only changes its label, no dialogs)
        // the window must still be visible.
        window.IsOffscreen.Should().BeFalse("the window should remain visible after a button click");
    }

    // ─── TreeView ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void TestTreeView_ShouldContainRootNode()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var treeView = window.FindFirstDescendant(cf => cf.ByAutomationId("TestTreeView"));

        treeView.Should().NotBeNull("TestTreeView should exist in the main window");

        // "Root Node" is expanded by default in XAML, so it should be reachable
        // as a tree-item descendant immediately.
        var rootNode = treeView!.FindFirstDescendant(
            cf => cf.ByControlType(ControlType.TreeItem).And(cf.ByName("Root Node")));

        rootNode.Should().NotBeNull("'Root Node' should be present and visible in the TreeView");
    }

    // ─── TabControl ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void TestTabControl_WhenSecondTabSelected_ShouldUpdateIndex()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var tabControl = window
            .FindFirstDescendant(cf => cf.ByAutomationId("TestTabControl"))
            ?.AsTab();

        tabControl.Should().NotBeNull("TestTabControl should exist in the main window");

        tabControl!.SelectTabItem(1); // zero-based – selects "Tab 2"
        tabControl.SelectedTabItemIndex.Should().Be(1, "the second tab should now be active");
    }

    // ─── RadioButton ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "WpfUI")]
    public void RadioButton2_WhenSelected_ShouldBeChecked()
    {
        _fixture.SkipIfUnavailable();

        var window = _fixture.GetMainWindow();
        var radio = window
            .FindFirstDescendant(cf => cf.ByAutomationId("RadioButton2"))
            ?.AsRadioButton();

        radio.Should().NotBeNull("RadioButton2 should exist in the main window");

        radio!.IsChecked = true;
        radio.IsChecked.Should().BeTrue("selecting the radio button should mark it as checked");
    }
}
