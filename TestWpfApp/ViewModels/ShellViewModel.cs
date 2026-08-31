using System.Collections.ObjectModel;
using System.Windows;
using Caliburn.Micro;

namespace TestWpfApp.ViewModels;

public class ShellViewModel : Screen
{
    private ObservableCollection<SampleData> _sampleData;
    private bool _isToggled;
    private string _statusMessage;
    private string _testTextBoxText;
    private string _testPasswordBoxPassword;
    private bool _testCheckBoxIsChecked;
    private bool _radioButton1IsChecked;
    private bool _radioButton2IsChecked;
    private int _selectedComboBoxIndex;
    private int _selectedListBoxIndex;
    private double _sliderValue;
    private double _progressBarValue;
    private System.DateTime? _selectedDate;
    private int _selectedTabIndex;
    private SampleData _selectedDataGridItem;
    private bool _isExpanderExpanded;
    private System.DateTime _selectedTime;
    private double _ratingValue;
    private bool _switchToggleIsChecked;
    private bool _actionToggleIsChecked;
    private bool _flatToggleIsChecked;
    private System.Windows.Media.Color _selectedColor;
    private double _discreteSliderValue;
    private bool _chip1IsChecked;
    private bool _chip2IsChecked;
    private bool _chip3IsChecked;

    public ShellViewModel()
    {
        DisplayName = "Comprehensive WPF Controls Test App";
        InitializeData();
        InitializeDefaults();
    }

    private void InitializeData()
    {
        SampleData = new ObservableCollection<SampleData>
        {
            new SampleData { Id = 1, Name = "John Doe", Age = 30, IsActive = true },
            new SampleData { Id = 2, Name = "Jane Smith", Age = 25, IsActive = false },
            new SampleData { Id = 3, Name = "Bob Johnson", Age = 35, IsActive = true },
            new SampleData { Id = 4, Name = "Alice Brown", Age = 28, IsActive = true },
        };
    }

    private void InitializeDefaults()
    {
        TestTextBoxText = "Enter text here";
        StatusMessage = "Tab selected: Tab 1";
        SliderValue = 50;
        ProgressBarValue = 30;
        SelectedTabIndex = 0;
        SelectedTime = System.DateTime.Now;
        RatingValue = 3.0;
        SelectedColor = System.Windows.Media.Colors.Blue;
        DiscreteSliderValue = 5.0;
    }

    // Properties with Caliburn.Micro naming conventions

    public ObservableCollection<SampleData> SampleData
    {
        get => _sampleData;
        set
        {
            _sampleData = value;
            NotifyOfPropertyChange();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(() => StatusTextBlock);
        }
    }

    public string StatusTextBlock => $"Status: {StatusMessage}";

    public string TestTextBoxText
    {
        get => _testTextBoxText;
        set
        {
            _testTextBoxText = value;
            NotifyOfPropertyChange();
        }
    }

    public string TestPasswordBoxPassword
    {
        get => _testPasswordBoxPassword;
        set
        {
            _testPasswordBoxPassword = value;
            NotifyOfPropertyChange();
        }
    }

    public bool TestCheckBoxIsChecked
    {
        get => _testCheckBoxIsChecked;
        set
        {
            _testCheckBoxIsChecked = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Checkbox is {(value ? "checked" : "unchecked")}");
        }
    }

    public bool RadioButton1IsChecked
    {
        get => _radioButton1IsChecked;
        set
        {
            _radioButton1IsChecked = value;
            NotifyOfPropertyChange();
            if (value) UpdateStatus("Radio Button 1 selected");
        }
    }

    public bool RadioButton2IsChecked
    {
        get => _radioButton2IsChecked;
        set
        {
            _radioButton2IsChecked = value;
            NotifyOfPropertyChange();
            if (value) UpdateStatus("Radio Button 2 selected");
        }
    }

    public int SelectedComboBoxIndex
    {
        get => _selectedComboBoxIndex;
        set
        {
            _selectedComboBoxIndex = value;
            NotifyOfPropertyChange();
            if (value >= 0)
                UpdateStatus($"ComboBox selected: Item {value + 1}");
        }
    }

    public int SelectedListBoxIndex
    {
        get => _selectedListBoxIndex;
        set
        {
            _selectedListBoxIndex = value;
            NotifyOfPropertyChange();
            if (value >= 0)
                UpdateStatus($"ListBox selected: List Item {value + 1}");
        }
    }

    public double SliderValue
    {
        get => _sliderValue;
        set
        {
            _sliderValue = value;
            NotifyOfPropertyChange();
            ProgressBarValue = value;
            UpdateStatus($"Slider value: {value:F0}");
        }
    }

    public double ProgressBarValue
    {
        get => _progressBarValue;
        set
        {
            _progressBarValue = value;
            NotifyOfPropertyChange();
        }
    }

    public System.DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            _selectedDate = value;
            NotifyOfPropertyChange();
            if (value.HasValue)
                UpdateStatus($"Date selected: {value.Value.ToShortDateString()}");
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            _selectedTabIndex = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Tab selected: Tab {value + 1}");
        }
    }

    public SampleData SelectedDataGridItem
    {
        get => _selectedDataGridItem;
        set
        {
            _selectedDataGridItem = value;
            NotifyOfPropertyChange();
            if (value != null)
                UpdateStatus($"DataGrid row selected: {value.Name}");
        }
    }

    public bool IsExpanderExpanded
    {
        get => _isExpanderExpanded;
        set
        {
            _isExpanderExpanded = value;
            NotifyOfPropertyChange();
            if (value)
                UpdateStatus("Expander opened");
        }
    }

    public System.DateTime SelectedTime
    {
        get => _selectedTime;
        set
        {
            _selectedTime = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Time selected: {value.ToShortTimeString()}");
        }
    }

    public double RatingValue
    {
        get => _ratingValue;
        set
        {
            _ratingValue = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Rating: {value} stars");
        }
    }

    public bool SwitchToggleIsChecked
    {
        get => _switchToggleIsChecked;
        set
        {
            _switchToggleIsChecked = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Switch toggle is {(value ? "ON" : "OFF")}");
        }
    }

    public bool ActionToggleIsChecked
    {
        get => _actionToggleIsChecked;
        set
        {
            _actionToggleIsChecked = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Action toggle is {(value ? "ON" : "OFF")}");
        }
    }

    public bool FlatToggleIsChecked
    {
        get => _flatToggleIsChecked;
        set
        {
            _flatToggleIsChecked = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Flat toggle is {(value ? "ON" : "OFF")}");
        }
    }

    public System.Windows.Media.Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            _selectedColor = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Color selected: {value}");
        }
    }

    public double DiscreteSliderValue
    {
        get => _discreteSliderValue;
        set
        {
            _discreteSliderValue = value;
            NotifyOfPropertyChange();
            UpdateStatus($"Discrete slider: {value}");
        }
    }

    public bool Chip1IsChecked
    {
        get => _chip1IsChecked;
        set
        {
            _chip1IsChecked = value;
            NotifyOfPropertyChange();
            if (value) UpdateStatus("Action Chip selected");
        }
    }

    public bool Chip2IsChecked
    {
        get => _chip2IsChecked;
        set
        {
            _chip2IsChecked = value;
            NotifyOfPropertyChange();
            if (value) UpdateStatus("Filter Chip selected");
        }
    }

    public bool Chip3IsChecked
    {
        get => _chip3IsChecked;
        set
        {
            _chip3IsChecked = value;
            NotifyOfPropertyChange();
            if (value) UpdateStatus("Choice Chip selected");
        }
    }

    // Action methods - Caliburn.Micro will wire these automatically

    public void TestButton()
    {
        // The fixture must remain non-modal so a script can continue through its
        // subsequent controls after invoking this button.
        UpdateStatus("Button clicked!");
    }

    public void ToggleButton()
    {
        _isToggled = !_isToggled;
        UpdateStatus($"Toggle button is {(_isToggled ? "ON" : "OFF")}");
    }

    public void MenuItem(string parameter)
    {
        UpdateStatus($"Menu item clicked: {parameter}");

        if (parameter == "Exit")
        {
            Application.Current.Shutdown();
        }
    }

    public void ToolBarButton(string parameter)
    {
        UpdateStatus($"ToolBar button clicked: {parameter}");
    }

    public void ToolBarToggle(string parameter)
    {
        UpdateStatus($"ToolBar toggle: {parameter}");
    }

    public void OutlinedButton()
    {
        UpdateStatus("Outlined button clicked!");
    }

    public void FlatButton()
    {
        UpdateStatus("Flat button clicked!");
    }

    public void IconButton()
    {
        UpdateStatus("Icon button clicked!");
    }

    public void FloatingActionButton()
    {
        UpdateStatus("Floating Action Button clicked!");
    }

    public void ShowSnackbar()
    {
        UpdateStatus("Snackbar message displayed");
        // In real app, you'd use MaterialDesign's MessageQueue
    }

    public void Chip1()
    {
        UpdateStatus("Action Chip clicked!");
    }

    public void Chip2()
    {
        Chip2IsChecked = !Chip2IsChecked;
    }

    public void Chip3()
    {
        Chip3IsChecked = !Chip3IsChecked;
    }

    public void DeletableChip()
    {
        UpdateStatus("Deletable chip action triggered");
    }

    private void UpdateStatus(string message)
    {
        StatusMessage = message;
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
