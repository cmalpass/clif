using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using CLIF.Core;
using System.Drawing;
using FlaUI.Core.Input;
using FlaUI.Core.Definitions;
using System.Runtime.InteropServices;

namespace CLIF.Services;

public class AutomationService : IAutomationService, IDisposable
{
    private readonly ILogger<AutomationService> _logger;
    private readonly ISessionCaptureService _captureService;
    private readonly IDataGridAutomationService _dataGridService;
    private readonly IDialogService _dialogService;
    private UIA3Automation? _automation;
    private FlaUI.Core.Application? _application;
    private AutomationElement? _rootElement;

    public bool IsAttached { get; private set; }
    public int? AttachedProcessId { get; private set; }

    public AutomationService(ILogger<AutomationService> logger, ISessionCaptureService captureService, IDataGridAutomationService dataGridService, IDialogService dialogService)
    {
        _logger = logger;
        _captureService = captureService;
        _dataGridService = dataGridService;
        _dialogService = dialogService;
    }

    public async Task<bool> AttachToProcessAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (IsAttached)
                {
                    DetachAsync().Wait();
                }

                _automation = new UIA3Automation();
                _application = FlaUI.Core.Application.Attach(processId);
                _rootElement = _application.GetMainWindow(_automation);

                if (_rootElement == null)
                {
                    _logger.LogWarning($"Could not get main window for process {processId}");
                    return false;
                }

                // Set the target window for focused screenshot capture
                _captureService.SetTargetWindow(_rootElement);

                IsAttached = true;
                AttachedProcessId = processId;
                _logger.LogInformation($"Successfully attached to process {processId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to attach to process {processId}");
                return false;
            }
        });
    }

    public async Task DetachAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                _rootElement = null;
                _application?.Dispose();
                _automation?.Dispose();

                IsAttached = false;
                AttachedProcessId = null;
                _logger.LogInformation("Successfully detached from process");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during detach");
            }
        });
    }

    public async Task<AutomationElement?> FindElementAsync(string selector)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!IsAttached || _rootElement == null)
                {
                    _logger.LogWarning("Not attached to any process");
                    return null;
                }

                return FindElementBySelector(_rootElement, selector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding element with selector: {selector}");
                return null;
            }
        });
    }

    public async Task<AutomationElement[]> FindElementsAsync(string selector)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!IsAttached || _rootElement == null)
                {
                    _logger.LogWarning("Not attached to any process");
                    return Array.Empty<AutomationElement>();
                }

                return FindElementsBySelector(_rootElement, selector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding elements with selector: {selector}");
                return Array.Empty<AutomationElement>();
            }
        });
    }

    public async Task<OperationResult> ClickAsync(AutomationElement element)
    {
        return await Task.Run(async () =>
        {
            try
            {
                // Capture state before click for validation
                var beforeState = await CaptureElementStateAsync(element);

                element.Click();
                _logger.LogInformation($"Clicked element: {element.Name ?? element.AutomationId}");

                // Wait for potential state changes
                await Task.Delay(AutomationConstants.DefaultDelayMs);

                // Validate click had an effect
                var afterState = await CaptureElementStateAsync(element);
                var stateChanged = ValidateStateChange(beforeState, afterState, element);

                string validationResult;
                if (stateChanged)
                {
                    validationResult = "✅ Element state changed as expected";
                    _logger.LogInformation(validationResult);
                }
                else
                {
                    validationResult = "ℹ️ No detectable state change (normal for buttons)";
                    _logger.LogInformation($"ℹ️ Click completed: {validationResult}");
                }

                // Check for and handle any modal dialogs that may have appeared
                await Task.Delay(AutomationConstants.ShortDelayMs); // Small delay to allow dialog to appear
                await HandleModalDialogsAsync();

                // Capture screenshot after interaction
                await _captureService.CaptureAfterInteractionAsync(
                    AutomationConstants.ClickAction,
                    element.AutomationId ?? element.Name ?? "Unknown",
                    true,
                    validationResult
                );

                return OperationResult.Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clicking element");
                return OperationResult.Fail("Error clicking element", ex);
            }
        });
    }

    public async Task<OperationResult> DoubleClickAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                element.DoubleClick();
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error double-clicking element");
                return OperationResult.Fail("Error double-clicking element", ex);
            }
        });
    }

    public async Task<OperationResult> RightClickAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                element.RightClick();
                return OperationResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error right-clicking element");
                return OperationResult.Fail("Error right-clicking element", ex);
            }
        });
    }

    public async Task<OperationResult> TypeTextAsync(AutomationElement element, string text)
    {
        return await Task.Run(async () =>
        {
            try
            {
                element.Focus();
                await Task.Delay(AutomationConstants.ShortDelayMs); // Small delay to ensure focus
                Keyboard.Type(text);
                _logger.LogInformation($"Typed text '{text}' into element");

                // Validate text was actually entered
                await Task.Delay(AutomationConstants.ValidationDelayMs); // Allow time for text to register
                var actualText = await GetElementTextAsync(element);
                string validationResult;
                bool success = true;

                if (actualText != null && actualText.Contains(text))
                {
                    validationResult = $"✅ Text validated: Found '{text}' in element";
                    _logger.LogInformation($"✅ Text input validated: Found '{text}' in element (current: '{actualText}')");
                }
                else
                {
                    validationResult = $"⚠️ Validation inconclusive: Expected '{text}', found '{actualText ?? "null"}'";
                    _logger.LogWarning($"⚠️ Text input validation inconclusive: Expected '{text}', found '{actualText ?? "null"}'");
                }

                // Capture screenshot after interaction
                await _captureService.CaptureAfterInteractionAsync(
                    AutomationConstants.TypeAction,
                    $"{element.AutomationId ?? element.Name ?? "Unknown"} = '{text}'",
                    success,
                    validationResult
                );

                return OperationResult.Ok(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error typing text");
                return OperationResult.Fail("Error typing text", ex);
            }
        });
    }

    public async Task<OperationResult> SetValueAsync(AutomationElement element, string value)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var beforeText = await GetElementTextAsync(element);

                if (element.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    valuePattern.SetValue(value);
                    _logger.LogInformation($"Set value '{value}' using ValuePattern");
                }
                else
                {
                    // Fallback to typing
                    element.Focus();
                    await Task.Delay(AutomationConstants.ShortDelayMs);

                    // Clear existing text and type new value
                    element.Focus();
                    await Task.Delay(AutomationConstants.ShortDelayMs);

                    // Select all and replace
                    var textBox = element.AsTextBox();
                    if (textBox != null)
                    {
                        textBox.Text = value;
                    }
                    else
                    {
                        // Fallback to keyboard
                        Keyboard.Type(value);
                    }
                    _logger.LogInformation($"Set value '{value}' using keyboard input");
                }

                // Validate the value was set
                await Task.Delay(AutomationConstants.ValidationDelayMs);
                var afterText = await GetElementTextAsync(element);
                string validationResult;
                bool success;

                if (afterText == value || (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(afterText)))
                {
                    validationResult = $"✅ Value validated: '{beforeText}' → '{afterText}'";
                    success = true;
                    _logger.LogInformation($"✅ Value setting validated: '{beforeText}' → '{afterText}'");
                }
                else
                {
                    validationResult = $"⚠️ Validation failed: Expected '{value}', found '{afterText}'";
                    success = false;
                    _logger.LogWarning($"⚠️ Value setting validation failed: Expected '{value}', found '{afterText}'");
                }

                // Capture screenshot after interaction
                await _captureService.CaptureAfterInteractionAsync(
                    string.IsNullOrEmpty(value) ? AutomationConstants.ClearAction : AutomationConstants.SetValueAction,
                    $"{element.AutomationId ?? element.Name ?? "Unknown"} = '{value}'",
                    success,
                    validationResult
                );

                if (success)
                    return OperationResult.Ok(validationResult);
                else
                    return OperationResult.Fail(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value");
                return OperationResult.Fail("Error setting value", ex);
            }
        });
    }

    public async Task<string> GetTextAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                return element.Name ?? element.Properties.Name.ValueOrDefault ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting text");
                return string.Empty;
            }
        });
    }

    public async Task<string> GetValueAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (element.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    return valuePattern.Value ?? string.Empty;
                }
                return element.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value");
                return string.Empty;
            }
        });
    }

    public async Task<Dictionary<string, object>> GetPropertiesAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            var properties = new Dictionary<string, object>();

            try
            {
                properties["Name"] = element.Name ?? string.Empty;
                properties["AutomationId"] = element.AutomationId ?? string.Empty;
                properties["ClassName"] = element.ClassName ?? string.Empty;
                properties["ControlType"] = element.ControlType.ToString();
                properties["IsEnabled"] = element.IsEnabled;
                properties["IsVisible"] = !element.IsOffscreen;
                properties["BoundingRectangle"] = element.BoundingRectangle.ToString();

                if (element.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    properties["Value"] = valuePattern.Value ?? string.Empty;
                }

                properties["ProcessId"] = element.Properties.ProcessId.ValueOrDefault;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting properties");
            }

            return properties;
        });
    }

    public async Task<bool> FocusAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                element.Focus();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error focusing element");
                return false;
            }
        });
    }

    public async Task<bool> ScrollToAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (element.Patterns.ScrollItem.TryGetPattern(out var scrollPattern))
                {
                    scrollPattern.ScrollIntoView();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scrolling to element");
                return false;
            }
        });
    }

    public async Task<byte[]> TakeScreenshotAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!IsAttached || _rootElement == null)
                    return Array.Empty<byte>();

                var capture = _rootElement.Capture();
                using var stream = new MemoryStream();
                capture.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking screenshot");
                return Array.Empty<byte>();
            }
        });
    }

    public async Task<byte[]> TakeElementScreenshotAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var capture = element.Capture();
                using var stream = new MemoryStream();
                capture.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error taking element screenshot");
                return Array.Empty<byte>();
            }
        });
    }

    public async Task<AutomationElement?> GetRootElementAsync()
    {
        return await Task.Run(() => _rootElement);
    }

    public async Task<AutomationElement?> GetMainWindowAsync()
    {
        // Return the cached root element which represents the main window when attached
        return await GetRootElementAsync();
    }

    private AutomationElement? FindElementBySelector(AutomationElement root, string selector)
    {
        return SelectorParser.FindElement(root, selector);
    }

    private AutomationElement[] FindElementsBySelector(AutomationElement root, string selector)
    {
        return SelectorParser.FindElements(root, selector);
    }

    // Advanced control interaction methods
    public async Task<bool> SelectComboBoxItemAsync(AutomationElement element, string itemText)
    {
        return await Task.Run(async () =>
        {
            try
            {
                // Get current selection before change
                var beforeSelection = await GetComboBoxSelectionAsync(element);

                var comboBox = element.AsComboBox();
                if (comboBox != null)
                {
                    comboBox.Select(itemText);
                    _logger.LogInformation($"Selected '{itemText}' from combo box");

                    await Task.Delay(200); // Allow selection to register

                    // Validate selection changed
                    var afterSelection = await GetComboBoxSelectionAsync(element);
                    string validationResult;
                    bool success;

                    if (afterSelection == itemText)
                    {
                        validationResult = $"✅ Selection validated: '{itemText}' is now selected";
                        success = true;
                        _logger.LogInformation($"✅ ComboBox selection validated: '{itemText}' is now selected");
                    }
                    else
                    {
                        validationResult = $"⚠️ Validation failed: Expected '{itemText}', found '{afterSelection ?? "null"}'";
                        success = false;
                        _logger.LogWarning($"⚠️ ComboBox selection validation failed: Expected '{itemText}', found '{afterSelection ?? "null"}'");
                    }

                    // Capture screenshot after interaction
                    await _captureService.CaptureAfterInteractionAsync(
                        "SELECT",
                        $"{element.AutomationId ?? element.Name ?? "Unknown"} = '{itemText}'",
                        success,
                        validationResult
                    );

                    return success;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting ComboBox item: {itemText}");
                return false;
            }
        });
    }

    public async Task<bool> SelectComboBoxItemByIndexAsync(AutomationElement element, int index)
    {
        return await Task.Run(() =>
        {
            try
            {
                var comboBox = element.AsComboBox();
                if (comboBox != null)
                {
                    comboBox.Select(index);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting ComboBox item by index: {index}");
                return false;
            }
        });
    }

    public async Task<bool> SelectListBoxItemAsync(AutomationElement element, string itemText)
    {
        return await Task.Run(() =>
        {
            try
            {
                var listBox = element.AsListBox();
                if (listBox != null)
                {
                    var item = listBox.Items.FirstOrDefault(i => i.Text == itemText);
                    if (item != null)
                    {
                        item.Select();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting ListBox item: {itemText}");
                return false;
            }
        });
    }

    public async Task<bool> SelectListBoxItemByIndexAsync(AutomationElement element, int index)
    {
        return await Task.Run(() =>
        {
            try
            {
                var listBox = element.AsListBox();
                if (listBox != null && index < listBox.Items.Length)
                {
                    listBox.Items[index].Select();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting ListBox item by index: {index}");
                return false;
            }
        });
    }

    public async Task<bool> SetCheckBoxAsync(AutomationElement element, bool isChecked)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var beforeState = await GetCheckBoxStateAsync(element);

                var checkBox = element.AsCheckBox();
                if (checkBox != null)
                {
                    checkBox.IsChecked = isChecked;
                    _logger.LogInformation($"Set CheckBox state to: {isChecked}");

                    await Task.Delay(200); // Allow state change to register

                    // Validate state changed
                    var afterState = await GetCheckBoxStateAsync(element);
                    string validationResult;
                    bool success;

                    if (afterState == isChecked)
                    {
                        validationResult = $"✅ CheckBox state validated: {(isChecked ? "Checked" : "Unchecked")}";
                        success = true;
                        _logger.LogInformation($"✅ CheckBox state validated: {(isChecked ? "Checked" : "Unchecked")}");
                    }
                    else
                    {
                        validationResult = $"⚠️ Validation failed: Expected {isChecked}, found {afterState}";
                        success = false;
                        _logger.LogWarning($"⚠️ CheckBox state validation failed: Expected {isChecked}, found {afterState}");
                    }

                    // Capture screenshot after interaction
                    await _captureService.CaptureAfterInteractionAsync(
                        "SET_CHECKBOX",
                        $"{element.AutomationId ?? element.Name ?? "Unknown"} = {(isChecked ? "Checked" : "Unchecked")}",
                        success,
                        validationResult
                    );

                    return success;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting CheckBox state: {isChecked}");
                return false;
            }
        });
    }

    public async Task<bool> SetRadioButtonAsync(AutomationElement element, bool isSelected)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var beforeState = await GetRadioButtonStateAsync(element);

                var radioButton = element.AsRadioButton();
                if (radioButton != null)
                {
                    if (isSelected)
                    {
                        radioButton.Click();
                        _logger.LogInformation($"Clicked RadioButton to select");
                    }

                    await Task.Delay(200); // Allow state change to register

                    // Validate state changed
                    var afterState = await GetRadioButtonStateAsync(element);
                    string validationResult;
                    bool success;

                    if (afterState == isSelected)
                    {
                        validationResult = $"✅ RadioButton state validated: {(isSelected ? "Selected" : "Not Selected")}";
                        success = true;
                        _logger.LogInformation($"✅ RadioButton state validated: {(isSelected ? "Selected" : "Not Selected")}");
                    }
                    else
                    {
                        validationResult = $"⚠️ Validation failed: Expected {isSelected}, found {afterState}";
                        success = false;
                        _logger.LogWarning($"⚠️ RadioButton state validation failed: Expected {isSelected}, found {afterState}");
                    }

                    // Capture screenshot after interaction
                    await _captureService.CaptureAfterInteractionAsync(
                        "SET_RADIOBUTTON",
                        $"{element.AutomationId ?? element.Name ?? "Unknown"} = {(isSelected ? "Selected" : "Not Selected")}",
                        success,
                        validationResult
                    );

                    return success;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting RadioButton state: {isSelected}");
                return false;
            }
        });
    }

    public async Task<bool> SetSliderValueAsync(AutomationElement element, double value)
    {
        return await Task.Run(() =>
        {
            try
            {
                var slider = element.AsSlider();
                if (slider != null)
                {
                    slider.Value = value;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting Slider value: {value}");
                return false;
            }
        });
    }

    public async Task<bool> SelectTabAsync(AutomationElement element, string tabName)
    {
        return await Task.Run(() =>
        {
            try
            {
                var tabControl = element.AsTab();
                if (tabControl != null)
                {
                    var tab = tabControl.TabItems.FirstOrDefault(t => t.Name == tabName);
                    if (tab != null)
                    {
                        tab.Select();
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting Tab: {tabName}");
                return false;
            }
        });
    }

    public async Task<bool> SelectTabByIndexAsync(AutomationElement element, int tabIndex)
    {
        return await Task.Run(() =>
        {
            try
            {
                var tabControl = element.AsTab();
                if (tabControl != null && tabIndex < tabControl.TabItems.Length)
                {
                    tabControl.TabItems[tabIndex].Select();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting Tab by index: {tabIndex}");
                return false;
            }
        });
    }

    public async Task<bool> ExpandTreeNodeAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var treeItem = element.AsTreeItem();
                if (treeItem != null)
                {
                    treeItem.Expand();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expanding TreeNode");
                return false;
            }
        });
    }

    public async Task<bool> CollapseTreeNodeAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var treeItem = element.AsTreeItem();
                if (treeItem != null)
                {
                    treeItem.Collapse();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collapsing TreeNode");
                return false;
            }
        });
    }

    public async Task<bool> SelectTreeNodeAsync(AutomationElement element, string nodePath)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Implementation would traverse the tree path and select the node
                // This is a simplified version
                var treeItem = element.AsTreeItem();
                if (treeItem != null)
                {
                    treeItem.Select();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error selecting TreeNode: {nodePath}");
                return false;
            }
        });
    }

    public async Task<bool> SetDatePickerAsync(AutomationElement element, DateTime date)
    {
        return await Task.Run(() =>
        {
            try
            {
                var datePicker = element.AsDateTimePicker();
                if (datePicker != null)
                {
                    datePicker.SelectedDate = date;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting DatePicker value: {date}");
                return false;
            }
        });
    }

    public async Task<bool> SetCalendarDateAsync(AutomationElement element, DateTime date)
    {
        return await Task.Run(() =>
        {
            try
            {
                // For Calendar controls, we need to find the specific date button
                var calendarDayButtons = element.FindAllDescendants(cf => cf.ByClassName("CalendarDayButton"));

                foreach (var dayButton in calendarDayButtons)
                {
                    var buttonName = dayButton.Properties.Name.ValueOrDefault;
                    if (!string.IsNullOrEmpty(buttonName) && buttonName.Contains(date.ToString("MMMM d, yyyy")))
                    {
                        dayButton.AsButton()?.Invoke();
                        _logger.LogInformation($"Selected calendar date: {date:yyyy-MM-dd}");
                        return true;
                    }
                }

                _logger.LogWarning($"Calendar date not found: {date:yyyy-MM-dd}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting Calendar date: {date}");
                return false;
            }
        });
    }

    public async Task<bool> ToggleExpanderAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var expandCollapsePattern = element.Patterns.ExpandCollapse.PatternOrDefault;
                if (expandCollapsePattern != null)
                {
                    if (expandCollapsePattern.ExpandCollapseState == ExpandCollapseState.Collapsed)
                    {
                        expandCollapsePattern.Expand();
                    }
                    else
                    {
                        expandCollapsePattern.Collapse();
                    }
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling Expander");
                return false;
            }
        });
    }

    public async Task<bool> SelectDataGridRowAsync(AutomationElement element, int rowIndex)
    {
        return await _dataGridService.SelectDataGridRowAsync(element, rowIndex);
    }

    public async Task<bool> SelectDataGridCellAsync(AutomationElement element, int rowIndex, int columnIndex)
    {
        return await _dataGridService.SelectDataGridCellAsync(element, rowIndex, columnIndex);
    }

    public async Task<bool> InvokeMenuItemAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var menuItem = element.AsMenuItem();
                if (menuItem != null)
                {
                    menuItem.Invoke();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking MenuItem");
                return false;
            }
        });
    }

    public async Task<bool> SetToggleButtonAsync(AutomationElement element, bool isToggled)
    {
        return await Task.Run(() =>
        {
            try
            {
                var toggleButton = element.AsToggleButton();
                if (toggleButton != null)
                {
                    toggleButton.IsToggled = isToggled;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting ToggleButton state: {isToggled}");
                return false;
            }
        });
    }

    // Data extraction methods for advanced controls
    public async Task<string[]> GetComboBoxItemsAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var comboBox = element.AsComboBox();
                if (comboBox != null)
                {
                    return comboBox.Items.Select(i => i.Text).ToArray();
                }
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ComboBox items");
                return Array.Empty<string>();
            }
        });
    }

    public async Task<string[]> GetListBoxItemsAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var listBox = element.AsListBox();
                if (listBox != null)
                {
                    return listBox.Items.Select(i => i.Text).ToArray();
                }
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ListBox items");
                return Array.Empty<string>();
            }
        });
    }

    public async Task<bool> GetCheckBoxStateAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var checkBox = element.AsCheckBox();
                return checkBox?.IsChecked == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting CheckBox state");
                return false;
            }
        });
    }

    public async Task<bool> GetRadioButtonStateAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var radioButton = element.AsRadioButton();
                return radioButton?.IsChecked == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting RadioButton state");
                return false;
            }
        });
    }

    public async Task<double> GetSliderValueAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var slider = element.AsSlider();
                return slider?.Value ?? 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Slider value");
                return 0.0;
            }
        });
    }

    public async Task<string> GetSelectedTabAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var tabControl = element.AsTab();
                var selectedTab = tabControl?.SelectedTabItem;
                return selectedTab?.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting selected Tab");
                return string.Empty;
            }
        });
    }

    public async Task<string[]> GetTreeNodePathAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                // This would build the path from root to the selected node
                // Simplified implementation
                var treeItem = element.AsTreeItem();
                return treeItem != null ? new[] { treeItem.Text } : Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting TreeNode path");
                return Array.Empty<string>();
            }
        });
    }

    public async Task<DateTime?> GetDatePickerValueAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var datePicker = element.AsDateTimePicker();
                return datePicker?.SelectedDate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting DatePicker value");
                return null;
            }
        });
    }

    public async Task<DateTime?> GetCalendarDateAsync(AutomationElement element)
    {
        return await Task.Run<DateTime?>(() =>
        {
            try
            {
                // For Calendar controls, find the status element to get selected date
                var rootElement = _automation?.GetDesktop();
                if (rootElement != null)
                {
                    var statusElement = rootElement.FindFirstDescendant(cf => cf.ByAutomationId("StatusTextBlock"));
                    if (statusElement != null)
                    {
                        var statusText = statusElement.Properties.Name.ValueOrDefault;
                        if (!string.IsNullOrEmpty(statusText) && statusText.Contains("Calendar date:"))
                        {
                            var dateStr = statusText.Replace("Calendar date:", "").Trim();
                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                            {
                                return (DateTime?)parsedDate;
                            }
                        }
                    }
                }

                _logger.LogWarning("Could not determine selected calendar date");
                return (DateTime?)null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Calendar date");
                return (DateTime?)null;
            }
        });
    }

    public async Task<bool> GetExpanderStateAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var expander = element.Patterns.ExpandCollapse.PatternOrDefault;
                return expander?.ExpandCollapseState == FlaUI.Core.Definitions.ExpandCollapseState.Expanded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Expander state");
                return false;
            }
        });
    }

    public async Task<Dictionary<string, object>[]> GetDataGridDataAsync(AutomationElement element)
    {
        return await _dataGridService.GetDataGridDataAsync(element);
    }

    public async Task<bool> GetToggleButtonStateAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var toggleButton = element.AsToggleButton();
                return toggleButton?.IsToggled == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ToggleButton state");
                return false;
            }
        });
    }

    private async Task<string?> GetElementTextAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Try different ways to get text based on control type
                if (element.ControlType == ControlType.Edit)
                {
                    return element.AsTextBox()?.Text;
                }
                else if (element.ControlType == ControlType.Text)
                {
                    return element.AsLabel()?.Text ?? element.Name;
                }
                else if (element.ControlType == ControlType.Document)
                {
                    return element.AsLabel()?.Text ?? element.Name;
                }
                return element.Name;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Could not get text from element: {ex.Message}");
                return element.Name;
            }
        });
    }

    private async Task<Dictionary<string, object?>> CaptureElementStateAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            var state = new Dictionary<string, object?>();
            try
            {
                state["IsEnabled"] = element.IsEnabled;
                state["Name"] = element.Name;
                state["ControlType"] = element.ControlType.ToString();

                if (element.ControlType == ControlType.CheckBox)
                {
                    state["IsChecked"] = element.AsCheckBox()?.IsChecked;
                }
                else if (element.ControlType == ControlType.RadioButton)
                {
                    state["IsSelected"] = element.AsRadioButton()?.IsChecked;
                }
                else if (element.ControlType == ControlType.Button)
                {
                    try
                    {
                        var toggleButton = element.AsToggleButton();
                        if (toggleButton != null)
                        {
                            state["ToggleState"] = toggleButton.ToggleState;
                        }
                    }
                    catch
                    {
                        // Not a toggle button, skip
                    }
                }
                else if (element.ControlType == ControlType.Edit)
                {
                    state["Text"] = element.AsTextBox()?.Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Could not capture all state for element: {ex.Message}");
            }
            return state;
        });
    }

    private bool ValidateStateChange(Dictionary<string, object?> before, Dictionary<string, object?> after, AutomationElement element)
    {
        try
        {
            // Check for meaningful state changes based on control type
            if (element.ControlType == ControlType.CheckBox)
            {
                var beforeChecked = before.GetValueOrDefault("IsChecked");
                var afterChecked = after.GetValueOrDefault("IsChecked");
                bool changed = !Equals(beforeChecked, afterChecked);
                if (changed) _logger.LogInformation($"CheckBox state changed: {beforeChecked} → {afterChecked}");
                return changed;
            }
            else if (element.ControlType == ControlType.RadioButton)
            {
                var beforeSelected = before.GetValueOrDefault("IsSelected");
                var afterSelected = after.GetValueOrDefault("IsSelected");
                bool changed = !Equals(beforeSelected, afterSelected);
                if (changed) _logger.LogInformation($"RadioButton state changed: {beforeSelected} → {afterSelected}");
                return changed;
            }
            else if (element.ControlType == ControlType.Button)
            {
                try
                {
                    var toggleButton = element.AsToggleButton();
                    if (toggleButton != null)
                    {
                        var beforeToggle = before.GetValueOrDefault("ToggleState");
                        var afterToggle = after.GetValueOrDefault("ToggleState");
                        bool changed = !Equals(beforeToggle, afterToggle);
                        if (changed) _logger.LogInformation($"ToggleButton state changed: {beforeToggle} → {afterToggle}");
                        return changed;
                    }
                }
                catch
                {
                    // Not a toggle button, treat as regular button
                }
                return false; // Regular button - no detectable change expected
            }
            else if (element.ControlType == ControlType.Button)
            {
                // For regular buttons, we can't easily detect state change
                // In a real scenario, you might check if a dialog appeared, etc.
                return false; // No detectable change expected
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug($"Error validating state change: {ex.Message}");
        }

        return false; // Default to no change detected
    }

    private async Task<string?> GetComboBoxSelectionAsync(AutomationElement comboBox)
    {
        return await Task.Run(() =>
        {
            try
            {
                var cb = comboBox.AsComboBox();
                return cb?.SelectedItem?.Name;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Could not get ComboBox selection: {ex.Message}");
                return null;
            }
        });
    }

    private async Task HandleModalDialogsAsync()
    {
        await _dialogService.HandleModalDialogsAsync(_automation);
    }

    // DataGrid-specific checkbox operations
    public async Task<bool> SetDataGridCheckboxAsync(string dataGridSelector, int rowIndex, bool isChecked)
    {
        var dataGrid = await FindElementAsync(dataGridSelector);
        if (dataGrid == null)
        {
            _logger.LogWarning($"DataGrid not found: {dataGridSelector}");
            return false;
        }
        return await _dataGridService.SetDataGridCheckboxAsync(dataGrid, rowIndex, isChecked);
    }

    public async Task<bool> SetDataGridCheckboxByNameAsync(string dataGridSelector, string rowName, bool isChecked)
    {
        var dataGrid = await FindElementAsync(dataGridSelector);
        if (dataGrid == null)
        {
            _logger.LogWarning($"DataGrid not found: {dataGridSelector}");
            return false;
        }
        return await _dataGridService.SetDataGridCheckboxByNameAsync(dataGrid, rowName, isChecked);
    }

    public async Task<bool> ToggleDataGridCheckboxAsync(string dataGridSelector, int rowIndex)
    {
        var dataGrid = await FindElementAsync(dataGridSelector);
        if (dataGrid == null)
        {
            _logger.LogWarning($"DataGrid not found: {dataGridSelector}");
            return false;
        }
        return await _dataGridService.ToggleDataGridCheckboxAsync(dataGrid, rowIndex);
    }

    public async Task<bool[]> GetDataGridCheckboxStatesAsync(string dataGridSelector)
    {
        var dataGrid = await FindElementAsync(dataGridSelector);
        if (dataGrid == null)
        {
            _logger.LogWarning($"DataGrid not found: {dataGridSelector}");
            return new bool[0];
        }
        return await _dataGridService.GetDataGridCheckboxStatesAsync(dataGrid);
    }

    public void Dispose()
    {
        DetachAsync().Wait();
    }
}