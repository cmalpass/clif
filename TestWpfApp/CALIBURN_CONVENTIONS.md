# Caliburn.Micro Conventions Guide

## View-ViewModel Naming

Caliburn.Micro automatically discovers and binds Views to ViewModels using naming conventions:

### Standard Pattern
- `ShellViewModel` → `ShellView`
- `CustomerViewModel` → `CustomerView`
- `OrderDetailsViewModel` → `OrderDetailsView`

### Namespace Convention
```
ViewModels/ShellViewModel.cs
Views/ShellView.xaml
```

## Property Binding

### Automatic Binding by Name
When a control's `x:Name` matches a ViewModel property, Caliburn.Micro automatically binds them:

**View:**
```xml
<TextBlock x:Name="StatusTextBlock" />
```

**ViewModel:**
```csharp
public string StatusTextBlock => $"Status: {StatusMessage}";
```

### Explicit Binding
Use traditional WPF binding syntax when needed:
```xml
<TextBox Text="{Binding TestTextBoxText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

## Action Methods

### Simple Actions
Method names matching control names are automatically wired:

**View:**
```xml
<Button x:Name="TestButton" Content="Click Me" />
```

**ViewModel:**
```csharp
public void TestButton()
{
    // Button click logic
}
```

### Actions with Parameters
Use `cal:Message.Attach` for methods with parameters:

**View:**
```xml
<MenuItem Header="Save" cal:Message.Attach="[Event Click] = [Action MenuItem('Save')]"/>
```

**ViewModel:**
```csharp
public void MenuItem(string parameter)
{
    // parameter will be "Save"
}
```

### Event Binding
Bind to any control event:

**View:**
```xml
<PasswordBox cal:Message.Attach="[Event PasswordChanged] = [Action PasswordChanged($source.Password)]"/>
```

**ViewModel:**
```csharp
public void PasswordChanged(string password)
{
    // Handle password change
}
```

## Property Change Notification

### Using Screen Base Class
Inherit from `Screen` for lifecycle management:

```csharp
public class ShellViewModel : Screen
{
    private string _statusMessage;
    
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            NotifyOfPropertyChange(); // No parameter needed (CallerMemberName)
            NotifyOfPropertyChange(() => StatusTextBlock); // Notify related property
        }
    }
    
    public string StatusTextBlock => $"Status: {StatusMessage}";
}
```

### Multiple Property Notifications
```csharp
NotifyOfPropertyChange(() => PropertyA);
NotifyOfPropertyChange(() => PropertyB);
```

## Guard Properties

### Can Execute Pattern
Add a `CanXxx` property to control when an action can execute:

**ViewModel:**
```csharp
public bool CanSaveData => !string.IsNullOrEmpty(Name);

public void SaveData()
{
    // Save logic
}
```

**View:**
```xml
<Button x:Name="SaveData" Content="Save" />
```

Button automatically enables/disables based on `CanSaveData`.

## Collections and Selection

### Observable Collections
```csharp
private ObservableCollection<SampleData> _sampleData;

public ObservableCollection<SampleData> SampleData
{
    get => _sampleData;
    set
    {
        _sampleData = value;
        NotifyOfPropertyChange();
    }
}
```

**View:**
```xml
<DataGrid ItemsSource="{Binding SampleData}" 
          SelectedItem="{Binding SelectedDataGridItem}" />
```

### Selection Binding
```csharp
private SampleData _selectedDataGridItem;

public SampleData SelectedDataGridItem
{
    get => _selectedDataGridItem;
    set
    {
        _selectedDataGridItem = value;
        NotifyOfPropertyChange();
        if (value != null)
            UpdateStatus($"Selected: {value.Name}");
    }
}
```

## Control-Specific Conventions

### TextBox
```csharp
// Property name matches x:Name
public string TestTextBox { get; set; }
```

### CheckBox
```csharp
// Add "IsChecked" suffix
public bool TestCheckBoxIsChecked { get; set; }
```

### RadioButton
```csharp
public bool RadioButton1IsChecked { get; set; }
public bool RadioButton2IsChecked { get; set; }
```

### ComboBox/ListBox
```csharp
// Use "SelectedIndex" or "SelectedItem" suffix
public int SelectedComboBoxIndex { get; set; }
public object SelectedListBoxItem { get; set; }
```

### Slider/ProgressBar
```csharp
// Use "Value" suffix
public double SliderValue { get; set; }
```

### DatePicker
```csharp
public DateTime? SelectedDate { get; set; }
```

### TabControl
```csharp
public int SelectedTabIndex { get; set; }
```

### Expander
```csharp
public bool IsExpanderExpanded { get; set; }
```

## Special Binding Contexts

### $source
Access the source control:
```xml
cal:Message.Attach="[Event PasswordChanged] = [Action PasswordChanged($source.Password)]"
```

### $eventArgs
Access event arguments:
```xml
cal:Message.Attach="[Event SelectionChanged] = [Action OnSelectionChanged($eventArgs)]"
```

### $dataContext
Access the DataContext explicitly:
```xml
cal:Message.Attach="[Event Click] = [Action DoSomething($dataContext)]"
```

## Bootstrapper Setup

### Basic Configuration
```csharp
public class AppBootstrapper : BootstrapperBase
{
    private SimpleContainer _container = new SimpleContainer();

    public AppBootstrapper()
    {
        Initialize();
    }

    protected override void Configure()
    {
        _container.Instance(_container);
        
        // Register framework services
        _container
            .Singleton<IWindowManager, WindowManager>()
            .Singleton<IEventAggregator, EventAggregator>();

        // Auto-register ViewModels
        GetType().Assembly.GetTypes()
            .Where(type => type.IsClass && type.Name.EndsWith("ViewModel"))
            .ToList()
            .ForEach(viewModelType => _container.RegisterPerRequest(
                viewModelType, viewModelType.ToString(), viewModelType));
    }

    protected override void OnStartup(object sender, StartupEventArgs e)
    {
        DisplayRootViewForAsync<ShellViewModel>();
    }

    protected override object GetInstance(Type service, string key)
    {
        return _container.GetInstance(service, key);
    }

    protected override IEnumerable<object> GetAllInstances(Type service)
    {
        return _container.GetAllInstances(service);
    }

    protected override void BuildUp(object instance)
    {
        _container.BuildUp(instance);
    }
}
```

### App.xaml Integration
```xml
<Application x:Class="TestWpfApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:TestWpfApp">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <local:AppBootstrapper x:Key="Bootstrapper" />
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## Best Practices

1. **Keep Views Simple**: Minimal or no code-behind
2. **ViewModel Logic**: All business logic in ViewModels
3. **Use Screen**: Inherit from `Screen` for lifecycle support
4. **Observable Collections**: Use for dynamic lists
5. **NotifyOfPropertyChange**: Always notify dependent properties
6. **Guard Properties**: Use `CanXxx` for action validation
7. **Naming Consistency**: Follow conventions strictly
8. **Testability**: Write unit tests for ViewModels

## Testing ViewModels

```csharp
[Fact]
public void TestButton_ShouldUpdateStatus()
{
    // Arrange
    var viewModel = new ShellViewModel();
    
    // Act
    viewModel.TestButton();
    
    // Assert
    Assert.Contains("Button clicked", viewModel.StatusMessage);
}
```

## Common Patterns

### Master-Detail
```csharp
public ObservableCollection<Customer> Customers { get; set; }
public Customer SelectedCustomer { get; set; }

public ObservableCollection<Order> Orders => 
    SelectedCustomer?.Orders ?? new ObservableCollection<Order>();
```

### Validation
```csharp
public bool CanSave => !string.IsNullOrEmpty(Name) && Age > 0;
```

### Async Operations
```csharp
public async Task LoadDataAsync()
{
    IsLoading = true;
    try
    {
        var data = await _dataService.GetDataAsync();
        Items = new ObservableCollection<Item>(data);
    }
    finally
    {
        IsLoading = false;
    }
}
```
