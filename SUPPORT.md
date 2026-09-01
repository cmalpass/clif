# Support

## Getting help

Start with the [README](README.md) and the fixture documentation in `TestWpfApp` and `TestCrossPlatformApp`. For reproducible defects, use the bug-report issue template. For feature ideas, use the feature-request template.

Before filing an issue, include the CLIF commit/version, Windows and .NET versions, target application framework, and a minimal reproduction. Remove credentials, personal data, and unrelated screenshot content.

## What support covers

CLIF is a Windows-only desktop automation product. The supported automation
backend is Microsoft Windows UI Automation through FlaUI/UIA3, and the CLI and
MCP server require a Windows desktop/session. WPF is the primary supported
application framework; Avalonia has provider-compatibility coverage on Windows.

The `TestCrossPlatformApp` Avalonia project is a portable fixture used to keep
the control/selector contract testable on Windows, macOS, and Linux. It is not a
cross-platform CLIF backend, and running it on macOS or Linux does not imply
that CLIF automation is supported there.

| Component | Supported platform | Purpose |
| --- | --- | --- |
| `CLIF` CLI | Windows 10/11 desktop session | Production WPF/UIA automation |
| `CLIF.Mcp` | Windows 10/11 desktop session | Local stdio MCP server controlling Windows apps |
| `TestWpfApp` | Windows | Primary WPF integration fixture |
| `TestCrossPlatformApp` | Windows, macOS, Linux | Portable contract fixture only |

Release artifacts currently target `win-x64` and `win-arm64`. x64 is the
primary validated target; ARM64 should be treated as experimental until it has
an equivalent interactive UI smoke-test environment.

The MCP server is currently suitable only for trusted local development environments. It is not a managed service and does not carry an availability, response-time, or compatibility guarantee.

## Security issues

Do not open public issues for suspected vulnerabilities or sensitive automation behavior. Follow [SECURITY.md](SECURITY.md).
