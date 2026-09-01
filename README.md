# CLIF - Comprehensive WPF UI Automation CLI

[![Build and Test](https://github.com/cmalpass/clif/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cmalpass/clif/actions/workflows/build-and-test.yml)

A powerful .NET CLI tool and MCP server for automating Windows desktop applications through UI automation using FlaUI.

> **Project status:** CLIF is under active development. The production automation backend is Windows UI Automation–based; the Avalonia application in this repository is a compatibility fixture, not a macOS or Linux automation backend. The MCP server is an early-release component intended for trusted local development environments, not untrusted agent input or production desktop control. See [SECURITY.md](SECURITY.md), [SUPPORT.md](SUPPORT.md), and [CHANGELOG.md](CHANGELOG.md).

## Features

🔧 **Process Attachment**: Attach to running WPF processes by process ID with robust error handling
🌳 **Element Tree Navigation**: Explore and print automation element trees with filtering and search options  
⚡ **Advanced Control Support**: Comprehensive interactions with ComboBox, ListBox, DataGrid, TreeView, TabControl, Expander, ToggleButton, Menu, and more
📝 **Script Support**: JSON-based automation scripts with session capture and logging
🎯 **DataGrid Automation**: Set/toggle checkboxes by name or index, get states, uncheck all functionality
🔍 **Multiple Selector Types**: Support for name=, id=, class=, type= selectors
📊 **Session Capture**: Automatic screenshot capture and logging for all interactions
📸 **Window-Focused Screenshots**: Brings target window to foreground before capturing
🎛️ **Interactive Command System**: Advanced interact command for complex WPF control manipulation
🤖 **MCP Server**: Model Context Protocol server for AI agent integration (Copilot, Claude, VS Code, etc.)
🔧 **Extensible Architecture**: Modular design with dependency injection and comprehensive logging

## Installation

### Prerequisites
- .NET 8.0 or later
- Windows OS (UI Automation is Windows-specific)

### Cross-Platform Test Fixture (not a cross-platform CLIF backend)

The repository also includes `TestCrossPlatformApp`, an Avalonia-based desktop
fixture that targets Windows, macOS, and Linux. It provides a stable control
surface for contract tests and future backends; it does not make CLIF itself
cross-platform. CLIF's production backend remains Windows UI Automation, while
`TestWpfApp` is the primary Windows/WPF compatibility fixture.

```bash
dotnet run --project TestCrossPlatformApp/TestCrossPlatformApp.csproj
```

The fixture contract is documented in [TestCrossPlatformApp/README.md](TestCrossPlatformApp/README.md). Its controls use stable `AutomationProperties.AutomationId` values and visible state readouts so automation tests can verify effects instead of only checking that an interaction call returned.

On Windows, the dedicated CLIF/UIA smoke tests can be run with:

```powershell
dotnet test CLIF.Tests/CLIF.Tests.csproj --configuration Release --filter "Category=CrossPlatformUI"
```

The display-free fixture contract tests can run on any supported desktop host:

```bash
dotnet test TestCrossPlatformApp.Tests/TestCrossPlatformApp.Tests.csproj
```

### Build from Source
```bash
git clone <repository-url>
cd clif
dotnet build
dotnet publish -c Release -o ./publish
```

## Quick Start

### List Available WPF Processes
```bash
clif list-processes
clif list-processes --detailed --format json
```

### Basic Commands
```bash
# Click a button by process ID and element selector
clif click --process-id 1234 --element "id=MyButton"

# Type text into a field
clif type --process-id 1234 --element "id=TextBox" --text "Hello World"

# Show application element tree
clif tree TestWpfApp --depth 5
```

### Advanced Control Interactions
```bash
# Select from ComboBox
clif interact --process-id 1234 --element "id=MyCombo" --control-type combobox --action select --value "Item 2"

# Toggle DataGrid checkbox by name
clif interact --process-id 1234 --element "id=MyGrid" --control-type datagrid --action set-checkbox-by-name --value "John Doe,true"

# Expand/collapse TreeView nodes
clif interact --process-id 1234 --element "id=MyTree" --control-type tree --action expand --value "Root Node"

# Toggle toolbar buttons
clif interact --process-id 1234 --element "name=Bold" --control-type togglebutton --action toggle
```

### Script Automation
```bash
# Execute automation script
clif script examples/comprehensive-wpf-test.json --process-id 1234
```

### Interactive Mode
```bash
# Start interactive session
clif interactive --process-id 1234

# Interactive mode is currently under development
```

## MCP Server (AI Agent Integration, Windows only)

CLIF includes a standalone MCP (Model Context Protocol) server that allows AI
agents to automate Windows desktop applications. It is a local Windows stdio
server, not a hosted or cross-platform automation service. The MCP server is
inspired by [FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman.

### Quick Start

```bash
# Build the MCP server
cd CLIF.Mcp
dotnet build

# Run the MCP server (JSON-RPC over stdio)
dotnet run --project CLIF.Mcp
```

### MCP Client Configuration

Configure your AI tool to use CLIF as an MCP server:

**GitHub Copilot / Claude Desktop / VS Code:**
```json
{
  "mcpServers": {
    "clif": {
      "type": "local",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\CLIF.Mcp"]
    }
  }
}
```

### MCP permissions

The MCP server uses a default-deny, least-privilege policy. Launches require an
exact executable name or canonical path in `CLIF_MCP_ALLOWED_APPS`; mutating UI
actions require `CLIF_MCP_ALLOW_INPUT=true`. Window enumeration, window close,
and full-screen capture are independently disabled unless explicitly enabled:

```text
CLIF_MCP_ALLOWED_APPS=C:\Path\To\TestWpfApp.exe
CLIF_MCP_ALLOW_INPUT=true
CLIF_MCP_ALLOW_WINDOW_ENUMERATION=false
CLIF_MCP_ALLOW_WINDOW_CLOSE=false
CLIF_MCP_ALLOW_FULL_SCREEN_CAPTURE=false
```

Keep these permissions scoped to a dedicated local session and grant only the
applications and capabilities required for the task. Permission changes take
effect when a new MCP server process starts.

MCP diagnostics are emitted as one JSON object per line to stderr; stdout is
reserved for JSON-RPC messages. Set `CLIF_MCP_LOG_LEVEL=off` to disable these
diagnostics for a quiet local session. Events include request correlation IDs,
method, outcome, and sanitized lifecycle metadata; typed values, script content,
screenshots, and command-line arguments are not logged by default.

**Using compiled executable:**
```json
{
  "mcpServers": {
    "clif": {
      "type": "local",
      "command": "C:\\path\\to\\CLIF.Mcp.exe"
    }
  }
}
```

### Available MCP Tools

| Tool | Description |
|------|-------------|
| `clif_launch` | Launch a Windows application and get its window handle |
| `clif_snapshot` | Get an accessibility snapshot with semantic element refs |
| `clif_click` | Click an element (supports Invoke, Toggle, Selection patterns) |
| `clif_type` | Type text into an element (append) |
| `clif_fill` | Clear and fill an element's value (replace) |
| `clif_get_text` | Extract text content from an element |
| `clif_screenshot` | Capture a specified element or registered window (full-screen capture requires host policy) |
| `clif_list_windows` | List all open windows with handles |
| `clif_focus` | Bring a window to the foreground |
| `clif_close` | Close a window |
| `clif_batch` | Execute multiple actions in a single call |
| `clif_interact` | Advanced WPF control interactions (ComboBox, DataGrid, TreeView, etc.) |
| `clif_search_elements` | Search for elements by name, ID, type, or class |
| `clif_validate_script` | Validate inline CLIF JSON automation script content (execution remains a CLI capability) |

### MCP Workflow Example

The MCP executable is hosted on the official C# SDK (currently 2.2.x). It
supports the modern `2026-07-28` stateless request metadata and discovery
contract, while negotiating down-level legacy clients such as `2025-06-18`.
Modern `tools/list` responses include cache metadata and deterministic ordinal
tool ordering. UI automation requests are serialized and honor
`notifications/cancelled`.

```
Agent: "Open Calculator and compute 7 × 8"

1. clif_launch { "app": "calc.exe" }
   → "Window handle: w1, Title: Calculator"

2. clif_snapshot { "handle": "w1" }
   → "- window "Calculator" [ref=w1e1]
        - button "7" [ref=w1e47]
        - button "×" [ref=w1e35]
        - button "8" [ref=w1e48]
        - button "=" [ref=w1e38]"

3. clif_batch { "actions": [
     { "action": "click", "ref": "w1e47" },
     { "action": "click", "ref": "w1e35" },
     { "action": "click", "ref": "w1e48" },
     { "action": "click", "ref": "w1e38" }
   ]}
   → "1. click: Invoked 7
      2. click: Invoked Multiply
      3. click: Invoked 8
      4. click: Invoked Equals"

4. clif_screenshot { "handle": "w1" }
   → [Base64 PNG showing "56"]
```

### Advanced MCP Interactions

The `clif_interact` tool provides access to all of CLIF's advanced WPF control capabilities:

```json
// Select from a ComboBox
{ "ref": "w1e10", "controlType": "combobox", "action": "select", "value": "Option B" }

// Get DataGrid data
{ "ref": "w1e20", "controlType": "datagrid", "action": "get_data" }

// Expand a TreeView node
{ "ref": "w1e30", "controlType": "tree", "action": "expand", "value": "Documents" }

// Select a tab
{ "ref": "w1e40", "controlType": "tabcontrol", "action": "select", "value": "Settings" }

// Set a slider value
{ "ref": "w1e50", "controlType": "slider", "action": "set_value", "value": "75" }
```

### Explore Application Structure
```bash
# Display full element tree
clif tree Calculator

# Show tree with custom depth and filters
clif tree Calculator --depth 5 --enabled-only --show-selectors

# Search for specific elements
clif tree MyApp --search "name:Submit" --output tree-output.txt

# Filter by control type  
clif tree Calculator --control-type Button
```

### Script Automation
```bash
# Execute automation script
clif script examples/calculator-script.json

# Validate script syntax
clif script --validate examples/my-script.json
```

## Element Selectors

CLIF supports multiple selector formats:

- **By Name**: `name=ButtonText` or just `ButtonText`
- **By Automation ID**: `id=MyButton`
- **By Class Name**: `class=Button`
- **By Control Type**: `type=Button`

## Commands Reference

### Core Commands

#### `list-processes [options]`
List all available WPF processes.

**Options:**
- `--detailed` - Show detailed process information
- `--format <format>` - Output format: table (default), json, csv

#### `click --process-id <id> --element <selector>`
Click on a UI element.

**Options:**
- `--process-id <id>` - Target process ID (required)
- `--element <selector>` - Element selector (required)

#### `type --process-id <id> --element <selector> --text <text>`
Type text into a UI element.

**Options:**
- `--process-id <id>` - Target process ID (required)
- `--element <selector>` - Element selector (required)
- `--text <text>` - Text to type (required)

#### `interact --process-id <id> --element <selector> --control-type <type> --action <action> [options]`
Advanced interactions with various WPF controls.

**Options:**
- `--process-id <id>` - Target process ID (required)
- `--element <selector>` - Element selector (required)
- `--control-type <type>` - Control type: combobox, listbox, checkbox, radiobutton, slider, tab, tree, datepicker, calendar, expander, datagrid, menu, togglebutton (required)
- `--action <action>` - Action to perform (required)
- `--value <value>` - Value for the action (optional)
- `--index <number>` - Index for selection actions (optional)

#### `tree <process> [options]`
Display or search the automation element tree.

**Arguments:**
- `<process>` - Process name, window title, or process ID

**Options:**
- `--depth <number>` - Maximum tree depth (default: 10)
- `--enabled-only` - Show only enabled elements
- `--visible-only` - Show only visible elements
- `--show-properties` - Show element properties (default: true)
- `--show-selectors` - Show element selectors (default: true)
- `--search <criteria>` - Search criteria (name:text, id:value, etc.)
- `--control-type <type>` - Filter by control type
- `--output <file>` - Save tree to file

#### `script <script-file>`
Execute automation scripts from JSON files.

**Arguments:**
- `<script-file>` - Path to JSON script file

#### `interactive [options]`
Start interactive mode for sequential operations.

**Options:**
- `--process-id <id>` - Process ID to attach to initially

*Note: Interactive mode is currently under development*

### Control-Specific Actions

#### ComboBox/ListBox Actions
- `select` - Select item by text value
- `get-items` - Display all available items

#### DataGrid Actions
- `select-row` - Select row by index
- `get-data` - Display all grid data
- `set-checkbox` - Set checkbox state by row index
- `set-checkbox-by-name` - Set checkbox state by row name
- `toggle-checkbox` - Toggle checkbox by row index
- `get-checkbox-states` - Display all checkbox states
- `uncheck-all` - Uncheck all checkboxes

#### TreeView Actions
- `expand` - Expand node by name
- `collapse` - Collapse node by name
- `select` - Select node by name

#### Tab Control Actions
- `select` - Select tab by name or index

#### Toggle Button Actions
- `toggle` - Toggle button state
- `set` - Set specific state (true/false)

#### Expander Actions
- `toggle` - Toggle expanded/collapsed state
- `expand` - Expand the expander
- `collapse` - Collapse the expander

## Script Format

Scripts are JSON files with the following structure:

```json
{
  "name": "Script Name",
  "description": "Script description", 
  "version": "1.0",
  "target": {
    "processName": "Calculator",
    "windowTitle": "Calculator", 
    "timeoutMs": 30000
  },
  "steps": [
    {
      "action": "click",
      "element": "name=Five",
      "description": "Click the number 5",
      "delayMs": 500,
      "ignoreErrors": false
    },
    {
      "action": "type",
      "element": "class=Edit",
      "value": "Hello World",
      "description": "Type text"
    }
  ],
  "options": {
    "stopOnError": true,
    "logSteps": true,
    "takeScreenshots": false,
    "defaultTimeoutMs": 5000,
    "outputPath": "./output/script.log"
  }
}
```

### Supported Script Actions

- `click` - Click element
- `type` - Type text into element
- `clear` - Clear element content
- `select` - Select from ComboBox/ListBox
- `selectTab` - Select tab in TabControl
- `selectRow` - Select DataGrid row
- `selectCell` - Select DataGrid cell
- `expand` - Expand TreeView nodes or Expander
- `collapse` - Collapse TreeView nodes or Expander
- `setValue` - Set slider, date picker values
- `validate` - Validate element values
- `getState` - Get element state
- `getValue` - Get element value
- `getSelection` - Get current selection
- `screenshot` - Take screenshot with filename
- `log` - Add log message
- `wait` - Wait for specified duration

## Configuration

Configure CLIF through `appsettings.json`:

```json
{
  "CLIF": {
    "DefaultTimeout": 5000,
    "MaxTreeDepth": 10,
    "TakeScreenshotsOnError": true,
    "OutputDirectory": "./output"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Session Capture System

CLIF automatically captures screenshots and logs all interactions for debugging and validation:

### Features
- **Automatic Screenshots**: Captured before and after each interaction
- **Window Focus**: Target window brought to foreground for clear captures
- **Session Organization**: Each operation creates a unique session folder
- **Interaction Logging**: Detailed logs with timestamps and validation results
- **Error Documentation**: Failed operations captured with error details

### Session Folders
Sessions are saved to: `./sessions/SESSION_ID_TIMESTAMP/`
- `interaction_001_before.png` - Screenshot before action
- `interaction_001_after.png` - Screenshot after action  
- `session.log` - Detailed interaction log
- `validation_results.txt` - Action validation outcomes

### Example Session Log Entry
```
[2025-10-25 14:30:15] INFO: Session started: INTERACT_COMBOBOX_143015
[2025-10-25 14:30:16] INFO: Target window focused: TestWpfApp
[2025-10-25 14:30:16] INFO: Action 'select' on combobox: Item 2 selected successfully
[2025-10-25 14:30:16] INFO: Validation: ✓ ComboBox selection verified
```

## Examples

See the `examples/` directory for comprehensive script samples:

### Key Example Scripts
- `comprehensive-wpf-test.json` - Tests all WPF control types
- `session-capture-demo.json` - Demonstrates session capture features
- `datagrid-keyboard-approach.json` - Advanced DataGrid automation
- `visual-demo-test.json` - Visual testing scenarios

### Real-World Usage Example
```bash
# Complete DataGrid automation workflow
clif interact --element "id=EmployeeGrid" --control-type datagrid --action uncheck-all --process-id 5432
clif interact --element "id=EmployeeGrid" --control-type datagrid --action set-checkbox-by-name --value "John Smith,true" --process-id 5432
clif interact --element "id=EmployeeGrid" --control-type datagrid --action set-checkbox-by-name --value "Jane Doe,true" --process-id 5432
clif interact --element "id=EmployeeGrid" --control-type datagrid --action get-checkbox-states --process-id 5432
```

## Architecture

CLIF is built with a comprehensive modular architecture:

- **Command Layer**: System.CommandLine-based commands with parameter validation
- **Service Layer**: Abstracted services for automation, process management, and scripting
- **FlaUI Integration**: Direct integration with FlaUI.Core and FlaUI.UIA3 for UI automation
- **Session Management**: Comprehensive capture and logging system
- **MCP Server** (`CLIF.Mcp`): Standalone MCP server with JSON-RPC 2.0 over stdio for AI agent integration
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service management
- **Configuration Management**: JSON-based configuration with environment support

### Project Structure

| Project | Description |
|---------|-------------|
| `CLIF` | Main CLI application with commands, services, and validation |
| `CLIF.Mcp` | Standalone MCP server for AI agent integration |
| `CLIF.Tests` | Unit and integration tests |
| `TestWpfApp` | Sample WPF application for testing |

## TestWpfApp - Example Application

The repository includes **TestWpfApp**, a comprehensive WPF test application built with **Caliburn.Micro MVVM framework**. This application demonstrates:

- ✅ Proper MVVM architecture with Caliburn.Micro conventions
- ✅ All WPF control types for testing automation
- ✅ Full compatibility with CLIF automation tools
- ✅ Clean separation of concerns (Views/ViewModels)
- ✅ Convention-based data binding and action methods

### Running TestWpfApp

```bash
# Build and run
cd TestWpfApp
dotnet build
dotnet run

# Or run the compiled executable
./TestWpfApp/bin/Debug/net8.0-windows/TestWpfApp.exe
```

### Testing Automation

```bash
# Get the process ID
clif list-processes

# Test various controls
clif click --process-id <PID> --element "id=TestButton"
clif type --process-id <PID> --element "id=TestTextBox" --text "Hello MVVM!"
clif interact --process-id <PID> --element "id=TestCheckBox" --control-type checkbox --action toggle
```

### Documentation
- **README.md**: Architecture overview and features
- **CALIBURN_CONVENTIONS.md**: Complete convention guide for Caliburn.Micro
- **MIGRATION_SUMMARY.md**: Detailed migration notes and verification

The TestWpfApp demonstrates that CLIF works seamlessly with modern MVVM applications while maintaining clean, testable code architecture.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see LICENSE file for details.

### Attribution

The CLIF MCP server (CLIF.Mcp) was inspired by [FlaUI-MCP](https://github.com/shanselman/FlaUI-MCP) by Scott Hanselman (MIT License). The MCP protocol implementation, element registry pattern, and snapshot builder architecture draw from that project's design.

## Troubleshooting

### Common Issues

**"Element not found"**
- Use the `tree` command to explore available elements: `clif tree ProcessID --depth 5`
- Verify the selector syntax (use `id=AutomationId`, `name=ElementName`, `class=ClassName`)
- Check if the element is enabled and visible in the tree output

**"Process attachment failed"**
- Ensure you're using the correct process ID (not process name)
- Use `clif list-processes --detailed` to find the exact process ID
- Verify the target application is fully loaded

**"Control type not supported"**
- Check supported control types: combobox, listbox, datagrid, tree, togglebutton, expander, tab, slider, calendar, datepicker, checkbox, radiobutton, menu
- Use the `interact` command for advanced controls instead of basic `click` command

**"Action failed on control"**
- Verify the action is supported for that control type (e.g., `toggle` for togglebutton, `select` for combobox)
- Check the element is in the correct state (enabled, visible, focusable)
- Review session capture screenshots in `./sessions/` folder for visual debugging

**"Session capture not working"**
- Ensure the target application window is not minimized
- Check that the sessions directory has write permissions
- Verify the target application supports Windows UI Automation

**"Script execution errors"**
- Validate JSON syntax with `clif script --validate script.json`
- Check that all required elements exist using `tree` command first
- Review example scripts in `examples/` directory for proper syntax

### Debugging Tips

1. **Use Tree Command**: Always start with `clif tree ProcessID` to understand the element structure
2. **Check Sessions**: Review session capture screenshots and logs in `./sessions/` folder
3. **Verify Process ID**: Use `Get-Process | Where-Object { $_.ProcessName -like "*YourApp*" }` in PowerShell
4. **Test Elements**: Use simple `click` or `type` commands before complex `interact` operations
5. **Review Logs**: Check console output for detailed error messages and validation results

For more help, use `clif --help` or `clif <command> --help` for specific commands.
