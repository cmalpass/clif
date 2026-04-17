// Licensed under the MIT License.

using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using CLIF.Mcp.Core;

namespace CLIF.Mcp.Tools;

/// <summary>
/// Consolidated MCP tool for advanced WPF control interactions.
/// Supports ComboBox, DataGrid, TreeView, TabControl, Slider, Menu, ToggleButton, Expander, and more.
/// </summary>
public class InteractTool : ToolBase
{
    private readonly ElementRegistry _elementRegistry;

    public InteractTool(ElementRegistry elementRegistry)
    {
        _elementRegistry = elementRegistry;
    }

    public override string Name => "clif_interact";

    public override string Description =>
        "Perform advanced interactions with WPF controls. Supports ComboBox, DataGrid, TreeView, " +
        "TabControl, Slider, Menu, ToggleButton, CheckBox, RadioButton, Expander, ListBox, and DatePicker. " +
        "Use controlType and action to specify the interaction.";

    public override object InputSchema => new
    {
        type = "object",
        properties = new
        {
            @ref = new
            {
                type = "string",
                description = "Element ref from clif_snapshot",
            },
            controlType = new
            {
                type = "string",
                @enum = new[]
                {
                    "combobox", "listbox", "datagrid", "tree", "tabcontrol",
                    "slider", "menu", "togglebutton", "checkbox", "radiobutton",
                    "expander", "datepicker",
                },
                description = "The type of WPF control to interact with",
            },
            action = new
            {
                type = "string",
                @enum = new[]
                {
                    "select", "list_items", "get_value", "set_value",
                    "expand", "collapse", "toggle", "check", "uncheck",
                    "select_row", "select_cell", "get_data", "invoke",
                },
                description = "The action to perform on the control",
            },
            value = new
            {
                type = "string",
                description = "Value for the action (e.g., item text, tab name, slider value)",
            },
            index = new
            {
                type = "integer",
                description = "Index for index-based operations (e.g., row index, tab index)",
            },
            column = new
            {
                type = "integer",
                description = "Column index for DataGrid cell operations",
            },
        },
        required = new[] { "ref", "controlType", "action" },
    };

    public override Task<McpToolResult> ExecuteAsync(JsonElement? arguments)
    {
        var refId = GetStringArgument(arguments, "ref");
        var controlType = GetStringArgument(arguments, "controlType");
        var action = GetStringArgument(arguments, "action");
        var value = GetStringArgument(arguments, "value");
        var index = GetIntArgument(arguments, "index", -1);
        var column = GetIntArgument(arguments, "column", -1);

        if (string.IsNullOrEmpty(refId) || string.IsNullOrEmpty(controlType) || string.IsNullOrEmpty(action))
        {
            return Task.FromResult(ErrorResult("Missing required arguments: ref, controlType, action"));
        }

        var element = _elementRegistry.GetElement(refId);
        if (element == null)
        {
            return Task.FromResult(ErrorResult(
                $"Element not found: {refId}. Run clif_snapshot to refresh element refs."));
        }

        try
        {
            var result = controlType.ToLowerInvariant() switch
            {
                "combobox" => HandleComboBox(element, action, value, index),
                "listbox" => HandleListBox(element, action, value, index),
                "datagrid" => HandleDataGrid(element, action, value, index, column),
                "tree" => HandleTreeView(element, action, value),
                "tabcontrol" => HandleTabControl(element, action, value, index),
                "slider" => HandleSlider(element, action, value),
                "menu" => HandleMenu(element, action),
                "togglebutton" => HandleToggle(element, action, value),
                "checkbox" => HandleCheckBox(element, action, value),
                "radiobutton" => HandleRadioButton(element, action),
                "expander" => HandleExpander(element, action),
                "datepicker" => HandleDatePicker(element, action, value),
                _ => $"Unknown control type: {controlType}",
            };

            return Task.FromResult(TextResult(result));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ErrorResult(
                $"Failed to interact with {controlType} ({refId}): {ex.Message}"));
        }
    }

    private static string HandleComboBox(AutomationElement element, string action, string? value, int index)
    {
        var comboBox = element.AsComboBox();
        return action.ToLowerInvariant() switch
        {
            "select" when !string.IsNullOrEmpty(value) => SelectComboBoxItem(comboBox, value),
            "select" when index >= 0 => SelectComboBoxByIndex(comboBox, index),
            "list_items" => ListComboBoxItems(comboBox),
            "get_value" => comboBox.SelectedItem?.Text ?? "(none)",
            _ => $"Unknown ComboBox action: {action}",
        };
    }

    private static string SelectComboBoxItem(ComboBox comboBox, string value)
    {
        comboBox.Expand();
        Thread.Sleep(100);
        var items = comboBox.Items;
        var item = items.FirstOrDefault(i => i.Text == value);
        if (item != null)
        {
            item.Select();
            comboBox.Collapse();
            return $"Selected \"{value}\"";
        }

        comboBox.Collapse();
        return $"Item not found: \"{value}\". Available: {string.Join(", ", items.Select(i => i.Text))}";
    }

    private static string SelectComboBoxByIndex(ComboBox comboBox, int index)
    {
        comboBox.Expand();
        Thread.Sleep(100);
        var items = comboBox.Items;
        if (index >= 0 && index < items.Length)
        {
            items[index].Select();
            comboBox.Collapse();
            return $"Selected item at index {index}: \"{items[index].Text}\"";
        }

        comboBox.Collapse();
        return $"Index {index} out of range (0-{items.Length - 1})";
    }

    private static string ListComboBoxItems(ComboBox comboBox)
    {
        comboBox.Expand();
        Thread.Sleep(100);
        var items = comboBox.Items.Select((item, i) => $"  {i}: \"{item.Text}\"");
        comboBox.Collapse();
        return $"Items:\n{string.Join("\n", items)}";
    }

    private static string HandleListBox(AutomationElement element, string action, string? value, int index)
    {
        var listBox = element.AsListBox();
        return action.ToLowerInvariant() switch
        {
            "select" when !string.IsNullOrEmpty(value) => SelectListBoxItem(listBox, value),
            "select" when index >= 0 => SelectListBoxByIndex(listBox, index),
            "list_items" => ListListBoxItems(listBox),
            "get_value" => listBox.SelectedItem?.Text ?? "(none)",
            _ => $"Unknown ListBox action: {action}",
        };
    }

    private static string SelectListBoxItem(ListBox listBox, string value)
    {
        var items = listBox.Items;
        var item = items.FirstOrDefault(i => i.Text == value);
        if (item != null)
        {
            item.Select();
            return $"Selected \"{value}\"";
        }

        return $"Item not found: \"{value}\"";
    }

    private static string SelectListBoxByIndex(ListBox listBox, int index)
    {
        var items = listBox.Items;
        if (index >= 0 && index < items.Length)
        {
            items[index].Select();
            return $"Selected item at index {index}";
        }

        return $"Index {index} out of range";
    }

    private static string ListListBoxItems(ListBox listBox)
    {
        var items = listBox.Items.Select((item, i) => $"  {i}: \"{item.Text}\"");
        return $"Items:\n{string.Join("\n", items)}";
    }

    private static string HandleDataGrid(AutomationElement element, string action, string? value, int index, int column)
    {
        var grid = element.AsDataGridView();
        return action.ToLowerInvariant() switch
        {
            "select_row" when index >= 0 => SelectDataGridRow(grid, index),
            "select_cell" when index >= 0 && column >= 0 => SelectDataGridCell(grid, index, column),
            "get_data" => GetDataGridData(grid),
            "get_value" when index >= 0 && column >= 0 => GetDataGridCellValue(grid, index, column),
            _ => $"Unknown DataGrid action: {action}. Provide valid index/column.",
        };
    }

    private static string SelectDataGridRow(DataGridView grid, int index)
    {
        var rows = grid.Rows;
        if (index >= 0 && index < rows.Length)
        {
            if (rows[index].Patterns.ScrollItem.IsSupported)
            {
                rows[index].Patterns.ScrollItem.Pattern.ScrollIntoView();
            }

            rows[index].Click();
            return $"Selected row {index}";
        }

        return $"Row index {index} out of range (0-{rows.Length - 1})";
    }

    private static string SelectDataGridCell(DataGridView grid, int row, int col)
    {
        var rows = grid.Rows;
        if (row >= 0 && row < rows.Length)
        {
            var cells = rows[row].Cells;
            if (col >= 0 && col < cells.Length)
            {
                cells[col].Click();
                return $"Selected cell [{row}, {col}]";
            }

            return $"Column {col} out of range (0-{cells.Length - 1})";
        }

        return $"Row {row} out of range (0-{rows.Length - 1})";
    }

    private static string GetDataGridData(DataGridView grid)
    {
        var rows = grid.Rows;
        var lines = new List<string>();
        for (int r = 0; r < Math.Min(rows.Length, 50); r++)
        {
            var cells = rows[r].Cells;
            var cellTexts = cells.Select(c => c.Value ?? c.Name ?? string.Empty);
            lines.Add($"  Row {r}: [{string.Join(", ", cellTexts)}]");
        }

        if (rows.Length > 50)
        {
            lines.Add($"  ... ({rows.Length - 50} more rows)");
        }

        return $"DataGrid ({rows.Length} rows):\n{string.Join("\n", lines)}";
    }

    private static string GetDataGridCellValue(DataGridView grid, int row, int col)
    {
        var rows = grid.Rows;
        if (row >= 0 && row < rows.Length)
        {
            var cells = rows[row].Cells;
            if (col >= 0 && col < cells.Length)
            {
                return cells[col].Value ?? cells[col].Name ?? string.Empty;
            }
        }

        return "Cell not found";
    }

    private static string HandleTreeView(AutomationElement element, string action, string? value)
    {
        var tree = element.AsTree();
        return action.ToLowerInvariant() switch
        {
            "expand" when !string.IsNullOrEmpty(value) => ExpandTreeNode(tree, value),
            "collapse" when !string.IsNullOrEmpty(value) => CollapseTreeNode(tree, value),
            "select" when !string.IsNullOrEmpty(value) => SelectTreeNode(tree, value),
            "expand" => ExpandElement(element),
            "collapse" => CollapseElement(element),
            _ => $"Unknown TreeView action: {action}",
        };
    }

    private static string ExpandTreeNode(Tree tree, string nodeName)
    {
        var item = FindTreeItem(tree, nodeName);
        if (item != null)
        {
            item.Expand();
            return $"Expanded \"{nodeName}\"";
        }

        return $"Tree node not found: \"{nodeName}\"";
    }

    private static string CollapseTreeNode(Tree tree, string nodeName)
    {
        var item = FindTreeItem(tree, nodeName);
        if (item != null)
        {
            item.Collapse();
            return $"Collapsed \"{nodeName}\"";
        }

        return $"Tree node not found: \"{nodeName}\"";
    }

    private static string SelectTreeNode(Tree tree, string nodeName)
    {
        var item = FindTreeItem(tree, nodeName);
        if (item != null)
        {
            item.Select();
            return $"Selected \"{nodeName}\"";
        }

        return $"Tree node not found: \"{nodeName}\"";
    }

    private static TreeItem? FindTreeItem(Tree tree, string name)
    {
        foreach (var item in tree.Items)
        {
            if (item.Text == name) return item;
            var found = FindTreeItemRecursive(item, name);
            if (found != null) return found;
        }

        return null;
    }

    private static TreeItem? FindTreeItemRecursive(TreeItem parent, string name)
    {
        try
        {
            parent.Expand();
            Thread.Sleep(50);
        }
        catch
        {
            // May not be expandable
        }

        foreach (var child in parent.Items)
        {
            if (child.Text == name) return child;
            var found = FindTreeItemRecursive(child, name);
            if (found != null) return found;
        }

        return null;
    }

    private static string ExpandElement(AutomationElement element)
    {
        if (element.Patterns.ExpandCollapse.IsSupported)
        {
            element.Patterns.ExpandCollapse.Pattern.Expand();
            return "Expanded";
        }

        return "Element does not support expand/collapse";
    }

    private static string CollapseElement(AutomationElement element)
    {
        if (element.Patterns.ExpandCollapse.IsSupported)
        {
            element.Patterns.ExpandCollapse.Pattern.Collapse();
            return "Collapsed";
        }

        return "Element does not support expand/collapse";
    }

    private static string HandleTabControl(AutomationElement element, string action, string? value, int index)
    {
        var tab = element.AsTab();
        return action.ToLowerInvariant() switch
        {
            "select" when !string.IsNullOrEmpty(value) => SelectTabByName(tab, value),
            "select" when index >= 0 => SelectTabByIndex(tab, index),
            "get_value" => tab.SelectedTabItem?.Name ?? "(none)",
            "list_items" => ListTabItems(tab),
            _ => $"Unknown TabControl action: {action}",
        };
    }

    private static string SelectTabByName(Tab tab, string name)
    {
        var items = tab.TabItems;
        var item = items.FirstOrDefault(t => t.Name == name);
        if (item != null)
        {
            item.Select();
            return $"Selected tab \"{name}\"";
        }

        return $"Tab not found: \"{name}\". Available: {string.Join(", ", items.Select(t => t.Name))}";
    }

    private static string SelectTabByIndex(Tab tab, int index)
    {
        var items = tab.TabItems;
        if (index >= 0 && index < items.Length)
        {
            items[index].Select();
            return $"Selected tab at index {index}: \"{items[index].Name}\"";
        }

        return $"Tab index {index} out of range (0-{items.Length - 1})";
    }

    private static string ListTabItems(Tab tab)
    {
        var items = tab.TabItems.Select((t, i) =>
        {
            var selected = t.IsSelected ? " [selected]" : string.Empty;
            return $"  {i}: \"{t.Name}\"{selected}";
        });
        return $"Tabs:\n{string.Join("\n", items)}";
    }

    private static string HandleSlider(AutomationElement element, string action, string? value)
    {
        var slider = element.AsSlider();
        return action.ToLowerInvariant() switch
        {
            "set_value" when double.TryParse(value, out var v) => SetSliderValue(slider, v),
            "get_value" => $"{slider.Value}",
            _ => $"Unknown Slider action: {action}",
        };
    }

    private static string SetSliderValue(Slider slider, double value)
    {
        slider.Value = value;
        return $"Set slider to {value}";
    }

    private static string HandleMenu(AutomationElement element, string action)
    {
        return action.ToLowerInvariant() switch
        {
            "invoke" => InvokeMenuItem(element),
            _ => $"Unknown Menu action: {action}",
        };
    }

    private static string InvokeMenuItem(AutomationElement element)
    {
        if (element.Patterns.Invoke.IsSupported)
        {
            element.Patterns.Invoke.Pattern.Invoke();
            return $"Invoked menu item: {element.Name}";
        }

        element.Click();
        return $"Clicked menu item: {element.Name}";
    }

    private static string HandleToggle(AutomationElement element, string action, string? value)
    {
        return action.ToLowerInvariant() switch
        {
            "toggle" => ToggleElement(element),
            "get_value" => GetToggleState(element),
            "check" => SetToggleState(element, true),
            "uncheck" => SetToggleState(element, false),
            _ => $"Unknown ToggleButton action: {action}",
        };
    }

    private static string HandleCheckBox(AutomationElement element, string action, string? value)
    {
        return action.ToLowerInvariant() switch
        {
            "check" => SetToggleState(element, true),
            "uncheck" => SetToggleState(element, false),
            "toggle" => ToggleElement(element),
            "get_value" => GetToggleState(element),
            _ => $"Unknown CheckBox action: {action}",
        };
    }

    private static string HandleRadioButton(AutomationElement element, string action)
    {
        return action.ToLowerInvariant() switch
        {
            "select" => SelectRadioButton(element),
            "get_value" => GetSelectionState(element),
            _ => $"Unknown RadioButton action: {action}",
        };
    }

    private static string SelectRadioButton(AutomationElement element)
    {
        if (element.Patterns.SelectionItem.IsSupported)
        {
            element.Patterns.SelectionItem.Pattern.Select();
            return $"Selected radio button: {element.Name}";
        }

        element.Click();
        return $"Clicked radio button: {element.Name}";
    }

    private static string GetSelectionState(AutomationElement element)
    {
        if (element.Patterns.SelectionItem.IsSupported)
        {
            return element.Patterns.SelectionItem.Pattern.IsSelected.ValueOrDefault
                ? "selected"
                : "not selected";
        }

        return "unknown";
    }

    private static string ToggleElement(AutomationElement element)
    {
        if (element.Patterns.Toggle.IsSupported)
        {
            element.Patterns.Toggle.Pattern.Toggle();
            var newState = element.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault;
            return $"Toggled to {newState}";
        }

        return "Element does not support Toggle pattern";
    }

    private static string GetToggleState(AutomationElement element)
    {
        if (element.Patterns.Toggle.IsSupported)
        {
            return element.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault.ToString();
        }

        return "unknown";
    }

    private static string SetToggleState(AutomationElement element, bool desired)
    {
        if (!element.Patterns.Toggle.IsSupported)
        {
            return "Element does not support Toggle pattern";
        }

        var current = element.Patterns.Toggle.Pattern.ToggleState.ValueOrDefault;
        var isOn = current == ToggleState.On;

        if (isOn == desired)
        {
            return $"Already {(desired ? "checked" : "unchecked")}";
        }

        element.Patterns.Toggle.Pattern.Toggle();
        return desired ? "Checked" : "Unchecked";
    }

    private static string HandleExpander(AutomationElement element, string action)
    {
        return action.ToLowerInvariant() switch
        {
            "expand" => ExpandElement(element),
            "collapse" => CollapseElement(element),
            "toggle" => ToggleExpander(element),
            "get_value" => GetExpanderState(element),
            _ => $"Unknown Expander action: {action}",
        };
    }

    private static string ToggleExpander(AutomationElement element)
    {
        if (element.Patterns.ExpandCollapse.IsSupported)
        {
            var state = element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.ValueOrDefault;
            if (state == ExpandCollapseState.Expanded)
            {
                element.Patterns.ExpandCollapse.Pattern.Collapse();
                return "Collapsed";
            }
            else
            {
                element.Patterns.ExpandCollapse.Pattern.Expand();
                return "Expanded";
            }
        }

        return "Element does not support expand/collapse";
    }

    private static string GetExpanderState(AutomationElement element)
    {
        if (element.Patterns.ExpandCollapse.IsSupported)
        {
            return element.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.ValueOrDefault.ToString();
        }

        return "unknown";
    }

    private static string HandleDatePicker(AutomationElement element, string action, string? value)
    {
        return action.ToLowerInvariant() switch
        {
            "set_value" when DateTime.TryParse(value, out var date) => SetDateValue(element, date),
            "get_value" => GetDateValue(element),
            _ => $"Unknown DatePicker action: {action}. Provide a valid date string for set_value.",
        };
    }

    private static string SetDateValue(AutomationElement element, DateTime date)
    {
        if (element.Patterns.Value.IsSupported)
        {
            element.Patterns.Value.Pattern.SetValue(date.ToString("yyyy-MM-dd"));
            return $"Set date to {date:yyyy-MM-dd}";
        }

        return "Element does not support Value pattern";
    }

    private static string GetDateValue(AutomationElement element)
    {
        if (element.Patterns.Value.IsSupported)
        {
            return element.Patterns.Value.Pattern.Value.ValueOrDefault ?? "(empty)";
        }

        return element.Name ?? "(unknown)";
    }
}
