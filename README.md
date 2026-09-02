# CLIF — Windows UI automation CLI and local MCP server

[![Build and Test](https://github.com/cmalpass/clif/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/cmalpass/clif/actions/workflows/build-and-test.yml)
[![CodeQL](https://github.com/cmalpass/clif/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/cmalpass/clif/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/github/license/cmalpass/clif)](LICENSE)

CLIF is a Windows desktop UI automation toolkit for WPF, WinForms, WinUI, and
other applications that expose Windows UI Automation. It has two entry points:

- a command-line client for repeatable, scriptable workflows;
- a local Model Context Protocol (MCP) server for trusted AI harnesses.

Use CLIF from PowerShell, interactive test sessions, JSON automation
scripts, or an MCP-compatible desktop agent. It is built on FlaUI and UIA3,
with a local-first permission model for launching applications and sending
input.

CLIF uses FlaUI and Windows UI Automation. The production automation backend
is Windows-only and requires an unlocked, interactive desktop session. The
repository's Avalonia app is a portable fixture for contract tests; it is not a
cross-platform CLIF backend.

> CLIF is under active development. The MCP server is intended for trusted local
> development sessions. Read [SECURITY.md](SECURITY.md) before granting an agent
> permission to launch applications or send input.

## Start here

| Goal | Guide |
| --- | --- |
| Install, build, and run the CLI | [CLI guide](docs/cli.md) |
| Configure Claude Desktop, VS Code/Copilot, Gemini CLI, Cursor, or another stdio host | [MCP guide](docs/mcp.md) |
| Run and validate the included JSON scripts | [Examples guide](docs/examples.md) |
| See a real CLI + WPF interaction | [Realtime demo](docs/realtime-demo.md) |
| Understand support and release constraints | [User guide](docs/user-guide.md) |

## Choose the right entry point

| If you need to… | Start with | Best for |
| --- | --- | --- |
| Inspect controls or run a repeatable workflow | [CLI guide](docs/cli.md) | PowerShell, scripts, test harnesses, and debugging |
| Let a desktop AI client inspect and operate an app | [MCP guide](docs/mcp.md) | Claude Desktop, VS Code/Copilot, Gemini CLI, Cursor, and other stdio hosts |
| Learn the script format by running a fixture | [Examples](docs/examples.md) | WPF automation, validation, screenshots, and regression samples |
| See the interaction in real time | [Realtime demo](docs/realtime-demo.md) | A side-by-side CLI and WPF walkthrough |

## Why CLIF

- **Discoverable controls:** inspect a target’s UI Automation tree and copy
  stable selectors before changing state.
- **Repeatable automation:** keep workflows in reviewable JSON with explicit
  targets, ordered actions, waits, and validation steps.
- **Agent-ready local MCP:** expose narrowly scoped desktop tools over stdio,
  with application, input, enumeration, close, and screenshot permissions
  controlled per server process.
- **Runnable fixtures:** use the included WPF app and examples without needing
  a separate application under test.

## Requirements

- Windows 10 or 11 for CLIF, `TestWpfApp`, and UI Automation tests.
- .NET SDK 8.0.424, selected by [global.json](global.json).
- An interactive, unlocked desktop. CLIF cannot reliably attach across user
  sessions or integrity levels.

## Install or build

For the quickest start, download a Windows archive from the
[GitHub Releases page](https://github.com/cmalpass/clif/releases), extract it,
and place the CLI directory on `PATH`. The MCP guide explains when to use the
published server executable instead of `dotnet run`.

```powershell
dotnet restore .\clif.sln --locked-mode
dotnet build .\clif.sln --configuration Release --no-restore
```

To run the CLI from the checkout:

```powershell
dotnet run --project .\CLIF\CLIF.csproj -- --help
```

Release checksums and contents are documented in
[RELEASE_ARTIFACTS.md](RELEASE_ARTIFACTS.md).

## Five-minute CLI check

Build and start the WPF fixture in one PowerShell window:

```powershell
dotnet build .\TestWpfApp\TestWpfApp.csproj --configuration Release
$wpf = (Resolve-Path .\TestWpfApp\bin\Release\net8.0-windows\TestWpfApp.exe).Path
Start-Process -FilePath $wpf
```

In another window, identify the process and inspect its automation tree:

```powershell
dotnet run --project .\CLIF\CLIF.csproj -- list-processes --detailed
dotnet run --project .\CLIF\CLIF.csproj -- tree <PID> --depth 5
```

Then use a selector from the tree (the fixture has stable IDs):

```powershell
dotnet run --project .\CLIF\CLIF.csproj -- type --process-id <PID> --element "id=TestTextBox" --text "Hello from CLIF"
dotnet run --project .\CLIF\CLIF.csproj -- click --process-id <PID> --element "id=TestButton"
```

For a repeatable workflow, run
`examples/getting-started-wpf.json`; the complete script catalog and validator
are in [docs/examples.md](docs/examples.md).

## Five-minute MCP check

Build the server, configure a stdio-compatible MCP host with an absolute path,
and grant only the WPF fixture for the session. The exact recipes and a
tool-by-tool workflow are in [docs/mcp.md](docs/mcp.md).

```powershell
dotnet build .\CLIF.Mcp\CLIF.Mcp.csproj --configuration Release
$env:CLIF_MCP_ALLOWED_APPS = (Resolve-Path .\TestWpfApp\bin\Release\net8.0-windows\TestWpfApp.exe).Path
$env:CLIF_MCP_ALLOW_INPUT = "true"
dotnet run --project .\CLIF.Mcp\CLIF.Mcp.csproj
```

The MCP server speaks JSON-RPC over stdout. Diagnostics go to stderr; wrappers
must not print banners or logs to stdout.

## Test

```powershell
dotnet test .\CLIF.Tests\CLIF.Tests.csproj --configuration Release --filter "Category=Integration"
dotnet test .\CLIF.Mcp.Tests\CLIF.Mcp.Tests.csproj --configuration Release
dotnet test .\TestCrossPlatformApp.Tests\TestCrossPlatformApp.Tests.csproj --configuration Release
.\tools\validate-examples.ps1 -InputPath .\examples\getting-started-wpf.json -CanonicalOnly
```

The full CI matrix, including interactive WPF/MCP UI tests, is described in
[CLIF.Tests/TESTING_GUIDE.md](CLIF.Tests/TESTING_GUIDE.md).

## Project map

- `CLIF/` — Windows CLI, selectors, scripts, and session capture.
- `CLIF.Mcp/` — local stdio MCP server and safety policy.
- `TestWpfApp/` — primary WPF automation fixture.
- `TestCrossPlatformApp/` — Avalonia contract-test fixture only.
- `examples/` — runnable CLI JSON scripts.
- `docs/` — task-oriented documentation and recorded interaction media.

## License

CLIF is released under the MIT License. See [LICENSE](LICENSE).
