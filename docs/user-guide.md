# CLIF user guide

This guide covers the supported Windows workflows for the CLIF command-line
client and the local CLIF MCP server. CLIF controls desktop applications through
Windows UI Automation; it does not provide a remote or cross-platform desktop
automation service.

## Support and prerequisites

| Component | Supported environment | Purpose |
| --- | --- | --- |
| `CLIF.exe` | Windows 10/11, interactive desktop session | CLI automation of WPF applications |
| `CLIF.Mcp.exe` | Windows 10/11, interactive desktop session | Local MCP stdio server for AI clients |
| `TestWpfApp` | Windows | WPF integration fixture |
| `TestCrossPlatformApp` | Windows, macOS, Linux | Portable contract-test fixture only |

Source builds require the pinned .NET SDK 8.0.424 and the Windows Desktop
workload. Published
release archives are self-contained and do not require a separate .NET runtime.
The first release target is `win-x64`; `win-arm64` is available but should be
validated on the deployment hardware before relying on it.

Desktop automation requires an interactive Windows user session. Services,
non-interactive scheduled tasks, locked desktops, elevated targets, and targets
running under a different user can prevent UI Automation from attaching or
bringing a window to the foreground.

## Install the CLI

### Use a published release

Download the matching `CLIF-<version>-win-x64.zip` (or `win-arm64`) archive from
the [GitHub Releases page](https://github.com/cmalpass/clif/releases). Extract it
to a dedicated directory such as `C:\Tools\CLIF` and run `CLIF.exe` from that
directory. The archive is self-contained; do not copy individual DLLs out of it.

Verify the download before use from PowerShell:

```powershell
Get-FileHash .\CLIF-<version>-win-x64.zip -Algorithm SHA256
Get-Content .\SHA256SUMS.txt
Get-Content .\CLIF-v<version>.release-manifest.json -Raw | ConvertFrom-Json
```

Compare the hash with the corresponding entry in `SHA256SUMS.txt`. The release
manifest records the source commit, archive names, sizes, and digests.

To use `clif` instead of an explicit path, add the extraction directory to the
user or system `PATH`, then open a new PowerShell window.

### Build from source

Run these commands from the repository root:

```powershell
dotnet restore clif.sln --locked-mode
dotnet build CLIF/CLIF.csproj --configuration Release
dotnet publish CLIF/CLIF.csproj --configuration Release --output .\publish\CLIF
```

The executable is `.\publish\CLIF\CLIF.exe`. You can also run the project directly:

```powershell
dotnet run --project .\CLIF\CLIF.csproj -- --help
```

## CLI quick start

List candidate WPF processes and note the PID:

```powershell
.\CLIF.exe list-processes --detailed
```

Inspect an application before mutating it:

```powershell
.\CLIF.exe tree <PID> --depth 5 --show-selectors
```

Use the selector shown by `tree` for an action:

```powershell
.\CLIF.exe click --process-id <PID> --element "id=MyButton"
.\CLIF.exe type --process-id <PID> --element "id=MyTextBox" --text "Hello"
```

For a connected workflow, `attach` resolves a process by PID, process name, or
window title and can perform one action or start interactive mode:

```powershell
.\CLIF.exe attach <PID>
.\CLIF.exe attach "My Window Title" --action get-text --element "id=StatusText"
.\CLIF.exe attach <PID> --interactive
```

Use `interact` for control-specific operations such as ComboBox selection,
DataGrid checkboxes, TreeView expansion, tabs, sliders, and expanders. Run
`.\CLIF.exe <command> --help` for the authoritative option list.

### Scripts

Execute a JSON script from the repository's `examples` directory or your own
file:

```powershell
.\CLIF.exe script .\examples\quick-wpf-test.json --process-id <PID>
```

The CLI currently validates and reports script errors while loading/executing a
script; there is no separate `script --validate` command. To check JSON syntax
without running automation, use PowerShell:

```powershell
Get-Content .\my-script.json -Raw | ConvertFrom-Json | Out-Null
```

Each command creates session output under `.\sessions\` when capture is enabled.
Review the session log and before/after screenshots when an action is
inconclusive.

## Run the MCP server

The MCP server communicates over stdio. Keep stdout reserved for MCP JSON-RPC;
diagnostics are written to stderr. Run from the repository root:

```powershell
dotnet run --project .\CLIF.Mcp\CLIF.Mcp.csproj
```

Or build/publish and point the MCP client at the compiled executable:

```powershell
dotnet publish .\CLIF.Mcp\CLIF.Mcp.csproj --configuration Release --output .\publish\CLIF.Mcp
```

The executable is `.\publish\CLIF.Mcp\CLIF.Mcp.exe`. If you are already in the
`CLIF.Mcp` directory, use `dotnet run`; `dotnet run --project CLIF.Mcp` would
look for a nonexistent nested project directory.

### Client configuration

Most local MCP clients accept a command plus arguments. Use an absolute project
or executable path because GUI clients often choose a working directory other
than the repository root:

```json
{
  "mcpServers": {
    "clif": {
      "type": "local",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Users\\you\\src\\clif\\CLIF.Mcp\\CLIF.Mcp.csproj"]
    }
  }
}
```

For a self-contained release, replace `command` with the absolute path to
`CLIF.Mcp.exe` and omit `args`:

```json
{
  "mcpServers": {
    "clif": {
      "type": "local",
      "command": "C:\\Tools\\CLIF\\CLIF.Mcp.exe"
    }
  }
}
```

Restart the client after changing the configuration. The server reads its
permissions once at process startup.

### Permission policy

The default policy is deny-by-default:

```text
CLIF_MCP_ALLOWED_APPS=C:\Path\To\TestWpfApp.exe;calc.exe
CLIF_MCP_ALLOW_INPUT=true
CLIF_MCP_ALLOW_WINDOW_ENUMERATION=false
CLIF_MCP_ALLOW_WINDOW_CLOSE=false
CLIF_MCP_ALLOW_FULL_SCREEN_CAPTURE=false
```

`CLIF_MCP_ALLOWED_APPS` is a semicolon-separated list of exact executable names
or canonical paths. Launch is denied when the list is empty. Input, focus,
window enumeration, close, and full-screen capture are independent capabilities;
enable only what the workflow needs. These variables are not a security boundary
for untrusted agents—run the server only for trusted local clients.

### MCP workflow

The normal flow is:

1. Call `clif_launch` for an allow-listed executable, or work with an existing
   registered window.
2. Call `clif_snapshot` and use the returned bounded element references.
3. Call `clif_click`, `clif_type`, `clif_fill`, `clif_interact`, or `clif_batch`.
4. Call `clif_get_text`, `clif_snapshot`, or `clif_screenshot` to verify the
   result.
5. Call `clif_close` when the workflow owns the launched window and close is
   explicitly allowed.

Element references are session-scoped handles. Do not persist them across a new
server process or after the corresponding window is closed. `clif_batch` accepts
at most 25 actions and is bounded by the server's safety limits.

`clif_snapshot` and `clif_search_elements` require a registered window handle;
they do not inspect the current foreground window. Get a handle through
`clif_launch` or, when `CLIF_MCP_ALLOW_WINDOW_ENUMERATION=true` is explicitly
granted, `clif_list_windows`. In `clif_batch`, `click`, `type`, and `fill`
actions require an element `ref`, while `snapshot` requires a window `handle`.
This prevents keyboard input or accessibility data collection from falling back
to an unrelated focused application.

The server implements the modern `2026-07-28` MCP contract and negotiates
legacy clients such as `2025-06-18`. The `clif_validate_script` tool validates
inline JSON only; it does not execute files or perform UI actions.

### Diagnostics and troubleshooting

Set `CLIF_MCP_LOG_LEVEL=off` for a quiet session. Otherwise, each diagnostic is a
single JSON object on stderr with a correlation ID, method, outcome, and
sanitized lifecycle metadata. Never parse diagnostics from stdout.

Common failures:

- **Launch denied:** add the exact executable name/path to
  `CLIF_MCP_ALLOWED_APPS`, then restart the MCP client.
- **Input or focus denied:** set `CLIF_MCP_ALLOW_INPUT=true` for the session.
- **Window not found:** confirm the application is running in the same desktop
  session and repeat `clif_snapshot` to obtain fresh handles.
- **Element reference invalid:** handles are not portable between windows or
  server restarts; snapshot again.
- **No visible effect:** unlock/foreground the desktop, check elevation levels,
  and inspect the target application's UI Automation support.
- **Protocol errors:** ensure the MCP client launches the server as a stdio
  process and that no wrapper writes banners or logs to stdout.

## Validate locally

From a Windows development checkout:

```powershell
dotnet restore clif.sln --locked-mode
dotnet build clif.sln --configuration Release --no-restore
dotnet test CLIF.Mcp.Tests/CLIF.Mcp.Tests.csproj --configuration Release --no-restore
```

The end-to-end UI tests require an interactive desktop and the repository test
fixtures. See [CLIF.Tests/TESTING_GUIDE.md](../CLIF.Tests/TESTING_GUIDE.md) for
the complete test matrix.
