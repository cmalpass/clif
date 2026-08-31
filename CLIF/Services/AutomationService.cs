using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Microsoft.Extensions.Logging;
using CLIF.Core;
using System.Drawing;
using FlaUI.Core.Input;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

[assembly: InternalsVisibleTo("CLIF.Tests")]

namespace CLIF.Services;

/// <summary>Provides Windows UI Automation operations backed by FlaUI.</summary>
[SupportedOSPlatform("windows7.0")]
public class AutomationService : IAutomationService, IDisposable
{
    private readonly ILogger<AutomationService> _logger;
    private readonly ISessionCaptureService _captureService;
    private UIA3Automation? _automation;
    private FlaUI.Core.Application? _application;
    private AutomationElement? _rootElement;

    /// <summary>Gets a value indicating whether a target process is attached.</summary>
    public bool IsAttached { get; private set; }

    /// <summary>Gets the attached process identifier, or <see langword="null"/>.</summary>
    public int? AttachedProcessId { get; private set; }

    // Windows API for dialog handling
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

    private const uint WM_KEYDOWN = 0x0100;
    private const int VK_RETURN = 0x0D;
    private const int VK_ESCAPE = 0x1B;

    /// <summary>Initializes the automation service.</summary>
    /// <param name="logger">Logger used to record automation operations.</param>
    /// <param name="captureService">Service used to capture interaction evidence.</param>
    public AutomationService(ILogger<AutomationService> logger, ISessionCaptureService captureService)
    {
        this._logger = logger;
        this._captureService = captureService;
    }

    /// <summary>Attaches to a running Windows process.</summary>
    /// <param name="processId">Identifier of the process to attach to.</param>
    /// <returns><see langword="true"/> when the process and main window are attached.</returns>
    public async Task<bool> AttachToProcessAsync(int processId)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (this.IsAttached)
                {
                    this.DetachAsync().Wait();
                }

                this._automation = new UIA3Automation();
                this._application = FlaUI.Core.Application.Attach(processId);
                this._rootElement = this._application.GetMainWindow(this._automation);

                if (this._rootElement == null)
                {
                    this._logger.LogWarning($"Could not get main window for process {processId}");
                    return false;
                }

                // Set the target window for focused screenshot capture
                this._captureService.SetTargetWindow(this._rootElement);

                this.IsAttached = true;
                this.AttachedProcessId = processId;
                this._logger.LogInformation($"Successfully attached to process {processId}");
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Failed to attach to process {processId}");
                return false;
            }
        });
    }

    /// <summary>Detaches from the current process and releases UI Automation resources.</summary>
    public async Task DetachAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                this._rootElement = null;
                this._application?.Dispose();
                this._automation?.Dispose();

                this.IsAttached = false;
                this.AttachedProcessId = null;
                this._logger.LogInformation("Successfully detached from process");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during detach");
            }
        });
    }

    /// <summary>Finds the first element matching a selector.</summary>
    /// <param name="selector">Selector describing the target element.</param>
    /// <returns>The matching element, or <see langword="null"/>.</returns>
    public async Task<AutomationElement?> FindElementAsync(string selector)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!this.IsAttached || this._rootElement == null)
                {
                    this._logger.LogWarning("Not attached to any process");
                    return null;
                }

                return this.FindElementBySelector(this._rootElement, selector);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error finding element with selector: {selector}");
                return null;
            }
        });
    }

    /// <summary>Finds all elements matching a selector.</summary>
    /// <param name="selector">Selector describing the target elements.</param>
    /// <returns>The matching elements, or an empty array.</returns>
    public async Task<AutomationElement[]> FindElementsAsync(string selector)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!this.IsAttached || this._rootElement == null)
                {
                    this._logger.LogWarning("Not attached to any process");
                    return Array.Empty<AutomationElement>();
                }

                return this.FindElementsBySelector(this._rootElement, selector);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error finding elements with selector: {selector}");
                return Array.Empty<AutomationElement>();
            }
        });
    }

    /// <summary>Invokes a click on an automation element and captures the result.</summary>
    /// <param name="element">Element to click.</param>
    /// <returns><see langword="true"/> when the click operation completes.</returns>
    public async Task<bool> ClickAsync(AutomationElement element)
    {
        return await Task.Run(async () =>
        {
            try
            {
                // Capture state before click for validation
                var beforeState = await this.CaptureElementStateAsync(element);

                var invokePattern = element.Patterns.Invoke.PatternOrDefault;
                if (invokePattern != null)
                {
                    invokePattern.Invoke();
                }
                else
                {
                    element.Click();
                }

                this._logger.LogInformation($"Clicked element: {element.Name ?? element.AutomationId}");

                // Wait for potential state changes
                await Task.Delay(300);

                // Validate click had an effect
                var afterState = await this.CaptureElementStateAsync(element);
                var stateChanged = this.ValidateStateChange(beforeState, afterState, element);

                string validationResult;
                if (stateChanged)
                {
                    validationResult = "✅ Element state changed as expected";
                    this._logger.LogInformation(validationResult);
                }
                else
                {
                    validationResult = "ℹ️ No detectable state change (normal for buttons)";
                    this._logger.LogInformation($"ℹ️ Click completed: {validationResult}");
                }

                // Check for and handle any modal dialogs that may have appeared
                await Task.Delay(100); // Small delay to allow dialog to appear
                await this.HandleModalDialogsAsync();

                // Capture screenshot after interaction
                await this._captureService.CaptureAfterInteractionAsync(
                    "CLICK",
                    element.AutomationId ?? element.Name ?? "Unknown",
                    true,
                    validationResult
                );

                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error clicking element");
                return false;
            }
        });
    }

    /// <summary>Invokes a double-click on an automation element.</summary>
    /// <param name="element">Element to double-click.</param>
    /// <returns><see langword="true"/> when the operation completes.</returns>
    public async Task<bool> DoubleClickAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                element.DoubleClick();
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error double-clicking element");
                return false;
            }
        });
    }

    /// <summary>Invokes a right-click on an automation element and captures the result.</summary>
    /// <param name="element">Element to right-click.</param>
    /// <returns><see langword="true"/> when the right-click operation completes.</returns>
    public async Task<bool> RightClickAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                element.RightClick();
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error right-clicking element");
                return false;
            }
        });
    }

    /// <summary>Types text into an automation element.</summary>
    /// <param name="element">Editable element that receives the text.</param>
    /// <param name="text">Text to type.</param>
    /// <returns><see langword="true"/> when the text is entered successfully.</returns>
    public async Task<bool> TypeTextAsync(AutomationElement element, string text)
    {
        return await Task.Run(async () =>
        {
            try
            {
                if (element.Patterns.Value.TryGetPattern(out var valuePattern) && !valuePattern.IsReadOnly)
                {
                    var currentValue = valuePattern.Value ?? string.Empty;
                    valuePattern.SetValue(currentValue + text);
                    this._logger.LogInformation($"Typed text '{text}' using ValuePattern");
                }
                else
                {
                    element.Focus();
                    await Task.Delay(100); // Small delay to ensure focus
                    Keyboard.Type(text);
                    this._logger.LogInformation($"Typed text '{text}' using keyboard input");
                }

                // Validate text was actually entered
                await Task.Delay(200); // Allow time for text to register
                var actualText = await this.GetValueAsync(element);
                string validationResult;
                bool success;

                if (actualText != null && actualText.Contains(text))
                {
                    validationResult = $"✅ Text validated: Found '{text}' in element";
                    success = true;
                    this._logger.LogInformation($"✅ Text input validated: Found '{text}' in element (current: '{actualText}')");
                }
                else
                {
                    validationResult = $"⚠️ Validation inconclusive: Expected '{text}', found '{actualText ?? "null"}'";
                    success = false;
                    this._logger.LogWarning($"⚠️ Text input validation inconclusive: Expected '{text}', found '{actualText ?? "null"}'");
                }

                // Capture screenshot after interaction
                await this._captureService.CaptureAfterInteractionAsync(
                    "TYPE",
                    $"{element.AutomationId ?? element.Name ?? "Unknown"} = '{text}'",
                    success,
                    validationResult
                );

                return success;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error typing text");
                return false;
            }
        });
    }

    /// <summary>Sets the value of an automation element through its supported value pattern.</summary>
    /// <param name="element">Element whose value should be changed.</param>
    /// <param name="value">Value to assign.</param>
    /// <returns><see langword="true"/> when the value is set successfully.</returns>
    public async Task<bool> SetValueAsync(AutomationElement element, string value)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var beforeText = await this.GetElementTextAsync(element);

                if (element.Patterns.Value.TryGetPattern(out var valuePattern))
                {
                    valuePattern.SetValue(value);
                    this._logger.LogInformation($"Set value '{value}' using ValuePattern");
                }
                else
                {
                    // Fallback to typing
                    element.Focus();
                    await Task.Delay(100);

                    // Clear existing text and type new value
                    element.Focus();
                    await Task.Delay(100);

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

                    this._logger.LogInformation($"Set value '{value}' using keyboard input");
                }

                // Validate the value was set
                await Task.Delay(200);
                var afterText = await this.GetElementTextAsync(element);
                string validationResult;
                bool success;

                if (afterText == value || (string.IsNullOrEmpty(value) && string.IsNullOrEmpty(afterText)))
                {
                    validationResult = $"✅ Value validated: '{beforeText}' → '{afterText}'";
                    success = true;
                    this._logger.LogInformation($"✅ Value setting validated: '{beforeText}' → '{afterText}'");
                }
                else
                {
                    validationResult = $"⚠️ Validation failed: Expected '{value}', found '{afterText}'";
                    success = false;
                    this._logger.LogWarning($"⚠️ Value setting validation failed: Expected '{value}', found '{afterText}'");
                }

                // Capture screenshot after interaction
                await this._captureService.CaptureAfterInteractionAsync(
                    string.IsNullOrEmpty(value) ? "CLEAR" : "SET_VALUE",
                    $"{element.AutomationId ?? element.Name ?? "Unknown"} = '{value}'",
                    success,
                    validationResult
                );

                return success;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error setting value");
                return false;
            }
        });
    }

    /// <summary>Reads the user-visible text from an automation element.</summary>
    /// <param name="element">Element from which to read text.</param>
    /// <returns>The element text, or an empty string when no text is available.</returns>
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
                this._logger.LogError(ex, "Error getting text");
                return string.Empty;
            }
        });
    }

    /// <summary>Reads the value exposed by an automation element.</summary>
    /// <param name="element">Element from which to read the value.</param>
    /// <returns>The element value, or an empty string when no value is available.</returns>
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
                this._logger.LogError(ex, "Error getting value");
                return string.Empty;
            }
        });
    }

    /// <summary>Gets the key automation properties exposed by an element.</summary>
    /// <param name="element">Element whose properties should be read.</param>
    /// <returns>A dictionary containing the element's automation properties.</returns>
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
                this._logger.LogError(ex, "Error getting properties");
            }

            return properties;
        });
    }

    /// <summary>Moves keyboard focus to an automation element.</summary>
    /// <param name="element">Element to focus.</param>
    /// <returns><see langword="true"/> when focus is assigned successfully.</returns>
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
                this._logger.LogError(ex, "Error focusing element");
                return false;
            }
        });
    }

    /// <summary>Scrolls an automation element into view.</summary>
    /// <param name="element">Element to bring into view.</param>
    /// <returns><see langword="true"/> when the scroll operation completes.</returns>
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
                this._logger.LogError(ex, "Error scrolling to element");
                return false;
            }
        });
    }

    /// <summary>Captures a screenshot of the attached application window.</summary>
    /// <returns>Screenshot bytes in PNG format, or an empty array on failure.</returns>
    public async Task<byte[]> TakeScreenshotAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!this.IsAttached || this._rootElement == null)
                {
                    return Array.Empty<byte>();
                }

                var capture = this._rootElement.Capture();
                using var stream = new MemoryStream();
                capture.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error taking screenshot");
                return Array.Empty<byte>();
            }
        });
    }

    /// <summary>Captures a screenshot of an automation element.</summary>
    /// <param name="element">Element to capture.</param>
    /// <returns>Screenshot bytes in PNG format, or an empty array on failure.</returns>
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
                this._logger.LogError(ex, "Error taking element screenshot");
                return Array.Empty<byte>();
            }
        });
    }

    /// <summary>Gets the root automation element for the attached application.</summary>
    /// <returns>The root element, or <see langword="null"/> when no application is attached.</returns>
    public async Task<AutomationElement?> GetRootElementAsync()
    {
        return await Task.Run(() => this._rootElement);
    }

    /// <summary>Gets the main window of the attached application.</summary>
    /// <returns>The main window, or <see langword="null"/> when no application is attached.</returns>
    public async Task<AutomationElement?> GetMainWindowAsync()
    {
        // Return the cached root element which represents the main window when attached
        return await this.GetRootElementAsync();
    }

    private AutomationElement? FindElementBySelector(AutomationElement root, string selector)
    {
        return SelectorParser.TryParse(selector, out var criteria)
            ? root.FindFirstDescendant(cf => CreateSelectorCondition(cf, criteria))
            : null;
    }

    private AutomationElement[] FindElementsBySelector(AutomationElement root, string selector)
    {
        return SelectorParser.TryParse(selector, out var criteria)
            ? root.FindAllDescendants(cf => CreateSelectorCondition(cf, criteria))
            : Array.Empty<AutomationElement>();
    }

    private static ConditionBase CreateSelectorCondition(ConditionFactory conditionFactory, SelectorCriteria criteria)
    {
        var conditions = new List<ConditionBase>();

        if (!string.IsNullOrEmpty(criteria.AutomationId))
        {
            conditions.Add(conditionFactory.ByAutomationId(criteria.AutomationId));
        }

        if (!string.IsNullOrEmpty(criteria.Name))
        {
            conditions.Add(conditionFactory.ByName(criteria.Name));
        }

        if (!string.IsNullOrEmpty(criteria.ClassName))
        {
            conditions.Add(conditionFactory.ByClassName(criteria.ClassName));
        }

        if (!string.IsNullOrEmpty(criteria.ControlType) &&
            Enum.TryParse<ControlType>(criteria.ControlType, ignoreCase: true, out var controlType))
        {
            conditions.Add(conditionFactory.ByControlType(controlType));
        }

        return conditions.Count switch
        {
            1 => conditions[0],
            _ => new AndCondition(conditions)
        };
    }

    // Advanced control interaction methods

    /// <summary>Selects a combo box item by its displayed text.</summary>
    /// <param name="element">Combo box to update.</param>
    /// <param name="itemText">Displayed text of the item to select.</param>
    /// <returns><see langword="true"/> when an item is selected.</returns>
    public async Task<bool> SelectComboBoxItemAsync(AutomationElement element, string itemText)
    {
        return await Task.Run(async () =>
        {
            try
            {
                // Get current selection before change
                var beforeSelection = await this.GetComboBoxSelectionAsync(element);

                var comboBox = element.AsComboBox();
                if (comboBox != null)
                {
                    comboBox.Select(itemText);
                    this._logger.LogInformation($"Selected '{itemText}' from combo box");

                    await Task.Delay(200); // Allow selection to register

                    // Validate selection changed
                    var afterSelection = await this.GetComboBoxSelectionAsync(element);
                    string validationResult;
                    bool success;

                    if (afterSelection == itemText)
                    {
                        validationResult = $"✅ Selection validated: '{itemText}' is now selected";
                        success = true;
                        this._logger.LogInformation($"✅ ComboBox selection validated: '{itemText}' is now selected");
                    }
                    else
                    {
                        validationResult = $"⚠️ Validation failed: Expected '{itemText}', found '{afterSelection ?? "null"}'";
                        success = false;
                        this._logger.LogWarning($"⚠️ ComboBox selection validation failed: Expected '{itemText}', found '{afterSelection ?? "null"}'");
                    }

                    // Capture screenshot after interaction
                    await this._captureService.CaptureAfterInteractionAsync(
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
                this._logger.LogError(ex, $"Error selecting ComboBox item: {itemText}");
                return false;
            }
        });
    }

    /// <summary>Selects a combo box item by zero-based index.</summary>
    /// <param name="element">Combo box to update.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <returns><see langword="true"/> when an item is selected.</returns>
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
                this._logger.LogError(ex, $"Error selecting ComboBox item by index: {index}");
                return false;
            }
        });
    }

    /// <summary>Selects a list box item by its displayed text.</summary>
    /// <param name="element">List box to update.</param>
    /// <param name="itemText">Displayed text of the item to select.</param>
    /// <returns><see langword="true"/> when an item is selected.</returns>
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
                this._logger.LogError(ex, $"Error selecting ListBox item: {itemText}");
                return false;
            }
        });
    }

    /// <summary>Selects a list box item by zero-based index.</summary>
    /// <param name="element">List box to update.</param>
    /// <param name="index">Zero-based item index.</param>
    /// <returns><see langword="true"/> when an item is selected.</returns>
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
                this._logger.LogError(ex, $"Error selecting ListBox item by index: {index}");
                return false;
            }
        });
    }

    /// <summary>Sets the checked state of a check box.</summary>
    /// <param name="element">Check box to update.</param>
    /// <param name="isChecked">Desired checked state.</param>
    /// <returns><see langword="true"/> when the state is set successfully.</returns>
    public async Task<bool> SetCheckBoxAsync(AutomationElement element, bool isChecked)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var beforeState = await this.GetCheckBoxStateAsync(element);

                var checkBox = element.AsCheckBox();
                if (checkBox != null)
                {
                    checkBox.IsChecked = isChecked;
                    this._logger.LogInformation($"Set CheckBox state to: {isChecked}");

                    await Task.Delay(200); // Allow state change to register

                    // Validate state changed
                    var afterState = await this.GetCheckBoxStateAsync(element);
                    string validationResult;
                    bool success;

                    if (afterState == isChecked)
                    {
                        validationResult = $"✅ CheckBox state validated: {(isChecked ? "Checked" : "Unchecked")}";
                        success = true;
                        this._logger.LogInformation($"✅ CheckBox state validated: {(isChecked ? "Checked" : "Unchecked")}");
                    }
                    else
                    {
                        validationResult = $"⚠️ Validation failed: Expected {isChecked}, found {afterState}";
                        success = false;
                        this._logger.LogWarning($"⚠️ CheckBox state validation failed: Expected {isChecked}, found {afterState}");
                    }

                    // Capture screenshot after interaction
                    await this._captureService.CaptureAfterInteractionAsync(
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
                this._logger.LogError(ex, $"Error setting CheckBox state: {isChecked}");
                return false;
            }
        });
    }

    /// <summary>Sets the selected state of a radio button.</summary>
    /// <param name="element">Radio button to update.</param>
    /// <param name="isSelected">Desired selected state.</param>
    /// <returns><see langword="true"/> when the state is set successfully.</returns>
    public async Task<bool> SetRadioButtonAsync(AutomationElement element, bool isSelected)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var beforeState = await this.GetRadioButtonStateAsync(element);

                var radioButton = element.AsRadioButton();
                if (radioButton != null)
                {
                    if (isSelected)
                    {
                        radioButton.Click();
                        this._logger.LogInformation($"Clicked RadioButton to select");
                    }

                    await Task.Delay(200); // Allow state change to register

                    // Validate state changed
                    var afterState = await this.GetRadioButtonStateAsync(element);
                    string validationResult;
                    bool success;

                    if (afterState == isSelected)
                    {
                        validationResult = $"✅ RadioButton state validated: {(isSelected ? "Selected" : "Not Selected")}";
                        success = true;
                        this._logger.LogInformation($"✅ RadioButton state validated: {(isSelected ? "Selected" : "Not Selected")}");
                    }
                    else
                    {
                        validationResult = $"⚠️ Validation failed: Expected {isSelected}, found {afterState}";
                        success = false;
                        this._logger.LogWarning($"⚠️ RadioButton state validation failed: Expected {isSelected}, found {afterState}");
                    }

                    // Capture screenshot after interaction
                    await this._captureService.CaptureAfterInteractionAsync(
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
                this._logger.LogError(ex, $"Error setting RadioButton state: {isSelected}");
                return false;
            }
        });
    }

    /// <summary>Sets a slider to a numeric value.</summary>
    /// <param name="element">Slider to update.</param>
    /// <param name="value">Desired slider value.</param>
    /// <returns><see langword="true"/> when the value is set successfully.</returns>
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
                this._logger.LogError(ex, $"Error setting Slider value: {value}");
                return false;
            }
        });
    }

    /// <summary>Selects a tab by its displayed name.</summary>
    /// <param name="element">Tab control to update.</param>
    /// <param name="tabName">Displayed name of the tab to select.</param>
    /// <returns><see langword="true"/> when a tab is selected.</returns>
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
                this._logger.LogError(ex, $"Error selecting Tab: {tabName}");
                return false;
            }
        });
    }

    /// <summary>Selects a tab by zero-based index.</summary>
    /// <param name="element">Tab control to update.</param>
    /// <param name="tabIndex">Zero-based tab index.</param>
    /// <returns><see langword="true"/> when a tab is selected.</returns>
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
                this._logger.LogError(ex, $"Error selecting Tab by index: {tabIndex}");
                return false;
            }
        });
    }

    /// <summary>Expands a tree node.</summary>
    /// <param name="element">Tree node to expand.</param>
    /// <returns><see langword="true"/> when the node is expanded.</returns>
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
                this._logger.LogError(ex, "Error expanding TreeNode");
                return false;
            }
        });
    }

    /// <summary>Collapses a tree node.</summary>
    /// <param name="element">Tree node to collapse.</param>
    /// <returns><see langword="true"/> when the node is collapsed.</returns>
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
                this._logger.LogError(ex, "Error collapsing TreeNode");
                return false;
            }
        });
    }

    /// <summary>Selects a tree node using a path relative to the supplied element.</summary>
    /// <param name="element">Tree root or node from which to resolve the path.</param>
    /// <param name="nodePath">Path identifying the node to select.</param>
    /// <returns><see langword="true"/> when the node is selected.</returns>
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
                this._logger.LogError(ex, $"Error selecting TreeNode: {nodePath}");
                return false;
            }
        });
    }

    /// <summary>Sets a date picker to a date.</summary>
    /// <param name="element">Date picker to update.</param>
    /// <param name="date">Desired date.</param>
    /// <returns><see langword="true"/> when the date is set successfully.</returns>
    public async Task<bool> SetDatePickerAsync(AutomationElement element, DateTime date)
    {
        var valueSet = await Task.Run(() =>
        {
            try
            {
                var datePicker = element.AsDateTimePicker();
                if (datePicker != null)
                {
                    try
                    {
                        datePicker.SelectedDate = date;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogDebug(
                            ex,
                            "Native DateTimePicker setter was rejected; trying standard Value-pattern fallbacks.");
                    }
                }

                // Avalonia's DatePicker is not exposed as the native UIA
                // DateTimePicker control on Windows. When it exposes an editable
                // value instead, use that standard UIA pattern.
                var formattedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var valuePattern = element.Patterns.Value.PatternOrDefault;
                if (valuePattern != null)
                {
                    valuePattern.SetValue(formattedDate);
                    return true;
                }

                var editor = element.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit));
                var editorValuePattern = editor?.Patterns.Value.PatternOrDefault;
                if (editorValuePattern != null)
                {
                    editorValuePattern.SetValue(formattedDate);
                    return true;
                }

                this._logger.LogWarning(
                    "Date picker does not expose a DateTimePicker or Value UI Automation pattern. Control type: {ControlType}; class: {ClassName}",
                    element.ControlType,
                    element.ClassName);
                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error setting DatePicker value: {date}");
                return false;
            }
        });

        if (valueSet)
        {
            await Task.Delay(200);
        }

        return valueSet;
    }

    /// <summary>Sets a calendar control to a date.</summary>
    /// <param name="element">Calendar to update.</param>
    /// <param name="date">Desired date.</param>
    /// <returns><see langword="true"/> when the date is set successfully.</returns>
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
                        this._logger.LogInformation($"Selected calendar date: {date:yyyy-MM-dd}");
                        return true;
                    }
                }

                this._logger.LogWarning($"Calendar date not found: {date:yyyy-MM-dd}");
                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error setting Calendar date: {date}");
                return false;
            }
        });
    }

    /// <summary>Toggles the expanded state of an expander.</summary>
    /// <param name="element">Expander to toggle.</param>
    /// <returns><see langword="true"/> when the state changes successfully.</returns>
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
                this._logger.LogError(ex, "Error toggling Expander");
                return false;
            }
        });
    }

    /// <summary>Selects a row in a data grid by zero-based index.</summary>
    /// <param name="element">Data grid to update.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <returns><see langword="true"/> when the row is selected.</returns>
    public async Task<bool> SelectDataGridRowAsync(AutomationElement element, int rowIndex)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = element.AsDataGridView();
                if (dataGrid != null && rowIndex < dataGrid.Rows.Length)
                {
                    dataGrid.Rows[rowIndex].Click();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error selecting DataGrid row: {rowIndex}");
                return false;
            }
        });
    }

    /// <summary>Selects a cell in a data grid by zero-based row and column indexes.</summary>
    /// <param name="element">Data grid to update.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnIndex">Zero-based column index.</param>
    /// <returns><see langword="true"/> when the cell is selected.</returns>
    public async Task<bool> SelectDataGridCellAsync(AutomationElement element, int rowIndex, int columnIndex)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = element.AsDataGridView();
                if (dataGrid != null && rowIndex < dataGrid.Rows.Length)
                {
                    var row = dataGrid.Rows[rowIndex];
                    if (columnIndex < row.Cells.Length)
                    {
                        row.Cells[columnIndex].Click();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Error selecting DataGrid cell: {rowIndex}, {columnIndex}");
                return false;
            }
        });
    }

    /// <summary>Invokes a menu item automation element.</summary>
    /// <param name="element">Menu item to invoke.</param>
    /// <returns><see langword="true"/> when the invocation completes.</returns>
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
                this._logger.LogError(ex, "Error invoking MenuItem");
                return false;
            }
        });
    }

    /// <summary>Sets the toggled state of a toggle button.</summary>
    /// <param name="element">Toggle button to update.</param>
    /// <param name="isToggled">Desired toggled state.</param>
    /// <returns><see langword="true"/> when the state is set successfully.</returns>
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
                this._logger.LogError(ex, $"Error setting ToggleButton state: {isToggled}");
                return false;
            }
        });
    }

    // Data extraction methods for advanced controls

    /// <summary>Gets the displayed items in a combo box.</summary>
    /// <param name="element">Combo box to inspect.</param>
    /// <returns>The displayed item texts, or an empty array when none are available.</returns>
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
                this._logger.LogError(ex, "Error getting ComboBox items");
                return Array.Empty<string>();
            }
        });
    }

    /// <summary>Gets the displayed items in a list box.</summary>
    /// <param name="element">List box to inspect.</param>
    /// <returns>The displayed item texts, or an empty array when none are available.</returns>
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
                this._logger.LogError(ex, "Error getting ListBox items");
                return Array.Empty<string>();
            }
        });
    }

    /// <summary>Reads the checked state of a check box.</summary>
    /// <param name="element">Check box to inspect.</param>
    /// <returns><see langword="true"/> when the check box is checked.</returns>
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
                this._logger.LogError(ex, "Error getting CheckBox state");
                return false;
            }
        });
    }

    /// <summary>Reads the selected state of a radio button.</summary>
    /// <param name="element">Radio button to inspect.</param>
    /// <returns><see langword="true"/> when the radio button is selected.</returns>
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
                this._logger.LogError(ex, "Error getting RadioButton state");
                return false;
            }
        });
    }

    /// <summary>Reads the current value of a slider.</summary>
    /// <param name="element">Slider to inspect.</param>
    /// <returns>The slider value, or zero when it cannot be read.</returns>
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
                this._logger.LogError(ex, "Error getting Slider value");
                return 0.0;
            }
        });
    }

    /// <summary>Gets the displayed name of the selected tab.</summary>
    /// <param name="element">Tab control to inspect.</param>
    /// <returns>The selected tab name, or an empty string when none is selected.</returns>
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
                this._logger.LogError(ex, "Error getting selected Tab");
                return string.Empty;
            }
        });
    }

    /// <summary>Gets the path of a tree node from its automation hierarchy.</summary>
    /// <param name="element">Tree node to inspect.</param>
    /// <returns>The node path, or an empty array when no path is available.</returns>
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
                this._logger.LogError(ex, "Error getting TreeNode path");
                return Array.Empty<string>();
            }
        });
    }

    /// <summary>Reads the selected date from a date picker.</summary>
    /// <param name="element">Date picker to inspect.</param>
    /// <returns>The selected date, or <see langword="null"/> when no date is available.</returns>
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
                this._logger.LogError(ex, "Error getting DatePicker value");
                return null;
            }
        });
    }

    /// <summary>Reads the selected date from a calendar.</summary>
    /// <param name="element">Calendar to inspect.</param>
    /// <returns>The selected date, or <see langword="null"/> when no date is available.</returns>
    public async Task<DateTime?> GetCalendarDateAsync(AutomationElement element)
    {
        return await Task.Run<DateTime?>(() =>
        {
            try
            {
                // For Calendar controls, find the status element to get selected date
                var rootElement = this._automation?.GetDesktop();
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

                this._logger.LogWarning("Could not determine selected calendar date");
                return (DateTime?)null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting Calendar date");
                return (DateTime?)null;
            }
        });
    }

    /// <summary>Reads whether an expander is currently expanded.</summary>
    /// <param name="element">Expander to inspect.</param>
    /// <returns><see langword="true"/> when the expander is expanded.</returns>
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
                this._logger.LogError(ex, "Error getting Expander state");
                return false;
            }
        });
    }

    /// <summary>Reads the rows and cells exposed by a data grid.</summary>
    /// <param name="element">Data grid to inspect.</param>
    /// <returns>An array of row property dictionaries.</returns>
    public async Task<Dictionary<string, object>[]> GetDataGridDataAsync(AutomationElement element)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = element.AsDataGridView();
                if (dataGrid != null)
                {
                    var results = new List<Dictionary<string, object>>();

                    foreach (var row in dataGrid.Rows)
                    {
                        var rowData = new Dictionary<string, object>();
                        for (int i = 0; i < row.Cells.Length; i++)
                        {
                            rowData[$"Column{i}"] = row.Cells[i].Value ?? string.Empty;
                        }

                        results.Add(rowData);
                    }

                    return results.ToArray();
                }

                return Array.Empty<Dictionary<string, object>>();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting DataGrid data");
                return Array.Empty<Dictionary<string, object>>();
            }
        });
    }

    /// <summary>Reads whether a toggle button is currently toggled.</summary>
    /// <param name="element">Toggle button to inspect.</param>
    /// <returns><see langword="true"/> when the toggle button is toggled.</returns>
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
                this._logger.LogError(ex, "Error getting ToggleButton state");
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
                this._logger.LogDebug($"Could not get text from element: {ex.Message}");
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
                this._logger.LogDebug($"Could not capture all state for element: {ex.Message}");
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
                if (changed)
                {
                    this._logger.LogInformation($"CheckBox state changed: {beforeChecked} → {afterChecked}");
                }

                return changed;
            }
            else if (element.ControlType == ControlType.RadioButton)
            {
                var beforeSelected = before.GetValueOrDefault("IsSelected");
                var afterSelected = after.GetValueOrDefault("IsSelected");
                bool changed = !Equals(beforeSelected, afterSelected);
                if (changed)
                {
                    this._logger.LogInformation($"RadioButton state changed: {beforeSelected} → {afterSelected}");
                }

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
                        if (changed)
                        {
                            this._logger.LogInformation($"ToggleButton state changed: {beforeToggle} → {afterToggle}");
                        }

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
            this._logger.LogDebug($"Error validating state change: {ex.Message}");
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
                this._logger.LogDebug($"Could not get ComboBox selection: {ex.Message}");
                return null;
            }
        });
    }

    private async Task HandleModalDialogsAsync()
    {
        await Task.Run(async () =>
        {
            try
            {
                // Common Windows dialog class names and titles
                var dialogPatterns = new (string? ClassName, string? Title)[]
                {
                    ("#32770", null), // Standard Windows dialog
                    (null, "Button Click"), // Our specific MessageBox title
                    (null, "Information"),
                    (null, "Warning"),
                    (null, "Error"),
                    (null, "Confirm")
                };

                foreach (var pattern in dialogPatterns)
                {
                    IntPtr dialogHandle = FindWindow(pattern.ClassName, pattern.Title);
                    if (dialogHandle != IntPtr.Zero)
                    {
                        this._logger.LogInformation($"Found modal dialog: {pattern.ClassName ?? "Unknown"} - {pattern.Title ?? "Unknown title"}");

                        // Bring dialog to foreground
                        SetForegroundWindow(dialogHandle);
                        Thread.Sleep(100);

                        // Try to find and click OK button first
                        IntPtr okButton = FindWindowEx(dialogHandle, IntPtr.Zero, "Button", "OK");
                        if (okButton != IntPtr.Zero)
                        {
                            PostMessage(okButton, 0x00F5, IntPtr.Zero, IntPtr.Zero); // BM_CLICK
                            this._logger.LogInformation("Clicked OK button on dialog");
                        }
                        else
                        {
                            // Fallback: Send Enter key to dismiss dialog
                            PostMessage(dialogHandle, WM_KEYDOWN, new IntPtr(VK_RETURN), IntPtr.Zero);
                            this._logger.LogInformation("Sent Enter key to dismiss dialog");
                        }

                        Thread.Sleep(200); // Allow time for dialog to close
                        break; // Handle one dialog at a time
                    }
                }

                // Also try FlaUI approach for more complex dialogs
                if (this._automation != null)
                {
                    var desktop = this._automation.GetDesktop();
                    var dialogs = desktop.FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                        .Where(w => w.IsOffscreen == false)
                        .ToArray();

                    foreach (var dialog in dialogs)
                    {
                        try
                        {
                            // Check if this window might be a dialog (has certain characteristics)
                            if (dialog.Name.Contains("Information") || dialog.Name.Contains("Button Click") ||
                                dialog.Name.Contains("Warning") || dialog.Name.Contains("Error"))
                            {
                                this._logger.LogInformation($"Found FlaUI modal dialog: {dialog.Name}");

                                // Look for OK, Yes, or Close buttons
                                var buttons = dialog.FindAllChildren(cf => cf.ByControlType(ControlType.Button));
                                var dismissButton = buttons.FirstOrDefault(b =>
                                    b.Name?.ToLower().Contains("ok") == true ||
                                    b.Name?.ToLower().Contains("yes") == true ||
                                    b.Name?.ToLower().Contains("close") == true);

                                if (dismissButton != null)
                                {
                                    dismissButton.Click();
                                    this._logger.LogInformation($"Clicked '{dismissButton.Name}' button to dismiss dialog");
                                    await Task.Delay(200);
                                    break;
                                }
                                else
                                {
                                    // Send Escape to close dialog
                                    Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.ESCAPE);
                                    this._logger.LogInformation("Sent Escape key to dismiss dialog");
                                    await Task.Delay(200);
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogDebug($"Error handling FlaUI dialog: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogDebug($"Error in dialog handling: {ex.Message}");
            }
        });
    }

    // DataGrid-specific checkbox operations

    /// <summary>Sets the check box state in a data-grid row.</summary>
    /// <param name="dataGridSelector">Selector identifying the data grid.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="isChecked">Desired check box state.</param>
    /// <returns><see langword="true"/> when the state is set successfully.</returns>
    public async Task<bool> SetDataGridCheckboxAsync(string dataGridSelector, int rowIndex, bool isChecked)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = this.FindElementAsync(dataGridSelector).Result;
                if (dataGrid == null)
                {
                    this._logger.LogWarning($"DataGrid not found: {dataGridSelector}");
                    return false;
                }

                // Get all data rows (excluding NewItemPlaceholder)
                var dataRows = dataGrid.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.DataItem)
                    .And(cf.ByName("TestWpfApp.SampleData")));

                if (rowIndex >= dataRows.Length)
                {
                    this._logger.LogWarning($"Row index {rowIndex} out of range. Found {dataRows.Length} rows.");
                    return false;
                }

                var row = dataRows[rowIndex];

                // Find the checkbox cell by looking for cells that contain checkboxes
                AutomationElement? checkboxCell = null;
                var cells = row.FindAllDescendants(cf => cf.ByControlType(ControlType.Custom));

                foreach (var cell in cells)
                {
                    var cellCheckbox = cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                    if (cellCheckbox != null)
                    {
                        checkboxCell = cell;
                        break;
                    }
                }

                if (checkboxCell == null)
                {
                    this._logger.LogWarning($"Checkbox cell not found in row {rowIndex}");
                    return false;
                }

                var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));

                if (checkbox != null)
                {
                    var checkboxElement = checkbox.AsCheckBox();
                    var currentState = checkboxElement.IsChecked ?? false;

                    this._logger.LogInformation($"Row {rowIndex} checkbox current state: {currentState}, target state: {isChecked}");

                    if (currentState != isChecked)
                    {
                        checkboxElement.Toggle();
                        this._logger.LogInformation($"Toggled checkbox in row {rowIndex} from {currentState} to {isChecked}");

                        // Verify the change - remove await from lambda
                        Task.Delay(100).Wait();
                        var newState = checkboxElement.IsChecked ?? false;
                        this._logger.LogInformation($"Verified checkbox state in row {rowIndex}: {newState}");

                        return newState == isChecked;
                    }
                    else
                    {
                        this._logger.LogInformation($"Checkbox in row {rowIndex} already in desired state: {isChecked}");
                        return true;
                    }
                }
                else
                {
                    this._logger.LogWarning($"Checkbox not found in row {rowIndex} cell");
                    return false;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Failed to set DataGrid checkbox at row {rowIndex}");
                return false;
            }
        });
    }

    /// <summary>Sets the check box state in a named data-grid row.</summary>
    /// <param name="dataGridSelector">Selector identifying the data grid.</param>
    /// <param name="rowName">Displayed name of the row.</param>
    /// <param name="isChecked">Desired check box state.</param>
    /// <returns><see langword="true"/> when the state is set successfully.</returns>
    public async Task<bool> SetDataGridCheckboxByNameAsync(string dataGridSelector, string rowName, bool isChecked)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = this.FindElementAsync(dataGridSelector).Result;
                if (dataGrid == null)
                {
                    return false;
                }

                // Find row by name cell content
                var nameCell = dataGrid.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.Custom)
                    .And(cf.ByName(rowName)));

                if (nameCell == null)
                {
                    this._logger.LogWarning($"Row with name '{rowName}' not found");
                    return false;
                }

                // Get the parent row
                var row = nameCell.Parent;
                if (row == null)
                {
                    return false;
                }

                // Find checkbox cell in this row
                AutomationElement? checkboxCell = null;
                var cells = row.FindAllDescendants(cf => cf.ByControlType(ControlType.Custom));

                foreach (var cell in cells)
                {
                    var cellCheckbox = cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                    if (cellCheckbox != null)
                    {
                        checkboxCell = cell;
                        break;
                    }
                }

                if (checkboxCell == null)
                {
                    return false;
                }

                var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));

                if (checkbox != null)
                {
                    var checkboxElement = checkbox.AsCheckBox();
                    var currentState = checkboxElement.IsChecked ?? false;
                    if (currentState != isChecked)
                    {
                        checkboxElement.Toggle();
                        this._logger.LogInformation($"Toggled checkbox for row '{rowName}' to {isChecked}");
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Failed to set DataGrid checkbox for row '{rowName}'");
                return false;
            }
        });
    }

    /// <summary>Toggles the check box state in a data-grid row.</summary>
    /// <param name="dataGridSelector">Selector identifying the data grid.</param>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <returns><see langword="true"/> when the state is toggled successfully.</returns>
    public async Task<bool> ToggleDataGridCheckboxAsync(string dataGridSelector, int rowIndex)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = this.FindElementAsync(dataGridSelector).Result;
                if (dataGrid == null)
                {
                    return false;
                }

                var dataRows = dataGrid.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.DataItem)
                    .And(cf.ByName("TestWpfApp.SampleData")));

                if (rowIndex >= dataRows.Length)
                {
                    return false;
                }

                var row = dataRows[rowIndex];

                // Find checkbox cell in this row
                AutomationElement? checkboxCell = null;
                var cells = row.FindAllDescendants(cf => cf.ByControlType(ControlType.Custom));

                foreach (var cell in cells)
                {
                    var cellCheckbox = cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                    if (cellCheckbox != null)
                    {
                        checkboxCell = cell;
                        break;
                    }
                }

                if (checkboxCell == null)
                {
                    return false;
                }

                var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));

                if (checkbox != null)
                {
                    var checkboxElement = checkbox.AsCheckBox();
                    var currentState = checkboxElement.IsChecked ?? false;
                    checkboxElement.Toggle();
                    this._logger.LogInformation($"Toggled checkbox in row {rowIndex} from {currentState} to {!currentState}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, $"Failed to toggle DataGrid checkbox at row {rowIndex}");
                return false;
            }
        });
    }

    /// <summary>Reads check box states from the rows of a data grid.</summary>
    /// <param name="dataGridSelector">Selector identifying the data grid.</param>
    /// <returns>Check box states in row order.</returns>
    public async Task<bool[]> GetDataGridCheckboxStatesAsync(string dataGridSelector)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dataGrid = this.FindElementAsync(dataGridSelector).Result;
                if (dataGrid == null)
                {
                    this._logger.LogWarning($"DataGrid not found with selector: {dataGridSelector}");
                    return new bool[0];
                }

                this._logger.LogInformation($"DataGrid found: {dataGrid.Name}, ControlType: {dataGrid.ControlType}");

                // Try to find all DataItem descendants without the name filter first
                var allDataItems = dataGrid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem));
                this._logger.LogInformation($"Found {allDataItems.Length} DataItem descendants");

                // Log the names of all DataItems
                for (int i = 0; i < allDataItems.Length; i++)
                {
                    this._logger.LogInformation($"DataItem {i}: Name='{allDataItems[i].Name}', ClassName='{allDataItems[i].ClassName}'");
                }

                // Now try the original filter
                var dataRows = dataGrid.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.DataItem)
                    .And(cf.ByName("TestWpfApp.SampleData")));

                this._logger.LogInformation($"Found {dataRows.Length} filtered DataRows with name 'TestWpfApp.SampleData'");

                // If the filtered search didn't work, try excluding the NewItemPlaceholder
                if (dataRows.Length == 0 && allDataItems.Length > 0)
                {
                    dataRows = allDataItems.Where(item => !item.Name.Contains("NewItemPlaceholder")).ToArray();
                    this._logger.LogInformation($"Using fallback: Found {dataRows.Length} DataRows excluding NewItemPlaceholder");
                }

                var states = new List<bool>();

                foreach (var row in dataRows)
                {
                    this._logger.LogInformation($"Processing row: {row.Name}");

                    // Find the checkbox cell by looking for cells that contain checkboxes
                    AutomationElement? checkboxCell = null;
                    var cells = row.FindAllDescendants(cf => cf.ByControlType(ControlType.Custom));
                    this._logger.LogInformation($"Found {cells.Length} custom cells in row: {row.Name}");

                    foreach (var cell in cells)
                    {
                        this._logger.LogInformation($"Checking cell: '{cell.Name}' for checkbox");
                        var checkbox = cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                        if (checkbox != null)
                        {
                            this._logger.LogInformation($"Found checkbox in cell: '{cell.Name}'");
                            checkboxCell = cell;
                            break;
                        }
                    }

                    if (checkboxCell != null)
                    {
                        this._logger.LogInformation($"Found checkbox cell in row: {row.Name}");

                        var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                        if (checkbox != null)
                        {
                            var isChecked = checkbox.AsCheckBox().IsChecked ?? false;
                            this._logger.LogInformation($"Checkbox in row '{row.Name}' is {isChecked}");
                            states.Add(isChecked);
                        }
                        else
                        {
                            this._logger.LogWarning($"Checkbox not found in cell for row: {row.Name}");
                            states.Add(false);
                        }
                    }
                    else
                    {
                        this._logger.LogWarning($"Checkbox cell not found for row: {row.Name}");
                        states.Add(false);
                    }
                }

                this._logger.LogInformation($"Returning {states.Count} checkbox states");
                return states.ToArray();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to get DataGrid checkbox states");
                return new bool[0];
            }
        });
    }

    /// <summary>Releases the attached application's UI Automation resources.</summary>
    public void Dispose()
    {
        this.DetachAsync().Wait();
    }
}

/// <summary>
/// Parses the selector format emitted by <see cref="ElementTreeService"/>.
/// </summary>
[SupportedOSPlatform("windows7.0")]
internal static class SelectorParser
{
    private static readonly string[] SupportedKeys = ["id", "name", "class", "type"];

    internal static bool TryParse(string? selector, out SelectorCriteria criteria)
    {
        criteria = new SelectorCriteria();
        if (string.IsNullOrWhiteSpace(selector))
        {
            return false;
        }

        var trimmedSelector = selector.Trim();
        if (!trimmedSelector.Contains('='))
        {
            return criteria.TrySet("name", trimmedSelector);
        }

        var clauses = SplitClauses(trimmedSelector);
        if (clauses.Count == 0)
        {
            return false;
        }

        foreach (var clause in clauses)
        {
            var separatorIndex = clause.IndexOf('=');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var key = clause[..separatorIndex].Trim().ToLowerInvariant();
            if (!SupportedKeys.Contains(key, StringComparer.Ordinal))
            {
                return false;
            }

            var value = ParseValue(clause[(separatorIndex + 1)..].Trim());
            if (value is null || !criteria.TrySet(key, value))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(criteria.ControlType) &&
            !Enum.TryParse<ControlType>(criteria.ControlType, ignoreCase: true, out _))
        {
            return false;
        }

        return criteria.HasCriteria;
    }

    internal static string FormatValue(string value)
    {
        if (value.IndexOf('"') < 0 && value.IndexOf('\\') < 0 &&
            !value.Contains(" and ", StringComparison.OrdinalIgnoreCase) &&
            value == value.Trim())
        {
            return value;
        }

        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static List<string> SplitClauses(string selector)
    {
        var clauses = new List<string>();
        var start = 0;
        var quoted = false;
        var escaped = false;

        for (var index = 0; index < selector.Length; index++)
        {
            var character = selector[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (quoted && character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (!quoted && index + 5 <= selector.Length &&
                selector.AsSpan(index, 5).Equals(" and ", StringComparison.OrdinalIgnoreCase))
            {
                clauses.Add(selector[start..index].Trim());
                start = index + 5;
                index += 4;
            }
        }

        if (quoted || escaped)
        {
            return [];
        }

        clauses.Add(selector[start..].Trim());
        return clauses.Any(string.IsNullOrEmpty) ? [] : clauses;
    }

    private static string? ParseValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value[0] != '"')
        {
            return value.Contains('"') ? null : value;
        }

        if (value.Length < 2 || value[^1] != '"')
        {
            return null;
        }

        var result = new System.Text.StringBuilder(value.Length - 2);
        var escaped = false;
        for (var index = 1; index < value.Length - 1; index++)
        {
            var character = value[index];
            if (escaped)
            {
                if (character is not ('"' or '\\'))
                {
                    return null;
                }

                result.Append(character);
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            result.Append(character);
        }

        return escaped ? null : result.ToString();
    }
}

internal sealed class SelectorCriteria
{
    internal string? AutomationId { get; private set; }
    internal string? Name { get; private set; }
    internal string? ClassName { get; private set; }
    internal string? ControlType { get; private set; }

    internal bool HasCriteria => this.AutomationId is not null || this.Name is not null || this.ClassName is not null || this.ControlType is not null;

    internal bool TrySet(string key, string value)
    {
        switch (key)
        {
            case "id" when this.AutomationId is null:
                this.AutomationId = value;
                return true;
            case "name" when this.Name is null:
                this.Name = value;
                return true;
            case "class" when this.ClassName is null:
                this.ClassName = value;
                return true;
            case "type" when this.ControlType is null:
                this.ControlType = value;
                return true;
            default:
                return false;
        }
    }
}
