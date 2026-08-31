// <copyright file="InteractCommand.cs" company="CLIF - Comprehensive UI Automation CLI">
// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// </copyright>
// Licensed under the MIT License.

using System.CommandLine;
using CLIF.Core;
using CLIF.Services;
using Microsoft.Extensions.Logging;

namespace CLIF.Commands;

/// <summary>Provides advanced interactions with common WPF controls.</summary>
public class InteractCommand : Command
{
    private readonly ISessionCaptureService captureService;

    /// <summary>Initializes a new instance of the <see cref="InteractCommand"/> class. Creates an interaction command backed by automation and session services.</summary>
    /// <param name="automationService">Service used to locate and manipulate controls.</param>
    /// <param name="captureService">Service used to record the interaction session.</param>
    /// <param name="logger">Logger used to report interaction failures.</param>
    public InteractCommand(IAutomationService automationService, ISessionCaptureService captureService, ILogger<InteractCommand> logger)
        : base("interact", "Advanced interactions with various WPF controls")
    {
        this.captureService = captureService;

        // Element selector options
        var elementOption = new Option<string>(
            "--element",
            "Element selector (name=, id=, class=, type=)")
        { IsRequired = true };

        // Control type option
        var controlTypeOption = new Option<string>(
            "--control-type",
            "Control type (combobox, listbox, checkbox, radiobutton, slider, tab, tree, datepicker, calendar, expander, datagrid, menu, togglebutton)")
        { IsRequired = true };

        // Action option
        var actionOption = new Option<string>(
            "--action",
            "Action to perform (select, set, toggle, expand, collapse, invoke)")
        { IsRequired = true };

        // Value option
        var valueOption = new Option<string?>(
            "--value",
            "Value for the action (text, number, true/false, date)")
        { IsRequired = false };

        // Index option
        var indexOption = new Option<int?>(
            "--index",
            "Index for selection actions")
        { IsRequired = false };

        // Process ID option
        var processIdOption = new Option<int>(
            "--process-id",
            "Process ID to attach to")
        { IsRequired = true };

        this.AddOption(elementOption);
        this.AddOption(controlTypeOption);
        this.AddOption(actionOption);
        this.AddOption(valueOption);
        this.AddOption(indexOption);
        this.AddOption(processIdOption);

        this.SetHandler(async (element, controlType, action, value, index, processId) =>
        {
            // Start a mini-session for individual command
            var sessionId = await this.captureService.StartSessionAsync($"INTERACT_{controlType.ToUpper()}_{DateTime.Now:HHmmss}");

            try
            {
                await this.captureService.LogInteractionAsync($"INTERACT command started: {action} on {controlType} element {element} (Process: {processId})");

                Console.WriteLine($"Attaching to process {processId}...");
                var attachSuccess = await automationService.AttachToProcessAsync(processId);

                if (!attachSuccess)
                {
                    Console.WriteLine("Failed to attach to process.");
                    await this.captureService.LogInteractionAsync("ERROR: Failed to attach to process", Microsoft.Extensions.Logging.LogLevel.Error);
                    return;
                }

                var targetElement = await automationService.FindElementAsync(element);
                if (targetElement == null)
                {
                    Console.WriteLine($"Error: Element not found with selector '{element}'");
                    return;
                }

                bool success = false;

                try
                {
                    success = controlType.ToLower() switch
                    {
                        "combobox" => await HandleComboBoxAsync(automationService, targetElement, action, value, index),
                        "listbox" => await HandleListBoxAsync(automationService, targetElement, action, value, index),
                        "checkbox" => await HandleCheckBoxAsync(automationService, targetElement, action, value),
                        "radiobutton" => await HandleRadioButtonAsync(automationService, targetElement, action, value),
                        "slider" => await HandleSliderAsync(automationService, targetElement, action, value),
                        "tab" => await HandleTabAsync(automationService, targetElement, action, value, index),
                        "tree" => await HandleTreeAsync(automationService, targetElement, action, value),
                        "datepicker" => await HandleDatePickerAsync(automationService, targetElement, action, value),
                        "calendar" => await HandleCalendarAsync(automationService, targetElement, action, value),
                        "expander" => await HandleExpanderAsync(automationService, targetElement, action),
                        "datagrid" => await HandleDataGridAsync(automationService, targetElement, action, value, index),
                        "menu" => await HandleMenuAsync(automationService, targetElement, action),
                        "togglebutton" => await HandleToggleButtonAsync(automationService, targetElement, action, value),
                        _ => throw new ArgumentException($"Unsupported control type: {controlType}"),
                    };

                    if (success)
                    {
                        Console.WriteLine($"✓ Action '{action}' completed successfully on {controlType}");
                        await this.captureService.LogInteractionAsync($"INTERACT command completed successfully: {action} on {controlType}");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Action '{action}' failed on {controlType}");
                        await this.captureService.LogInteractionAsync($"INTERACT command failed: {action} on {controlType}", Microsoft.Extensions.Logging.LogLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error performing {action} on {controlType}");
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    await this.captureService.LogInteractionAsync($"ERROR: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Error);
                }
                finally
                {
                    await this.captureService.EndSessionAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in INTERACT command: {ex.Message}");
                await this.captureService.LogInteractionAsync($"ERROR: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Error);
            }
            finally
            {
                await this.captureService.EndSessionAsync();
            }
        }, elementOption, controlTypeOption, actionOption, valueOption, indexOption, processIdOption);
    }

    private static async Task<bool> HandleComboBoxAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value, int? index)
    {
        return action.ToLower() switch
        {
            "select" when value != null => await automation.SelectComboBoxItemAsync(element, value),
            "select" when index.HasValue => await automation.SelectComboBoxItemByIndexAsync(element, index.Value),
            "get-items" => await DisplayComboBoxItems(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleListBoxAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value, int? index)
    {
        return action.ToLower() switch
        {
            "select" when value != null => await automation.SelectListBoxItemAsync(element, value),
            "select" when index.HasValue => await automation.SelectListBoxItemByIndexAsync(element, index.Value),
            "get-items" => await DisplayListBoxItems(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleCheckBoxAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "set" when bool.TryParse(value, out bool checkValue) => await automation.SetCheckBoxAsync(element, checkValue),
            "toggle" => await automation.SetCheckBoxAsync(element, !await automation.GetCheckBoxStateAsync(element)),
            "get" => await DisplayCheckBoxState(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleRadioButtonAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "select" => await automation.SetRadioButtonAsync(element, true),
            "get" => await DisplayRadioButtonState(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleSliderAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "set" when double.TryParse(value, out double sliderValue) => await automation.SetSliderValueAsync(element, sliderValue),
            "get" => await DisplaySliderValue(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleTabAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value, int? index)
    {
        return action.ToLower() switch
        {
            "select" when value != null => await automation.SelectTabAsync(element, value),
            "select" when index.HasValue => await automation.SelectTabByIndexAsync(element, index.Value),
            "get" => await DisplaySelectedTab(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleTreeAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "expand" => await automation.ExpandTreeNodeAsync(element),
            "collapse" => await automation.CollapseTreeNodeAsync(element),
            "select" when value != null => await automation.SelectTreeNodeAsync(element, value),
            _ => false,
        };
    }

    private static async Task<bool> HandleDatePickerAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "set" when DateTime.TryParse(value, out DateTime date) => await automation.SetDatePickerAsync(element, date),
            "get" => await DisplayDatePickerValue(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleCalendarAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "select" when DateTime.TryParse(value, out DateTime date) => await automation.SetCalendarDateAsync(element, date),
            "get" => await DisplayCalendarValue(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleExpanderAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action)
    {
        return action.ToLower() switch
        {
            "toggle" => await automation.ToggleExpanderAsync(element),
            "get" => await DisplayExpanderState(automation, element),
            _ => false,
        };
    }

    private static async Task<bool> HandleDataGridAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value, int? index)
    {
        return action.ToLower() switch
        {
            "select-row" when index.HasValue => await automation.SelectDataGridRowAsync(element, index.Value),
            "get-data" => await DisplayDataGridData(automation, element),
            "set-checkbox" when index.HasValue && bool.TryParse(value, out bool checkValue) =>
                await automation.SetDataGridCheckboxAsync($"id={element.Properties.AutomationId.ValueOrDefault ?? "TestDataGrid"}", index.Value, checkValue),
            "set-checkbox-by-name" when !string.IsNullOrEmpty(value) =>
                await HandleDataGridCheckboxByName(automation, element, value),
            "toggle-checkbox" when index.HasValue =>
                await automation.ToggleDataGridCheckboxAsync($"id={element.Properties.AutomationId.ValueOrDefault ?? "TestDataGrid"}", index.Value),
            "get-checkbox-states" => await DisplayDataGridCheckboxStates(automation, element),
            "uncheck-all" => await HandleUncheckAllDataGridCheckboxes(automation, element),
            _ => throw new ArgumentException($"Unknown DataGrid action: {action}. Available actions: select-row, get-data, set-checkbox, set-checkbox-by-name, toggle-checkbox, get-checkbox-states, uncheck-all"),
        };
    }

    private static async Task<bool> HandleMenuAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action)
    {
        return action.ToLower() switch
        {
            "invoke" => await automation.InvokeMenuItemAsync(element),
            _ => false,
        };
    }

    private static async Task<bool> HandleToggleButtonAsync(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string action, string? value)
    {
        return action.ToLower() switch
        {
            "set" when bool.TryParse(value, out bool toggleValue) => await automation.SetToggleButtonAsync(element, toggleValue),
            "toggle" => await automation.SetToggleButtonAsync(element, !await automation.GetToggleButtonStateAsync(element)),
            "get" => await DisplayToggleButtonState(automation, element),
            _ => false,
        };
    }

    // Display helper methods
    private static async Task<bool> DisplayComboBoxItems(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var items = await automation.GetComboBoxItemsAsync(element);
        Console.WriteLine($"ComboBox items ({items.Length}):");
        for (int i = 0; i < items.Length; i++)
        {
            Console.WriteLine($"  [{i}] {items[i]}");
        }

        return true;
    }

    private static async Task<bool> DisplayListBoxItems(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var items = await automation.GetListBoxItemsAsync(element);
        Console.WriteLine($"ListBox items ({items.Length}):");
        for (int i = 0; i < items.Length; i++)
        {
            Console.WriteLine($"  [{i}] {items[i]}");
        }

        return true;
    }

    private static async Task<bool> DisplayCheckBoxState(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var state = await automation.GetCheckBoxStateAsync(element);
        Console.WriteLine($"CheckBox state: {(state ? "Checked" : "Unchecked")}");
        return true;
    }

    private static async Task<bool> DisplayRadioButtonState(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var state = await automation.GetRadioButtonStateAsync(element);
        Console.WriteLine($"RadioButton state: {(state ? "Selected" : "Not Selected")}");
        return true;
    }

    private static async Task<bool> DisplaySliderValue(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var value = await automation.GetSliderValueAsync(element);
        Console.WriteLine($"Slider value: {value}");
        return true;
    }

    private static async Task<bool> DisplaySelectedTab(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var tab = await automation.GetSelectedTabAsync(element);
        Console.WriteLine($"Selected tab: {tab}");
        return true;
    }

    private static async Task<bool> DisplayDatePickerValue(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var date = await automation.GetDatePickerValueAsync(element);
        Console.WriteLine($"DatePicker value: {(date?.ToString("yyyy-MM-dd") ?? "No date selected")}");
        return true;
    }

    private static async Task<bool> DisplayCalendarValue(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var date = await automation.GetCalendarDateAsync(element);
        Console.WriteLine($"Calendar date: {(date?.ToString("yyyy-MM-dd") ?? "No date selected")}");
        return true;
    }

    private static async Task<bool> DisplayExpanderState(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var state = await automation.GetExpanderStateAsync(element);
        Console.WriteLine($"Expander state: {(state ? "Expanded" : "Collapsed")}");
        return true;
    }

    private static async Task<bool> DisplayDataGridData(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var data = await automation.GetDataGridDataAsync(element);
        Console.WriteLine($"DataGrid data ({data.Length} rows):");
        for (int i = 0; i < data.Length; i++)
        {
            Console.WriteLine($"  Row {i}:");
            foreach (var kvp in data[i])
            {
                Console.WriteLine($"    {kvp.Key}: {kvp.Value}");
            }
        }

        return true;
    }

    private static async Task<bool> DisplayToggleButtonState(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var state = await automation.GetToggleButtonStateAsync(element);
        Console.WriteLine($"ToggleButton state: {(state ? "Toggled" : "Not Toggled")}");
        return true;
    }

    private static async Task<bool> HandleDataGridCheckboxByName(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element, string nameAndState)
    {
        // Expected format: "John Doe:false" or "John Doe:true"
        var parts = nameAndState.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !bool.TryParse(parts[1], out bool checkValue))
        {
            Console.WriteLine("Invalid format. Use: 'RowName:true' or 'RowName:false'");
            return false;
        }

        var rowName = parts[0].Trim();
        var elementId = $"id={element.Properties.AutomationId.ValueOrDefault ?? "TestDataGrid"}";

        Console.WriteLine($"Setting checkbox for row '{rowName}' to {checkValue}");
        var result = await automation.SetDataGridCheckboxByNameAsync(elementId, rowName, checkValue);

        if (result)
        {
            Console.WriteLine($"✅ Successfully set checkbox for '{rowName}' to {checkValue}");
        }
        else
        {
            Console.WriteLine($"❌ Failed to set checkbox for '{rowName}'");
        }

        return result;
    }

    private static async Task<bool> DisplayDataGridCheckboxStates(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var automationId = element.Properties.AutomationId.ValueOrDefault ?? "TestDataGrid";
        var elementId = $"id={automationId}";
        var states = await automation.GetDataGridCheckboxStatesAsync(elementId);

        Console.WriteLine($"DataGrid checkbox states ({states.Length} rows):");
        var rowNames = new[] { "John Doe", "Jane Smith", "Bob Johnson", "Alice Brown" };

        for (int i = 0; i < states.Length && i < rowNames.Length; i++)
        {
            Console.WriteLine($"Row {i} ({rowNames[i]}): {(states[i] ? "Checked" : "Unchecked")}");
        }

        return true;
    }

    private static async Task<bool> HandleUncheckAllDataGridCheckboxes(IAutomationService automation, FlaUI.Core.AutomationElements.AutomationElement element)
    {
        var elementId = $"id={element.Properties.AutomationId.ValueOrDefault ?? "TestDataGrid"}";
        Console.WriteLine("Unchecking all DataGrid checkboxes...");

        // Get current states first
        var states = await automation.GetDataGridCheckboxStatesAsync(elementId);
        var rowNames = new[] { "John Doe", "Jane Smith", "Bob Johnson", "Alice Brown" };

        bool allSuccessful = true;
        int changedCount = 0;

        for (int i = 0; i < states.Length && i < rowNames.Length; i++)
        {
            if (states[i]) // Only uncheck if currently checked
            {
                Console.WriteLine($"Unchecking row {i} ({rowNames[i]})...");
                var result = await automation.SetDataGridCheckboxAsync(elementId, i, false);

                if (result)
                {
                    Console.WriteLine($"✅ Successfully unchecked {rowNames[i]}");
                    changedCount++;
                }
                else
                {
                    Console.WriteLine($"❌ Failed to uncheck {rowNames[i]}");
                    allSuccessful = false;
                }

                // Small delay between operations
                await Task.Delay(200);
            }
            else
            {
                Console.WriteLine($"Row {i} ({rowNames[i]}) already unchecked");
            }
        }

        Console.WriteLine($"Uncheck operation completed. Changed {changedCount} checkboxes.");
        return allSuccessful;
    }
}

// Copyright (c) CLIF - Comprehensive UI Automation CLI. All rights reserved.
// Licensed under the MIT License.
