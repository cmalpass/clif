# Examples and scripting

The JSON files in `examples/` are CLI scripts for the Windows `TestWpfApp`
fixture unless noted otherwise. They intentionally use stable selectors such as
`id=TestTextBox` and visible state changes so a person can watch the app while a
script runs.

## Before running an example

```powershell
dotnet build .\TestWpfApp\TestWpfApp.csproj --configuration Release
$wpf = (Resolve-Path .\TestWpfApp\bin\Release\net8.0-windows\TestWpfApp.exe).Path
Start-Process -FilePath $wpf
$clif = (Resolve-Path .\CLIF\bin\Release\net8.0-windows\CLIF.exe).Path
$targetPid = (Get-Process TestWpfApp | Select-Object -First 1 -ExpandProperty Id)
```

Validate JSON and the supported script shape without touching the app:

```powershell
.\tools\validate-examples.ps1 -InputPath .\examples\getting-started-wpf.json -CanonicalOnly
```

Run the curated getting-started script:

```powershell
& $clif script .\examples\getting-started-wpf.json --process-id $targetPid
```

The script should clear and fill the text box, validate the value, click the
main button, select `Item 2`, set the slider to `42`, and capture a final
screenshot. The process remains open so you can inspect the result.

## Curated examples

| File | What it demonstrates |
| --- | --- |
| `getting-started-wpf.json` | Small canonical workflow for first-time users |
| `quick-wpf-test.json` | Core controls, screenshots, tabs, and a data-grid row |
| `data-entry-example.json` | Focus, clear, type, validate, and submit |
| `test-advanced-controls.json` | Advanced WPF controls |
| `comprehensive-wpf-test.json` | Broad control coverage |
| `validation-test.json` | Legacy-field compatibility plus visible validation steps |
| `session-capture-demo.json` | Logging, waits, screenshots, and session output |
| `visual-demo-test.json` | Deliberately slowed visible interactions |

The remaining JSON files cover narrower regressions and control-specific
experiments. Some are legacy/manual payloads rather than current `script`
inputs. Validate an individual file before running it; the validator reports
whether it is canonical or relies on the CLI loader's legacy aliases.

## Authoring a script

Start with this canonical shape:

```json
{
  "name": "My WPF workflow",
  "description": "A self-checking example",
  "version": "1.0",
  "target": { "processName": "TestWpfApp", "timeoutMs": 30000 },
  "options": {
    "stopOnError": true,
    "logSteps": true,
    "delayBetweenActionsMs": 300
  },
  "steps": [
    { "action": "clear", "element": "id=TestTextBox" },
    { "action": "type", "element": "id=TestTextBox", "value": "Scripted value" },
    { "action": "validate", "element": "id=TestTextBox", "value": "Scripted value" },
    { "action": "click", "element": "id=TestButton", "delayMs": 500 }
  ]
}
```

Use `parameters` for row and cell coordinates:

```json
{
  "action": "selectrow",
  "element": "id=TestDataGrid",
  "parameters": { "index": 1 },
  "description": "Select the second data row"
}
```

Use `wait` for an explicit pause and `validate` for a value assertion. A delay
is not an assertion. For selector discovery, first run `tree` and copy an
`id=`, `name=`, `class=`, or `type=` selector.

## Sessions and artifacts

CLI commands that perform automation create timestamped folders under
`sessions/`. The folder contains `session.log` and screenshots when the
underlying operation captures them. Keep these artifacts local if they contain
user data, passwords, or other desktop content. The repository's `.gitignore`
excludes session output.

## Cross-platform fixture

`TestCrossPlatformApp` is an Avalonia contract fixture and can run on Windows,
macOS, and Linux:

```powershell
dotnet run --project .\TestCrossPlatformApp\TestCrossPlatformApp.csproj
dotnet test .\TestCrossPlatformApp.Tests\TestCrossPlatformApp.Tests.csproj
```

CLIF's FlaUI/UIA3 backend remains Windows-only. On Windows, the dedicated
Avalonia UIA smoke tests are:

```powershell
dotnet test .\CLIF.Tests\CLIF.Tests.csproj --configuration Release --filter "Category=CrossPlatformUI"
```
