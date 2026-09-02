# CLIF user guide

CLIF controls Windows desktop applications through FlaUI and Windows UI
Automation. Use the task guides below for the current command and protocol
contracts.

## Choose a path

- [CLI guide](cli.md) — install, discover windows and selectors, use every CLI
  command, author JSON scripts, inspect exit codes, and troubleshoot.
- [MCP guide](mcp.md) — build the local stdio server, configure common AI
  harnesses, apply the permission policy, and run a tool workflow.
- [Examples and scripting](examples.md) — run the canonical WPF example,
  validate JSON, understand compatibility aliases, and use the portable fixture.
- [Realtime CLI + WPF demo](realtime-demo.md) — watch the CLI window drive the
  WPF fixture.

## Support boundary

| Component | Supported environment | Purpose |
| --- | --- | --- |
| `CLIF.exe` | Windows 10/11, interactive desktop | UI automation of desktop applications that expose UI Automation |
| `CLIF.Mcp.exe` | Windows 10/11, interactive desktop | Local MCP stdio server for trusted AI clients |
| `TestWpfApp` | Windows | Primary WPF integration fixture |
| `TestCrossPlatformApp` | Windows, macOS, Linux | Avalonia contract-test fixture only |

Source builds use the pinned .NET SDK 8.0.424 and the Windows Desktop workload.
Release archives are self-contained. Services, locked desktops, elevated
targets, and applications running under a different user can prevent UI
Automation from attaching or bringing a window to the foreground.

## Validation

From a Windows development checkout:

```powershell
dotnet restore .\clif.sln --locked-mode
dotnet build .\clif.sln --configuration Release --no-restore
dotnet test .\CLIF.Mcp.Tests\CLIF.Mcp.Tests.csproj --configuration Release --no-build
.\tools\validate-examples.ps1 -InputPath .\examples\getting-started-wpf.json -CanonicalOnly
```

The complete test matrix is in [CLIF.Tests/TESTING_GUIDE.md](../CLIF.Tests/TESTING_GUIDE.md).
The MCP-specific readiness record is in
[mcp-production-readiness.md](mcp-production-readiness.md).
