# CLIF - Comprehensive WPF UI Automation CLI Instructions

This project is a mature .NET CLI wrapper for FlaUI that provides robust support for comprehensive WPF application automation through process attachment.

## Project Overview
- **Type**: .NET Console Application / CLI Tool (.NET 8.0-windows)
- **Primary Library**: FlaUI.Core and FlaUI.UIA3 for UI automation
- **Target**: WPF applications via process ID attachment
- **Architecture**: Dependency injection with comprehensive service layer
- **Capabilities**: Advanced WPF control automation with session capture

## Current Implementation Status ✅
The application is **fully functional** with the following implemented features:

### Core Commands Available
1. **`list-processes`** - List WPF processes with detailed info and multiple output formats
2. **`click`** - Click UI elements with session capture
3. **`type`** - Type text into UI elements with session capture
4. **`tree`** - Display automation element trees with filtering and search
5. **`interact`** - Advanced interactions with complex WPF controls
6. **`script`** - Execute JSON automation scripts with comprehensive features
7. **`interactive`** - Interactive mode (framework ready, under development)

### Advanced WPF Control Support
- **DataGrid**: Checkbox manipulation, row selection, data extraction
- **TreeView**: Node expansion/collapse, selection by name
- **ComboBox/ListBox**: Item selection, item enumeration
- **TabControl**: Tab selection by name or index
- **ToggleButton**: State toggling and setting
- **Expander**: Expand/collapse operations
- **Slider**: Value setting
- **DatePicker/Calendar**: Date selection
- **Menu/Toolbar**: Menu navigation and toolbar actions

### Session Capture System
- Automatic screenshot capture before/after interactions
- Window-focused capture (brings target window to foreground)
- Session-based organization with unique session IDs
- Comprehensive logging with interaction validation

## Quick Reference for CLI Usage

### Basic Operations
```bash
# List processes
clif list-processes --detailed

# Simple click
clif click --process-id 1234 --element "id=MyButton"

# Type text
clif type --process-id 1234 --element "id=TextBox" --text "Hello World"

# Show element tree
clif tree MyApp --depth 5 --search "name:Button"
```

### Advanced Control Interactions
```bash
# ComboBox selection
clif interact --element "id=MyCombo" --control-type combobox --action select --value "Item 2" --process-id 1234

# DataGrid checkbox operations
clif interact --element "id=MyGrid" --control-type datagrid --action set-checkbox-by-name --value "John Doe,true" --process-id 1234
clif interact --element "id=MyGrid" --control-type datagrid --action uncheck-all --process-id 1234

# TreeView operations
clif interact --element "id=MyTree" --control-type tree --action expand --value "Root Node" --process-id 1234

# Toggle buttons
clif interact --element "name=Bold" --control-type togglebutton --action toggle --process-id 1234
```

### Script Execution
```bash
# Run comprehensive automation script
clif script examples/comprehensive-wpf-test.json --process-id 1234
```

## Development Guidelines
- **Architecture**: Uses dependency injection with IServiceProvider
- **Error Handling**: Comprehensive try-catch with logging throughout
- **Session Management**: SessionCaptureService handles all interaction logging
- **Element Discovery**: Multiple selector types (name=, id=, class=, type=)
- **Control Abstraction**: AutomationService abstracts FlaUI complexity
- **Extensibility**: Service-based architecture for easy extension

## Key Services Architecture
- **IAutomationService**: Core UI automation operations
- **IProcessService**: Process discovery and attachment
- **IElementTreeService**: Element tree navigation and search
- **IScriptService**: JSON script execution engine
- **ISessionCaptureService**: Screenshot and logging management
- **IInteractiveService**: Interactive mode framework

## Testing
Includes TestWpfApp for comprehensive testing of all WPF control types and automation scenarios.

## Current Status & Usage Notes

### Tested and Working Features ✅
- All basic commands (list-processes, click, type, tree) are fully functional
- Advanced interact command supports all documented control types
- Session capture system working with window-focused screenshots
- Script execution engine supports comprehensive JSON automation scripts
- DataGrid checkbox manipulation (set, toggle, uncheck-all, get-states)
- TreeView expand/collapse operations
- ComboBox/ListBox item selection
- ToggleButton state management
- Tab control navigation

### Under Development 🚧
- Interactive mode CLI (framework exists, command parsing in progress)
- Additional script validation features
- Extended menu navigation support

### Quick Command Reference for Development

#### Process Discovery
```bash
clif list-processes                    # List all WPF processes
clif list-processes --detailed         # Detailed process info
Get-Process | Where-Object { $_.ProcessName -like "*Test*" }  # PowerShell alternative
```

#### Element Discovery
```bash
clif tree 1234                         # Show full element tree
clif tree 1234 --depth 3               # Limit depth
clif tree 1234 --search "name:Button"  # Search for elements
```

#### Basic Interactions
```bash
clif click --process-id 1234 --element "id=MyButton"
clif type --process-id 1234 --element "id=TextBox" --text "Sample text"
```

#### Advanced Control Operations
```bash
# DataGrid operations
clif interact --element "id=Grid" --control-type datagrid --action uncheck-all --process-id 1234
clif interact --element "id=Grid" --control-type datagrid --action set-checkbox-by-name --value "Name,true" --process-id 1234

# TreeView operations  
clif interact --element "id=Tree" --control-type tree --action expand --value "Node Name" --process-id 1234

# ComboBox selection
clif interact --element "id=Combo" --control-type combobox --action select --value "Item Text" --process-id 1234
```

### Element Selector Formats
- `id=AutomationId` - Most reliable, use AutomationId property
- `name=ElementName` - Use Name property or visible text
- `class=ClassName` - Use ClassName property
- `type=ControlType` - Use control type (Button, TextBox, etc.)

### Session Capture Integration
Every command automatically creates a session with:
- Unique session ID and timestamp
- Before/after screenshots
- Interaction logs with validation results
- Error capture for failed operations

## Extension Points
The application is designed for easy extension:
- Add new commands by implementing Command base class
- Extend AutomationService for new control types
- Add new script actions through ScriptService
- Implement new selector types in element discovery
- Extend SessionCaptureService for additional logging features