using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using Microsoft.Extensions.Logging;

namespace CLIF.Services;

public interface IDataGridAutomationService
{
    Task<bool> SetDataGridCheckboxAsync(AutomationElement dataGrid, int rowIndex, bool isChecked);
    Task<bool> SetDataGridCheckboxByNameAsync(AutomationElement dataGrid, string rowName, bool isChecked);
    Task<bool> ToggleDataGridCheckboxAsync(AutomationElement dataGrid, int rowIndex);
    Task<bool[]> GetDataGridCheckboxStatesAsync(AutomationElement dataGrid);
    Task<Dictionary<string, object>[]> GetDataGridDataAsync(AutomationElement element);
    Task<bool> SelectDataGridRowAsync(AutomationElement element, int rowIndex);
    Task<bool> SelectDataGridCellAsync(AutomationElement element, int rowIndex, int columnIndex);
}

public class DataGridAutomationService : IDataGridAutomationService
{
    private readonly ILogger<DataGridAutomationService> _logger;

    public DataGridAutomationService(ILogger<DataGridAutomationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SetDataGridCheckboxAsync(AutomationElement dataGrid, int rowIndex, bool isChecked)
    {
        return await Task.Run(async () =>
        {
            try
            {
                // Get all data rows (excluding NewItemPlaceholder)
                var dataRows = dataGrid.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.DataItem)
                    .And(cf.ByName("TestWpfApp.SampleData")));

                if (rowIndex >= dataRows.Length)
                {
                    _logger.LogWarning($"Row index {rowIndex} out of range. Found {dataRows.Length} rows.");
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
                    _logger.LogWarning($"Checkbox cell not found in row {rowIndex}");
                    return false;
                }

                var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));

                if (checkbox != null)
                {
                    var checkboxElement = checkbox.AsCheckBox();
                    var currentState = checkboxElement.IsChecked ?? false;

                    _logger.LogInformation($"Row {rowIndex} checkbox current state: {currentState}, target state: {isChecked}");

                    if (currentState != isChecked)
                    {
                        checkboxElement.Toggle();
                        _logger.LogInformation($"Toggled checkbox in row {rowIndex} from {currentState} to {isChecked}");

                        // Verify the change - remove await from lambda
                        await Task.Delay(100);
                        var newState = checkboxElement.IsChecked ?? false;
                        _logger.LogInformation($"Verified checkbox state in row {rowIndex}: {newState}");

                        return newState == isChecked;
                    }
                    else
                    {
                        _logger.LogInformation($"Checkbox in row {rowIndex} already in desired state: {isChecked}");
                        return true;
                    }
                }
                else
                {
                    _logger.LogWarning($"Checkbox not found in row {rowIndex} cell");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set DataGrid checkbox at row {rowIndex}");
                return false;
            }
        });
    }

    public async Task<bool> SetDataGridCheckboxByNameAsync(AutomationElement dataGrid, string rowName, bool isChecked)
    {
        return await Task.Run(async () =>
        {
            try
            {
                // Find row by name cell content
                var nameCell = dataGrid.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.Custom)
                    .And(cf.ByName(rowName)));

                if (nameCell == null)
                {
                    _logger.LogWarning($"Row with name '{rowName}' not found");
                    return false;
                }

                // Get the parent row
                var row = nameCell.Parent;
                if (row == null) return false;

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

                if (checkboxCell == null) return false;

                var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));

                if (checkbox != null)
                {
                    var checkboxElement = checkbox.AsCheckBox();
                    var currentState = checkboxElement.IsChecked ?? false;
                    if (currentState != isChecked)
                    {
                        checkboxElement.Toggle();
                        _logger.LogInformation($"Toggled checkbox for row '{rowName}' to {isChecked}");
                    }
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to set DataGrid checkbox for row '{rowName}'");
                return false;
            }
        });
    }

    public async Task<bool> ToggleDataGridCheckboxAsync(AutomationElement dataGrid, int rowIndex)
    {
        return await Task.Run(async () =>
        {
            try
            {
                var dataRows = dataGrid.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.DataItem)
                    .And(cf.ByName("TestWpfApp.SampleData")));

                if (rowIndex >= dataRows.Length) return false;

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

                if (checkboxCell == null) return false;

                var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));

                if (checkbox != null)
                {
                    var checkboxElement = checkbox.AsCheckBox();
                    var currentState = checkboxElement.IsChecked ?? false;
                    checkboxElement.Toggle();
                    _logger.LogInformation($"Toggled checkbox in row {rowIndex} from {currentState} to {!currentState}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to toggle DataGrid checkbox at row {rowIndex}");
                return false;
            }
        });
    }

    public async Task<bool[]> GetDataGridCheckboxStatesAsync(AutomationElement dataGrid)
    {
        return await Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation($"DataGrid found: {dataGrid.Name}, ControlType: {dataGrid.ControlType}");

                // Try to find all DataItem descendants without the name filter first
                var allDataItems = dataGrid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem));
                _logger.LogInformation($"Found {allDataItems.Length} DataItem descendants");

                // Log the names of all DataItems
                for (int i = 0; i < allDataItems.Length; i++)
                {
                    _logger.LogInformation($"DataItem {i}: Name='{allDataItems[i].Name}', ClassName='{allDataItems[i].ClassName}'");
                }

                // Now try the original filter
                var dataRows = dataGrid.FindAllDescendants(cf =>
                    cf.ByControlType(ControlType.DataItem)
                    .And(cf.ByName("TestWpfApp.SampleData")));

                _logger.LogInformation($"Found {dataRows.Length} filtered DataRows with name 'TestWpfApp.SampleData'");

                // If the filtered search didn't work, try excluding the NewItemPlaceholder
                if (dataRows.Length == 0 && allDataItems.Length > 0)
                {
                    dataRows = allDataItems.Where(item => !item.Name.Contains("NewItemPlaceholder")).ToArray();
                    _logger.LogInformation($"Using fallback: Found {dataRows.Length} DataRows excluding NewItemPlaceholder");
                }

                var states = new List<bool>();

                foreach (var row in dataRows)
                {
                    _logger.LogInformation($"Processing row: {row.Name}");

                    // Find the checkbox cell by looking for cells that contain checkboxes
                    AutomationElement? checkboxCell = null;
                    var cells = row.FindAllDescendants(cf => cf.ByControlType(ControlType.Custom));
                    _logger.LogInformation($"Found {cells.Length} custom cells in row: {row.Name}");

                    foreach (var cell in cells)
                    {
                        _logger.LogInformation($"Checking cell: '{cell.Name}' for checkbox");
                        var checkbox = cell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                        if (checkbox != null)
                        {
                            _logger.LogInformation($"Found checkbox in cell: '{cell.Name}'");
                            checkboxCell = cell;
                            break;
                        }
                    }

                    if (checkboxCell != null)
                    {
                        _logger.LogInformation($"Found checkbox cell in row: {row.Name}");

                        var checkbox = checkboxCell.FindFirstDescendant(cf => cf.ByControlType(ControlType.CheckBox));
                        if (checkbox != null)
                        {
                            var isChecked = checkbox.AsCheckBox().IsChecked ?? false;
                            _logger.LogInformation($"Checkbox in row '{row.Name}' is {isChecked}");
                            states.Add(isChecked);
                        }
                        else
                        {
                            _logger.LogWarning($"Checkbox not found in cell for row: {row.Name}");
                            states.Add(false);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Checkbox cell not found for row: {row.Name}");
                        states.Add(false);
                    }
                }

                _logger.LogInformation($"Returning {states.Count} checkbox states");
                return states.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get DataGrid checkbox states");
                return new bool[0];
            }
        });
    }

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
                _logger.LogError(ex, "Error getting DataGrid data");
                return Array.Empty<Dictionary<string, object>>();
            }
        });
    }

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
                _logger.LogError(ex, $"Error selecting DataGrid row: {rowIndex}");
                return false;
            }
        });
    }

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
                _logger.LogError(ex, $"Error selecting DataGrid cell: {rowIndex}, {columnIndex}");
                return false;
            }
        });
    }
}
