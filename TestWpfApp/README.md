# TestWpfApp - Caliburn.Micro MVVM Implementation

## Overview

TestWpfApp is a comprehensive WPF test application built using **Caliburn.Micro** framework with proper MVVM (Model-View-ViewModel) conventions. This application demonstrates a wide variety of WPF controls and serves as a test target for the CLIF UI automation tool.

## Architecture

### Caliburn.Micro Conventions

The application follows Caliburn.Micro naming conventions:

- **Views**: Located in `Views/` directory
  - `ShellView.xaml` - Main window view
  
- **ViewModels**: Located in `ViewModels/` directory
  - `ShellViewModel.cs` - Main window view model with Screen lifecycle

- **Bootstrapper**: `AppBootstrapper.cs`
  - Configures dependency injection with SimpleContainer
  - Auto-discovers and registers ViewModels
  - Handles application startup and root view display

### MVVM Implementation

#### Data Binding
- All UI properties are bound to ViewModel properties using `{Binding PropertyName}`
- Two-way binding for user input controls (TextBox, CheckBox, etc.)
- Caliburn.Micro automatically wires up properties by naming convention

#### Action Methods
Caliburn.Micro automatically wires methods to UI elements:
- `TestButton()` method automatically wires to button with `x:Name="TestButton"`
- `MenuItem(string parameter)` with parameters using `cal:Message.Attach`
- No need for ICommand implementations

#### Property Change Notification
- ViewModel inherits from `Screen` (Caliburn.Micro base class)
- Uses `NotifyOfPropertyChange()` for property change notifications
- Automatic UI updates when properties change

### Key Features

#### Basic Controls
- **Buttons**: Click actions, toggle behavior
- **TextBox**: Two-way data binding with property updates
- **PasswordBox**: Secure input handling
- **CheckBox**: Boolean binding
- **RadioButtons**: Grouped selection with binding
- **ComboBox/ListBox**: Selection binding
- **Slider/ProgressBar**: Value synchronization

#### Advanced Controls
- **DatePicker/Calendar**: Date selection with nullable DateTime
- **GroupBox**: Organized control grouping
- **Expander**: Collapsible content with IsExpanded binding
- **TabControl**: Tab navigation with selected index binding

#### Complex Controls
- **DataGrid**: Observable collection binding with row selection
- **TreeView**: Hierarchical data display
- **Menu/ToolBar**: Action methods with parameters
- **StatusBar**: Dynamic status message display
- **RichTextBox**: Document editing

### Dependency Injection

The `AppBootstrapper` configures a `SimpleContainer` with:
- `IWindowManager` - For window management
- `IEventAggregator` - For loosely-coupled messaging
- Auto-registration of all ViewModels via reflection

### Naming Conventions

Caliburn.Micro uses convention-based approach:

1. **View-ViewModel Matching**: 
   - `ShellViewModel` → `ShellView`
   - Automatic discovery and binding

2. **Property Binding**:
   - `x:Name="TestButton"` → wires to method `TestButton()`
   - `x:Name="StatusTextBlock"` → binds to property `StatusTextBlock`

3. **Action Parameters**:
   - Use `cal:Message.Attach` for methods with parameters
   - Example: `cal:Message.Attach="[Event Click] = [Action MenuItem('New')]"`

## Automation Support

All controls maintain their AutomationId properties for CLIF automation:
- Button: `id=TestButton`
- TextBox: `id=TestTextBox`
- CheckBox: `id=TestCheckBox`
- DataGrid: `id=TestDataGrid`
- TabControl: `id=TestTabControl`
- etc.

The MVVM architecture does not affect UI Automation - all elements are discoverable through FlaUI.

## Running the Application

```bash
# Build
dotnet build TestWpfApp/TestWpfApp.csproj

# Run
dotnet run --project TestWpfApp/TestWpfApp.csproj
```

## Testing with CLIF

```bash
# List processes
clif list-processes

# Click button
clif click --process-id <PID> --element "id=TestButton"

# Type text
clif type --process-id <PID> --element "id=TestTextBox" --text "Hello MVVM!"

# Interact with controls
clif interact --process-id <PID> --element "id=TestCheckBox" --control-type checkbox --action toggle
```

## Benefits of Caliburn.Micro Implementation

1. **Cleaner Code**: No code-behind event handlers, all logic in ViewModel
2. **Testability**: ViewModels can be unit tested without UI
3. **Maintainability**: Clear separation of concerns
4. **Convention over Configuration**: Less boilerplate code
5. **Powerful Features**: Screen lifecycle, coroutines, event aggregation
6. **Automation-Friendly**: UI Automation still works seamlessly

## NuGet Dependencies

- **Caliburn.Micro** (4.0.212): Core MVVM framework
- Includes:
  - `Caliburn.Micro.Core`
  - Screen/Conductor patterns
  - Event aggregation
  - Dependency injection

## File Structure

```
TestWpfApp/
├── App.xaml                    # Application entry point
├── App.xaml.cs                 # Application code-behind
├── AppBootstrapper.cs          # Caliburn.Micro bootstrapper
├── ViewModels/
│   └── ShellViewModel.cs       # Main window ViewModel
├── Views/
│   ├── ShellView.xaml          # Main window View
│   └── ShellView.xaml.cs       # View code-behind (minimal)
└── TestWpfApp.csproj           # Project file with Caliburn.Micro reference
```

## Migration Notes

The application was migrated from traditional WPF event handlers to Caliburn.Micro MVVM:

**Before** (Code-behind):
```csharp
private void TestButton_Click(object sender, RoutedEventArgs e)
{
    MessageBox.Show("Hello!");
}
```

**After** (ViewModel):
```csharp
public void TestButton()
{
    MessageBox.Show("Hello!");
}
```

All automation IDs and element discovery remain unchanged, ensuring backward compatibility with existing CLIF automation scripts.
