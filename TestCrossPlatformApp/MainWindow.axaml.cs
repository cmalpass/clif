using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Text;

namespace TestCrossPlatformApp;

public partial class MainWindow : Window
{
    private static readonly DateTimeOffset DefaultDate = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);
    private readonly StringBuilder _actionLog = new();
    private int _actionCount;

    private TextBlock StatusControl => this.FindControl<TextBlock>("StatusTextBlock")!;
    private TextBlock ActionCountControl => this.FindControl<TextBlock>("ActionCountText")!;
    private TextBox ActionLogControl => this.FindControl<TextBox>("ActionLogTextBox")!;
    private Slider SliderControl => this.FindControl<Slider>("TestSlider")!;
    private ProgressBar ProgressControl => this.FindControl<ProgressBar>("TestProgressBar")!;
    private TextBlock SliderValueControl => this.FindControl<TextBlock>("SliderValueText")!;
    private ToggleButton ToggleControl => this.FindControl<ToggleButton>("ToggleButton")!;
    private CheckBox CheckControl => this.FindControl<CheckBox>("TestCheckBox")!;
    private ComboBox ComboControl => this.FindControl<ComboBox>("TestComboBox")!;
    private ListBox ListControl => this.FindControl<ListBox>("TestListBox")!;
    private TabControl TabsControl => this.FindControl<TabControl>("TestTabControl")!;
    private DatePicker DateControl => this.FindControl<DatePicker>("TestDatePicker")!;
    private TextBlock SelectedDateControl => this.FindControl<TextBlock>("SelectedDateText")!;
    private ListBox DataTableControl => this.FindControl<ListBox>("TestDataTable")!;
    private TextBlock SelectedDataRowControl => this.FindControl<TextBlock>("SelectedDataRowText")!;
    private Expander ExpanderControl => this.FindControl<Expander>("TestExpander")!;

    public MainWindow()
    {
        InitializeComponent();

        // Avalonia's UIA provider does not reliably raise a Name-changed event
        // when a TextBlock's Text is changed. Give mutable verification fields an
        // explicit automation name and keep it synchronized with their visible text.
        SetText(StatusControl, StatusControl.Text);
        SetText(SelectedDateControl, SelectedDateControl.Text);
        SetText(SelectedDataRowControl, SelectedDataRowControl.Text);

        SliderControl.PropertyChanged += (_, args) =>
        {
            if (args.Property == RangeBase.ValueProperty)
            {
                var value = SliderControl.Value;
                SliderValueControl.Text = value.ToString("0");
                ProgressControl.Value = value;
                SetStatus($"Slider value: {value:0}");
            }
        };

        ToggleControl.PropertyChanged += (_, args) =>
        {
            if (args.Property == ToggleButton.IsCheckedProperty)
            {
                SetStatus($"Toggle button is {(ToggleControl.IsChecked == true ? "ON" : "OFF")}");
            }
        };

        CheckControl.PropertyChanged += (_, args) =>
        {
            if (args.Property == ToggleButton.IsCheckedProperty)
            {
                SetStatus($"Check box is {(CheckControl.IsChecked == true ? "checked" : "unchecked")}");
            }
        };

        ComboControl.SelectionChanged += (_, _) =>
        {
            if (ComboControl.SelectedItem is ComboBoxItem item)
            {
                SetStatus($"Combo box selected: {item.Content}");
            }
        };

        ListControl.SelectionChanged += (_, _) =>
        {
            if (ListControl.SelectedItem is ListBoxItem item)
            {
                SetStatus($"List box selected: {item.Content}");
            }
        };

        TabsControl.SelectionChanged += (_, _) =>
        {
            if (TabsControl.SelectedItem is TabItem item)
            {
                SetStatus($"Tab selected: {item.Header}");
            }
        };

        DateControl.PropertyChanged += (_, args) =>
        {
            if (args.Property == DatePicker.SelectedDateProperty)
            {
                var selectedDate = DateControl.SelectedDate;
                SetText(SelectedDateControl, selectedDate?.ToString("yyyy-MM-dd") ?? "No date selected");
                SetStatus($"Date selected: {SelectedDateControl.Text}");
            }
        };

        DataTableControl.SelectionChanged += (_, _) =>
        {
            var customerName = DataTableControl.SelectedIndex switch
            {
                0 => "Alice Johnson",
                1 => "Bob Smith",
                2 => "Casey Lee",
                _ => "Unknown"
            };

            if (DataTableControl.SelectedIndex >= 0)
            {
                SetText(SelectedDataRowControl, $"Selected row: {customerName}");
                SetStatus(SelectedDataRowControl.Text);
            }
        };

        ExpanderControl.PropertyChanged += (_, args) =>
        {
            if (args.Property == Expander.IsExpandedProperty)
            {
                SetStatus($"Expander {(ExpanderControl.IsExpanded ? "opened" : "closed")}");
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void TestButtonClick(object? sender, RoutedEventArgs e)
    {
        SetStatus("Button clicked");
    }

    private void HiddenButtonClick(object? sender, RoutedEventArgs e)
    {
        SetStatus("Hidden button clicked");
    }

    private void ShowAbout(object? sender, RoutedEventArgs e)
    {
        SetStatus("CLIF cross-platform fixture v1");
    }

    private void ResetControls(object? sender, RoutedEventArgs e)
    {
        var textBox = this.FindControl<TextBox>("TestTextBox")!;
        var filledTextBox = this.FindControl<TextBox>("FilledTextBox")!;
        var radioButton1 = this.FindControl<RadioButton>("RadioButton1")!;
        var radioButton2 = this.FindControl<RadioButton>("RadioButton2")!;

        textBox.Text = string.Empty;
        filledTextBox.Text = "Initial value";
        ToggleControl.IsChecked = false;
        CheckControl.IsChecked = false;
        radioButton1.IsChecked = true;
        radioButton2.IsChecked = false;
        ComboControl.SelectedIndex = 0;
        ListControl.SelectedIndex = 0;
        SliderControl.Value = 25;
        ExpanderControl.IsExpanded = false;
        TabsControl.SelectedIndex = 0;
        DateControl.SelectedDate = DefaultDate;
        DataTableControl.SelectedIndex = 0;
        SetText(SelectedDateControl, DefaultDate.ToString("yyyy-MM-dd"));
        SetText(SelectedDataRowControl, "Selected row: Alice Johnson");
        SetStatus("Controls reset");
    }

    private void ExitApplication(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetStatus(string message)
    {
        _actionCount++;
        SetText(StatusControl, message);
        ActionCountControl.Text = $"Actions: {_actionCount}";
        _actionLog.AppendLine($"{_actionCount:000}: {message}");
        ActionLogControl.Text = _actionLog.ToString();
    }

    private static void SetText(TextBlock control, string text)
    {
        control.Text = text;
        AutomationProperties.SetName(control, text);
    }
}
