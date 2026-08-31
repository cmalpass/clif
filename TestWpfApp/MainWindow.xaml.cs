using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace TestWpfApp;

public partial class MainWindow : Window
{
    private ObservableCollection<SampleData> _sampleData;
    private bool _isToggled = false;

    public MainWindow()
    {
        InitializeComponent();
        InitializeData();
        SetupDataGrid();
    }

    private void InitializeData()
    {
        _sampleData = new ObservableCollection<SampleData>
        {
            new SampleData { Id = 1, Name = "John Doe", Age = 30, IsActive = true },
            new SampleData { Id = 2, Name = "Jane Smith", Age = 25, IsActive = false },
            new SampleData { Id = 3, Name = "Bob Johnson", Age = 35, IsActive = true },
            new SampleData { Id = 4, Name = "Alice Brown", Age = 28, IsActive = true },
        };
    }

    private void SetupDataGrid()
    {
        TestDataGrid.ItemsSource = _sampleData;
    }

    // Basic Control Event Handlers
    private void TestButton_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Hello from Comprehensive WPF App!", "Button Click", MessageBoxButton.OK, MessageBoxImage.Information);
        UpdateStatus("Button clicked!");
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isToggled = !_isToggled;
        var button = sender as Button;
        button.Content = _isToggled ? "Toggled ON" : "Toggle Me";
        UpdateStatus($"Toggle button is {(_isToggled ? "ON" : "OFF")}");
    }

    private void TestComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestComboBox.SelectedItem is ComboBoxItem item)
        {
            UpdateStatus($"ComboBox selected: {item.Content}");
        }
    }

    private void TestListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestListBox.SelectedItem is ListBoxItem item)
        {
            UpdateStatus($"ListBox selected: {item.Content}");
        }
    }

    private void TestSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TestProgressBar != null)
        {
            TestProgressBar.Value = e.NewValue;
            UpdateStatus($"Slider value: {e.NewValue:F0}");
        }
    }

    // Advanced Control Event Handlers
    private void TestDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestDatePicker.SelectedDate.HasValue)
        {
            UpdateStatus($"Date selected: {TestDatePicker.SelectedDate.Value.ToShortDateString()}");
        }
    }

    private void TestCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestCalendar.SelectedDate.HasValue)
        {
            UpdateStatus($"Calendar date: {TestCalendar.SelectedDate.Value.ToShortDateString()}");
        }
    }

    private void TestExpander_Expanded(object sender, RoutedEventArgs e)
    {
        UpdateStatus("Expander opened");
    }

    private void TestTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestTabControl.SelectedItem is TabItem item)
        {
            UpdateStatus($"Tab selected: {item.Header}");
        }
    }

    // Data Control Event Handlers
    private void TestDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TestDataGrid.SelectedItem is SampleData item)
        {
            UpdateStatus($"DataGrid row selected: {item.Name}");
        }
    }

    private void TestTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem item)
        {
            UpdateStatus($"TreeView item selected: {item.Header}");
        }
    }

    private void TestRichTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateStatus("RichTextBox content changed");
    }

    // Menu and Toolbar Event Handlers
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        UpdateStatus($"Menu item clicked: {menuItem?.Header}");

        if (menuItem?.Header?.ToString() == "Exit")
        {
            this.Close();
        }
    }

    private void ToolBarButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        UpdateStatus($"ToolBar button clicked: {button?.Content}");
    }

    private void ToolBarToggle_Click(object sender, RoutedEventArgs e)
    {
        var toggle = sender as ToggleButton;
        UpdateStatus($"ToolBar toggle: {toggle?.Content} is {(toggle?.IsChecked == true ? "ON" : "OFF")}");
    }

    // Utility Methods
    private void UpdateStatus(string message)
    {
        StatusTextBlock.Text = $"Status: {message}";

        // Update status bar
        if (TestStatusBar?.Items.Count > 0 && TestStatusBar.Items[0] is StatusBarItem statusItem)
        {
            statusItem.Content = message;
        }
    }
}

// Sample data class for DataGrid
public class SampleData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
}