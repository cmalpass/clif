using FlaUI.Core;
using FlaUI.Core.AutomationElements;

namespace CLIF.Core;

public interface IAutomationService
{
    Task<bool> AttachToProcessAsync(int processId);
    Task DetachAsync();
    bool IsAttached { get; }
    int? AttachedProcessId { get; }
    
    // Element operations
    Task<AutomationElement?> FindElementAsync(string selector);
    Task<AutomationElement[]> FindElementsAsync(string selector);
    
    // Basic interactions
    Task<bool> ClickAsync(AutomationElement element);
    Task<bool> DoubleClickAsync(AutomationElement element);
    Task<bool> RightClickAsync(AutomationElement element);
    Task<bool> TypeTextAsync(AutomationElement element, string text);
    Task<bool> SetValueAsync(AutomationElement element, string value);
    
    // Advanced control interactions
    Task<bool> SelectComboBoxItemAsync(AutomationElement element, string itemText);
    Task<bool> SelectComboBoxItemByIndexAsync(AutomationElement element, int index);
    Task<bool> SelectListBoxItemAsync(AutomationElement element, string itemText);
    Task<bool> SelectListBoxItemByIndexAsync(AutomationElement element, int index);
    Task<bool> SetCheckBoxAsync(AutomationElement element, bool isChecked);
    Task<bool> SetRadioButtonAsync(AutomationElement element, bool isSelected);
    Task<bool> SetSliderValueAsync(AutomationElement element, double value);
    Task<bool> SelectTabAsync(AutomationElement element, string tabName);
    Task<bool> SelectTabByIndexAsync(AutomationElement element, int tabIndex);
    Task<bool> ExpandTreeNodeAsync(AutomationElement element);
    Task<bool> CollapseTreeNodeAsync(AutomationElement element);
    Task<bool> SelectTreeNodeAsync(AutomationElement element, string nodePath);
    Task<bool> SetDatePickerAsync(AutomationElement element, DateTime date);
    Task<bool> SetCalendarDateAsync(AutomationElement element, DateTime date);
    Task<bool> ToggleExpanderAsync(AutomationElement element);
    Task<bool> SelectDataGridRowAsync(AutomationElement element, int rowIndex);
    Task<bool> SelectDataGridCellAsync(AutomationElement element, int rowIndex, int columnIndex);
    Task<bool> InvokeMenuItemAsync(AutomationElement element);
    Task<bool> SetToggleButtonAsync(AutomationElement element, bool isToggled);
    
    // DataGrid-specific operations
    Task<bool> SetDataGridCheckboxAsync(string dataGridSelector, int rowIndex, bool isChecked);
    Task<bool> SetDataGridCheckboxByNameAsync(string dataGridSelector, string rowName, bool isChecked);
    Task<bool> ToggleDataGridCheckboxAsync(string dataGridSelector, int rowIndex);
    Task<bool[]> GetDataGridCheckboxStatesAsync(string dataGridSelector);
    
    // Data extraction
    Task<string> GetTextAsync(AutomationElement element);
    Task<string> GetValueAsync(AutomationElement element);
    Task<Dictionary<string, object>> GetPropertiesAsync(AutomationElement element);
    Task<string[]> GetComboBoxItemsAsync(AutomationElement element);
    Task<string[]> GetListBoxItemsAsync(AutomationElement element);
    Task<bool> GetCheckBoxStateAsync(AutomationElement element);
    Task<bool> GetRadioButtonStateAsync(AutomationElement element);
    Task<double> GetSliderValueAsync(AutomationElement element);
    Task<string> GetSelectedTabAsync(AutomationElement element);
    Task<string[]> GetTreeNodePathAsync(AutomationElement element);
    Task<DateTime?> GetDatePickerValueAsync(AutomationElement element);
    Task<DateTime?> GetCalendarDateAsync(AutomationElement element);
    Task<bool> GetExpanderStateAsync(AutomationElement element);
    Task<Dictionary<string, object>[]> GetDataGridDataAsync(AutomationElement element);
    Task<bool> GetToggleButtonStateAsync(AutomationElement element);
    
    // Navigation
    Task<bool> FocusAsync(AutomationElement element);
    Task<bool> ScrollToAsync(AutomationElement element);
    
    // Screenshots
    Task<byte[]> TakeScreenshotAsync();
    Task<byte[]> TakeElementScreenshotAsync(AutomationElement element);
    
    // Get root element for tree navigation
    Task<AutomationElement?> GetRootElementAsync();
    
    // Convenience helper to get the main window (same as root element when attached)
    Task<AutomationElement?> GetMainWindowAsync();
}

public class ElementSelector
{
    public string Name { get; set; } = string.Empty;
    public string AutomationId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string XPath { get; set; } = string.Empty;
    public int Index { get; set; } = -1;
}