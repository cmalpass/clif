// <copyright file="IAutomationService.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

/// <summary>Provides UI Automation operations for an attached desktop application.</summary>
public interface IAutomationService
{
    /// <summary>Gets a value indicating whether gets whether an automation session is currently attached.</summary>
    bool IsAttached { get; }

    /// <summary>Gets the attached process identifier, if any.</summary>
    int? AttachedProcessId { get; }

    /// <summary>Attaches the automation client to a process.</summary>
    /// <param name="processId">Identifier of the target process.</param>
    /// <returns><see langword="true"/> when the process was attached successfully.</returns>
    Task<bool> AttachToProcessAsync(int processId);

    /// <summary>Detaches from the currently attached process.</summary>
    /// <returns>A task that completes when detachment has finished.</returns>
    Task DetachAsync();

    // Element operations

    /// <summary>Finds the first element matching a selector.</summary>
    /// <param name="selector">Selector identifying the target element.</param>
    /// <returns>The matching element, or <see langword="null"/> when none is found.</returns>
    Task<AutomationElement?> FindElementAsync(string selector);

    /// <summary>Finds all elements matching a selector.</summary>
    /// <param name="selector">Selector identifying the target elements.</param>
    /// <returns>All matching elements.</returns>
    Task<AutomationElement[]> FindElementsAsync(string selector);

    // Basic interactions

    /// <summary>Invokes the primary click action on an element.</summary>
    /// <param name="element">Element to click.</param>
    /// <returns><see langword="true"/> when the action succeeds.</returns>
    Task<bool> ClickAsync(AutomationElement element);

    /// <summary>Invokes a double-click action on an element.</summary>
    /// <param name="element">Element to double-click.</param>
    /// <returns><see langword="true"/> when the action succeeds.</returns>
    Task<bool> DoubleClickAsync(AutomationElement element);

    /// <summary>Invokes a right-click action on an element.</summary>
    /// <param name="element">Element to right-click.</param>
    /// <returns><see langword="true"/> when the action succeeds.</returns>
    Task<bool> RightClickAsync(AutomationElement element);

    /// <summary>Types text into an element.</summary>
    /// <param name="element">Element receiving the text.</param>
    /// <param name="text">Text to enter.</param>
    /// <returns><see langword="true"/> when the action succeeds.</returns>
    Task<bool> TypeTextAsync(AutomationElement element, string text);

    /// <summary>Sets the value of an element.</summary>
    /// <param name="element">Element whose value should change.</param>
    /// <param name="value">Value to assign.</param>
    /// <returns><see langword="true"/> when the action succeeds.</returns>
    Task<bool> SetValueAsync(AutomationElement element, string value);

    // Advanced control interactions

    /// <summary>Selects a combo box item by its displayed text.</summary>
    /// <param name="element">Combo box to update.</param>
    /// <param name="itemText">Displayed item text.</param>
    /// <returns><see langword="true"/> when the item is selected.</returns>
    Task<bool> SelectComboBoxItemAsync(AutomationElement element, string itemText);

    /// <summary>Selects a combo box item by zero-based index.</summary>
    /// <param name="element">Combo box to update.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <returns><see langword="true"/> when the item is selected.</returns>
    Task<bool> SelectComboBoxItemByIndexAsync(AutomationElement element, int index);

    /// <summary>Selects a list box item by its displayed text.</summary>
    /// <param name="element">List box to update.</param>
    /// <param name="itemText">Displayed item text.</param>
    /// <returns><see langword="true"/> when the item is selected.</returns>
    Task<bool> SelectListBoxItemAsync(AutomationElement element, string itemText);

    /// <summary>Selects a list box item by zero-based index.</summary>
    /// <param name="element">List box to update.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <returns><see langword="true"/> when the item is selected.</returns>
    Task<bool> SelectListBoxItemByIndexAsync(AutomationElement element, int index);

    /// <summary>Sets the checked state of a check box.</summary>
    /// <param name="element">Check box to update.</param>
    /// <param name="isChecked">Desired checked state.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> SetCheckBoxAsync(AutomationElement element, bool isChecked);

    /// <summary>Sets the selected state of a radio button.</summary>
    /// <param name="element">Radio button to update.</param>
    /// <param name="isSelected">Desired selected state.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> SetRadioButtonAsync(AutomationElement element, bool isSelected);

    /// <summary>Sets a slider value.</summary>
    /// <param name="element">Slider to update.</param>
    /// <param name="value">Desired slider value.</param>
    /// <returns><see langword="true"/> when the value changes.</returns>
    Task<bool> SetSliderValueAsync(AutomationElement element, double value);

    /// <summary>Selects a tab by its displayed name.</summary>
    /// <param name="element">Tab control to update.</param>
    /// <param name="tabName">Displayed tab name.</param>
    /// <returns><see langword="true"/> when the tab is selected.</returns>
    Task<bool> SelectTabAsync(AutomationElement element, string tabName);

    /// <summary>Selects a tab by zero-based index.</summary>
    /// <param name="element">Tab control to update.</param>
    /// <param name="tabIndex">Zero-based tab index.</param>
    /// <returns><see langword="true"/> when the tab is selected.</returns>
    Task<bool> SelectTabByIndexAsync(AutomationElement element, int tabIndex);

    /// <summary>Expands a tree node.</summary>
    /// <param name="element">Tree node to expand.</param>
    /// <returns><see langword="true"/> when the node is expanded.</returns>
    Task<bool> ExpandTreeNodeAsync(AutomationElement element);

    /// <summary>Collapses a tree node.</summary>
    /// <param name="element">Tree node to collapse.</param>
    /// <returns><see langword="true"/> when the node is collapsed.</returns>
    Task<bool> CollapseTreeNodeAsync(AutomationElement element);

    /// <summary>Selects a tree node by its path.</summary>
    /// <param name="element">Tree root or node to search.</param>
    /// <param name="nodePath">Path identifying the desired node.</param>
    /// <returns><see langword="true"/> when the node is selected.</returns>
    Task<bool> SelectTreeNodeAsync(AutomationElement element, string nodePath);

    /// <summary>Sets a date picker value.</summary>
    /// <param name="element">Date picker to update.</param>
    /// <param name="date">Desired date.</param>
    /// <returns><see langword="true"/> when the date changes.</returns>
    Task<bool> SetDatePickerAsync(AutomationElement element, DateTime date);

    /// <summary>Sets a calendar date.</summary>
    /// <param name="element">Calendar to update.</param>
    /// <param name="date">Desired date.</param>
    /// <returns><see langword="true"/> when the date changes.</returns>
    Task<bool> SetCalendarDateAsync(AutomationElement element, DateTime date);

    /// <summary>Toggles an expander.</summary>
    /// <param name="element">Expander to toggle.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> ToggleExpanderAsync(AutomationElement element);

    /// <summary>Selects a data grid row by zero-based index.</summary>
    /// <param name="element">Data grid to update.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <returns><see langword="true"/> when the row is selected.</returns>
    Task<bool> SelectDataGridRowAsync(AutomationElement element, int rowIndex);

    /// <summary>Selects a data grid cell by zero-based row and column indexes.</summary>
    /// <param name="element">Data grid to update.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    /// <returns><see langword="true"/> when the cell is selected.</returns>
    Task<bool> SelectDataGridCellAsync(AutomationElement element, int rowIndex, int columnIndex);

    /// <summary>Invokes a menu item.</summary>
    /// <param name="element">Menu item to invoke.</param>
    /// <returns><see langword="true"/> when the invocation succeeds.</returns>
    Task<bool> InvokeMenuItemAsync(AutomationElement element);

    /// <summary>Sets the toggled state of a toggle button.</summary>
    /// <param name="element">Toggle button to update.</param>
    /// <param name="isToggled">Desired toggled state.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> SetToggleButtonAsync(AutomationElement element, bool isToggled);

    // DataGrid-specific operations

    /// <summary>Sets a data grid check box by row index.</summary>
    /// <param name="dataGridSelector">Selector for the data grid.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="isChecked">Desired checked state.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> SetDataGridCheckboxAsync(string dataGridSelector, int rowIndex, bool isChecked);

    /// <summary>Sets a data grid check box by row name.</summary>
    /// <param name="dataGridSelector">Selector for the data grid.</param>
    /// <param name="rowName">Name of the target row.</param>
    /// <param name="isChecked">Desired checked state.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> SetDataGridCheckboxByNameAsync(string dataGridSelector, string rowName, bool isChecked);

    /// <summary>Toggles a data grid check box by row index.</summary>
    /// <param name="dataGridSelector">Selector for the data grid.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <returns><see langword="true"/> when the state changes.</returns>
    Task<bool> ToggleDataGridCheckboxAsync(string dataGridSelector, int rowIndex);

    /// <summary>Gets the checked states for data grid rows.</summary>
    /// <param name="dataGridSelector">Selector for the data grid.</param>
    /// <returns>Checked states in row order.</returns>
    Task<bool[]> GetDataGridCheckboxStatesAsync(string dataGridSelector);

    // Data extraction

    /// <summary>Gets the accessible text for an element.</summary>
    /// <param name="element">Element to inspect.</param>
    /// <returns>The element text.</returns>
    Task<string> GetTextAsync(AutomationElement element);

    /// <summary>Gets the value exposed by an element.</summary>
    /// <param name="element">Element to inspect.</param>
    /// <returns>The element value.</returns>
    Task<string> GetValueAsync(AutomationElement element);

    /// <summary>Gets the automation properties for an element.</summary>
    /// <param name="element">Element to inspect.</param>
    /// <returns>A property name/value map.</returns>
    Task<Dictionary<string, object>> GetPropertiesAsync(AutomationElement element);

    /// <summary>Gets the displayed items in a combo box.</summary>
    /// <param name="element">Combo box to inspect.</param>
    /// <returns>Displayed items in order.</returns>
    Task<string[]> GetComboBoxItemsAsync(AutomationElement element);

    /// <summary>Gets the displayed items in a list box.</summary>
    /// <param name="element">List box to inspect.</param>
    /// <returns>Displayed items in order.</returns>
    Task<string[]> GetListBoxItemsAsync(AutomationElement element);

    /// <summary>Gets whether a check box is checked.</summary>
    /// <param name="element">Check box to inspect.</param>
    /// <returns>The current checked state.</returns>
    Task<bool> GetCheckBoxStateAsync(AutomationElement element);

    /// <summary>Gets whether a radio button is selected.</summary>
    /// <param name="element">Radio button to inspect.</param>
    /// <returns>The current selected state.</returns>
    Task<bool> GetRadioButtonStateAsync(AutomationElement element);

    /// <summary>Gets the current slider value.</summary>
    /// <param name="element">Slider to inspect.</param>
    /// <returns>The current value.</returns>
    Task<double> GetSliderValueAsync(AutomationElement element);

    /// <summary>Gets the selected tab name.</summary>
    /// <param name="element">Tab control to inspect.</param>
    /// <returns>The selected tab name.</returns>
    Task<string> GetSelectedTabAsync(AutomationElement element);

    /// <summary>Gets the path of a tree node.</summary>
    /// <param name="element">Tree node to inspect.</param>
    /// <returns>Node names from the root to the element.</returns>
    Task<string[]> GetTreeNodePathAsync(AutomationElement element);

    /// <summary>Gets the selected date picker value.</summary>
    /// <param name="element">Date picker to inspect.</param>
    /// <returns>The selected date, if available.</returns>
    Task<DateTime?> GetDatePickerValueAsync(AutomationElement element);

    /// <summary>Gets the selected calendar date.</summary>
    /// <param name="element">Calendar to inspect.</param>
    /// <returns>The selected date, if available.</returns>
    Task<DateTime?> GetCalendarDateAsync(AutomationElement element);

    /// <summary>Gets whether an expander is expanded.</summary>
    /// <param name="element">Expander to inspect.</param>
    /// <returns>The current expanded state.</returns>
    Task<bool> GetExpanderStateAsync(AutomationElement element);

    /// <summary>Gets the rows and values exposed by a data grid.</summary>
    /// <param name="element">Data grid to inspect.</param>
    /// <returns>Rows represented as property maps.</returns>
    Task<Dictionary<string, object>[]> GetDataGridDataAsync(AutomationElement element);

    /// <summary>Gets whether a toggle button is toggled.</summary>
    /// <param name="element">Toggle button to inspect.</param>
    /// <returns>The current toggled state.</returns>
    Task<bool> GetToggleButtonStateAsync(AutomationElement element);

    // Navigation

    /// <summary>Moves keyboard focus to an element.</summary>
    /// <param name="element">Element to focus.</param>
    /// <returns><see langword="true"/> when focus succeeds.</returns>
    Task<bool> FocusAsync(AutomationElement element);

    /// <summary>Scrolls an element into view.</summary>
    /// <param name="element">Element to bring into view.</param>
    /// <returns><see langword="true"/> when scrolling succeeds.</returns>
    Task<bool> ScrollToAsync(AutomationElement element);

    // Screenshots

    /// <summary>Captures the attached application's current screen.</summary>
    /// <returns>Screenshot data encoded as a byte array.</returns>
    Task<byte[]> TakeScreenshotAsync();

    /// <summary>Captures the bounds of an element.</summary>
    /// <param name="element">Element to capture.</param>
    /// <returns>Screenshot data encoded as a byte array.</returns>
    Task<byte[]> TakeElementScreenshotAsync(AutomationElement element);

    // Get root element for tree navigation

    /// <summary>Gets the root element of the attached process.</summary>
    /// <returns>The root element, or <see langword="null"/> when unavailable.</returns>
    Task<AutomationElement?> GetRootElementAsync();

    // Convenience helper to get the main window (same as root element when attached)

    /// <summary>Gets the main window of the attached process.</summary>
    /// <returns>The main window, or <see langword="null"/> when unavailable.</returns>
    Task<AutomationElement?> GetMainWindowAsync();
}

/// <summary>Describes a UI Automation element selector.</summary>
public class ElementSelector
{
    /// <summary>Gets or sets the element's visible name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the element's automation identifier.</summary>
    public string AutomationId { get; set; } = string.Empty;

    /// <summary>Gets or sets the element's class name.</summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>Gets or sets the element's control type.</summary>
    public string ControlType { get; set; } = string.Empty;

    /// <summary>Gets or sets an XPath expression for the element.</summary>
    public string XPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the zero-based match index; -1 means unspecified.</summary>
    public int Index { get; set; } = -1;
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
