# CLI guide

This guide describes the current `CLIF.exe` command-line contract. All CLI
commands target a Windows desktop process with an accessible main window.

## Install or build

For a release, extract the matching self-contained archive from the
[releases page](https://github.com/cmalpass/clif/releases) and put the directory
on `PATH`. For a source checkout:

```powershell
dotnet restore .\clif.sln --locked-mode
dotnet build .\CLIF\CLIF.csproj --configuration Release --no-restore
$clif = (Resolve-Path .\CLIF\bin\Release\net8.0-windows\CLIF.exe).Path
& $clif --help
```

`dotnet run` is also supported:

```powershell
dotnet run --project .\CLIF\CLIF.csproj -- --help
```

The `--` separates `dotnet run` options from CLIF options.

## First workflow against TestWpfApp

Start the fixture from the repository root:

```powershell
dotnet build .\TestWpfApp\TestWpfApp.csproj --configuration Release
$wpf = (Resolve-Path .\TestWpfApp\bin\Release\net8.0-windows\TestWpfApp.exe).Path
Start-Process -FilePath $wpf
```

Find its PID. `list-processes` reports desktop processes with an accessible
main window, not every process on the machine:

```powershell
& $clif list-processes --detailed
$targetPid = (Get-Process TestWpfApp | Select-Object -First 1 -ExpandProperty Id)
```

Inspect before changing state:

```powershell
& $clif tree $targetPid --depth 5 --show-selectors
& $clif tree $targetPid --search id:TestTextBox
& $clif tree $targetPid --control-type Button --visible-only
```

Use a selector from the tree or a stable fixture ID:

```powershell
& $clif type --process-id $targetPid --element "id=TestTextBox" --text "Hello from the CLI"
& $clif click --process-id $targetPid --element "id=TestButton"
& $clif interact --process-id $targetPid --element "id=TestComboBox" --control-type combobox --action select --value "Item 2"
```

The standalone `click`, `type`, and `interact` commands require a PID. The
`attach` and `tree` commands accept a PID, process name, or window-title text.

## Commands

Run `& $clif <command> --help` for the executable's authoritative help. The
current command surface is:

| Command | Purpose | Required target |
| --- | --- | --- |
| `list-processes` | List processes with an accessible main window; `--format table\|json\|csv`, `--detailed` | none |
| `attach <process>` | Attach by PID, process name, or window title; optionally run one action or `--interactive` | process argument |
| `tree <process>` | Print, search, filter, and optionally save the automation tree | process argument |
| `click` | Invoke/toggle/select or mouse-click an element | `--process-id`, `--element` |
| `type` | Type text into an element | `--process-id`, `--element`, `--text` |
| `interact` | Use a control-specific operation | `--process-id`, `--element`, `--control-type`, `--action` |
| `script <script-file>` | Execute a JSON automation script | JSON file; optional `--process-id` override |
| `interactive` | Start a prompt-driven session | optional `--process-id` |

### Discovery and output

```powershell
& $clif list-processes
& $clif list-processes --detailed --format json | Set-Content .\processes.json
& $clif tree $targetPid --depth 8 --show-properties --show-selectors --output .\tree.txt
```

`tree` supports `--depth`, `--enabled-only`, `--visible-only`,
`--show-properties`, `--show-selectors`, `--search`, `--control-type`, and
`--output`. Search values may be plain text (name search) or prefixed with
`name:`, `id:`, `class:`, or `type:`. When `--search` or `--control-type` is
present, CLIF emits matching elements rather than the complete tree.

### Selectors

Selectors are case-insensitive key/value clauses:

```text
id=TestTextBox
name="Click Me"
class=TextBox
type=Button
id=TestTextBox and type=Edit
```

A selector without `=` is treated as a name selector. Quote values containing
spaces, `and`, quotes, or backslashes. Prefer `id=` when the target exposes a
stable AutomationId; use `tree --show-selectors` to copy the generated form.

### Control-specific interactions

`interact` uses `--value` for text, numbers, booleans, or dates and `--index` for
index-based selection. The implemented control/action combinations are:

| `--control-type` | Common actions |
| --- | --- |
| `combobox`, `listbox` | `select --value`, `select --index`, `get-items` |
| `checkbox` | `set --value true\|false`, `toggle`, `get` |
| `radiobutton` | `select`, `get` |
| `slider` | `set --value <number>`, `get` |
| `tab` | `select --value <name>`, `select --index <n>`, `get` |
| `tree` | `expand`, `collapse`, `select --value <name>` |
| `datepicker` | `set --value yyyy-MM-dd`, `get` |
| `calendar` | `select --value yyyy-MM-dd`, `get` |
| `expander` | `toggle`, `get` |
| `datagrid` | `select-row --index`, `get-data`, `set-checkbox`, `set-checkbox-by-name`, `toggle-checkbox`, `get-checkbox-states`, `uncheck-all` |
| `menu` | `invoke` |
| `togglebutton` | `set --value true\|false`, `toggle`, `get` |

For example:

```powershell
& $clif interact --process-id $targetPid --element "id=TestSlider" --control-type slider --action set --value 80
& $clif interact --process-id $targetPid --element "id=TestExpander" --control-type expander --action toggle
& $clif interact --process-id $targetPid --element "id=TestDataGrid" --control-type datagrid --action select-row --index 1
```

### Attach and interactive mode

`attach` is convenient for one-off actions or switching into the prompt:

```powershell
& $clif attach $targetPid
& $clif attach $targetPid --action get-text --element "id=StatusTextBlock"
& $clif attach $targetPid --interactive
```

Inside `interactive`, type `help`. Supported commands are `attach`, `click`,
`type`, `get-text`, `get-value`, `tree [depth]`, `search`, `screenshot [file]`,
`help`, and `exit`. Its parser respects double-quoted text but is intentionally
simple; use JSON scripts when quoting or repeatability matters.

## Scripts

The script command loads a JSON file, resolves its target, attaches once, and
executes ordered steps. A process ID passed on the command line overrides the
script's target:

```powershell
& $clif script .\examples\getting-started-wpf.json --process-id $targetPid
```

Use the repository validator before execution:

```powershell
.\tools\validate-examples.ps1 -InputPath .\examples\getting-started-wpf.json -CanonicalOnly
```

The CLI has no `script --validate` option. `ValidateScriptAsync` exists in the
service layer for tests and host integrations; it is not exposed as a CLI
subcommand. JSON-only checks can also be done with:

```powershell
Get-Content .\my-script.json -Raw | ConvertFrom-Json | Out-Null
```

### Script schema

```json
{
  "name": "Small WPF workflow",
  "description": "A repeatable example",
  "version": "1.0",
  "target": { "processName": "TestWpfApp", "windowTitle": "", "processId": 0, "timeoutMs": 30000 },
  "options": { "stopOnError": true, "logSteps": true, "delayBetweenActionsMs": 300 },
  "steps": [
    { "action": "clear", "element": "id=TestTextBox" },
    { "action": "type", "element": "id=TestTextBox", "value": "Hello" },
    { "action": "validate", "element": "id=TestTextBox", "value": "Hello" },
    { "action": "click", "element": "id=TestButton" }
  ]
}
```

Target resolution uses the first matching process by explicit override, process
ID, process name, or window-title substring. A target is required; CLIF never
falls back to an arbitrary foreground process.

Implemented step actions are `log`, `wait`, `screenshot`, `clear`, `type`,
`click`, `focus`, `select`, `setvalue`, `selecttab`, `selectrow`, `selectcell`,
`expand`, `collapse`, `getvalue`, `getselection`, `getstate`, and `validate`.
`selectrow` and `selectcell` use `parameters.index`, `parameters.row`, and
`parameters.column` (or the step's `row`/`column` fields). `validate` polls the
UIA value briefly to avoid racing a WPF dispatcher update.

`stopOnError` and `delayBetweenActionsMs` affect execution. `delayMs` on a step
overrides the inter-step delay, and `wait` owns its complete delay. The model
also accepts legacy aliases such as `targetProcess`, `selector`, `text`,
`expectedValue`, `duration`, `waitAfter`, `message`, `timeout`, and
`continueOnError` so older repository examples remain loadable; new scripts
should use the canonical fields above. Variables are parsed as metadata but are
not interpolated by the current executor.

### Exit codes and capture

The `script` command returns exit code `0` only when the script succeeds and
returns `1` for a missing file, attachment failure, parse/execution failure, or
failed step. Individual command failures also return non-zero where supported.

CLI interactions write session folders under `.\sessions\`. Each folder can
contain `session.log` and PNGs under `screenshots\`. Treat screenshots and logs
as potentially sensitive desktop data and remove or redact them before sharing.

## Common failure modes

- No process listed: confirm the app has a visible main window in the same
  interactive desktop session.
- Attach fails: run CLIF at the same elevation/integrity level as the target.
- Element not found: rerun `tree`, prefer `id=`, and check that the control is
  visible and enabled.
- No visible effect: bring the target to the foreground, unlock the desktop,
  and inspect the session screenshot/log.
- Script succeeds but a value is stale: add a `validate` step after the action
  and use a small `delayMs` only when the application needs settling time.
