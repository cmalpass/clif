# MCP server guide

CLIF.Mcp is a local, Windows-only MCP server that communicates over standard
input/output. It is designed for a trusted AI harness running on the same
interactive desktop as the target application. It is not a hosted endpoint and
does not expose a network transport.

## Build and smoke-test the server

From the repository root:

```powershell
dotnet restore .\clif.sln --locked-mode
dotnet build .\CLIF.Mcp\CLIF.Mcp.csproj --configuration Release --no-restore
```

The source command is:

```powershell
dotnet run --project .\CLIF.Mcp\CLIF.Mcp.csproj
```

For a published self-contained deployment:

```powershell
dotnet publish .\CLIF.Mcp\CLIF.Mcp.csproj --configuration Release --output .\publish\CLIF.Mcp
```

Use the absolute path `C:\...\publish\CLIF.Mcp\CLIF.Mcp.exe` in the harness
configuration. GUI hosts often choose a working directory other than the repo.

## Permission model

The server reads its policy once, when the process starts. The default is
deny-by-default:

| Variable | Default | Grants |
| --- | --- | --- |
| `CLIF_MCP_ALLOWED_APPS` | empty | Exact executable names or canonical paths allowed for `clif_launch`; separate entries with `;` |
| `CLIF_MCP_ALLOW_INPUT` | `false` | Keyboard, mouse, and control mutations; also required for focus |
| `CLIF_MCP_ALLOW_WINDOW_ENUMERATION` | `false` | `clif_list_windows` |
| `CLIF_MCP_ALLOW_WINDOW_CLOSE` | `false` | `clif_close` |
| `CLIF_MCP_ALLOW_FULL_SCREEN_CAPTURE` | `false` | `clif_screenshot` with `fullScreen: true` |
| `CLIF_MCP_LOG_LEVEL` | normal diagnostics | Set to `off` for no lifecycle diagnostics on stderr |

For a fixture-only session:

```powershell
$wpf = (Resolve-Path .\TestWpfApp\bin\Release\net8.0-windows\TestWpfApp.exe).Path
$env:CLIF_MCP_ALLOWED_APPS = $wpf
$env:CLIF_MCP_ALLOW_INPUT = "true"
dotnet run --project .\CLIF.Mcp\CLIF.Mcp.csproj
```

When configured in a harness, put these values in that server's `env` object so
they are applied to the spawned process. Do not allow broad paths or unrelated
applications. These environment variables are application policy, not a
security boundary for an untrusted agent.

## Harness configuration

All configurations below use the same source command. Replace the path with an
absolute path on the machine hosting the MCP process, and use the published EXE
when the host cannot locate `dotnet`.

### Claude Desktop

Edit Claude Desktop's `claude_desktop_config.json` and restart Claude Desktop.
The portable local-server shape uses `mcpServers`:

```json
{
  "mcpServers": {
    "clif": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Users\\you\\src\\clif\\CLIF.Mcp\\CLIF.Mcp.csproj", "--no-launch-profile"],
      "env": {
        "CLIF_MCP_ALLOWED_APPS": "C:\\Users\\you\\src\\clif\\TestWpfApp\\bin\\Release\\net8.0-windows\\TestWpfApp.exe",
        "CLIF_MCP_ALLOW_INPUT": "true"
      }
    }
  }
}
```

### VS Code and GitHub Copilot in VS Code

Create `.vscode/mcp.json` in the workspace or use the user MCP configuration.
VS Code uses a top-level `servers` object and `type: "stdio"`:

```json
{
  "servers": {
    "clif": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Users\\you\\src\\clif\\CLIF.Mcp\\CLIF.Mcp.csproj", "--no-launch-profile"],
      "cwd": "C:\\Users\\you\\src\\clif",
      "env": {
        "CLIF_MCP_ALLOWED_APPS": "C:\\Users\\you\\src\\clif\\TestWpfApp\\bin\\Release\\net8.0-windows\\TestWpfApp.exe",
        "CLIF_MCP_ALLOW_INPUT": "true"
      }
    }
  }
}
```

Use **MCP: List Servers** and **Show Output** to inspect startup failures. VS
Code may ask you to trust a new local server. See the current
[VS Code MCP configuration reference](https://code.visualstudio.com/docs/agents/reference/mcp-configuration).

### Gemini CLI

Add the server to `%USERPROFILE%\.gemini\settings.json` or the project
`.gemini\settings.json`. Gemini CLI uses `mcpServers` and discovers the tools
when it starts:

```json
{
  "mcpServers": {
    "clif": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\Users\\you\\src\\clif\\CLIF.Mcp\\CLIF.Mcp.csproj", "--no-launch-profile"],
      "cwd": "C:\\Users\\you\\src\\clif",
      "env": {
        "CLIF_MCP_ALLOWED_APPS": "C:\\Users\\you\\src\\clif\\TestWpfApp\\bin\\Release\\net8.0-windows\\TestWpfApp.exe",
        "CLIF_MCP_ALLOW_INPUT": "true"
      }
    }
  }
}
```

Run `gemini mcp list` to check the connection. The server alias `clif` avoids
underscores in Gemini's generated tool names. See Gemini CLI's
[MCP reference](https://github.com/google-gemini/gemini-cli/blob/main/docs/tools/mcp-server.md)
for current command and settings-file details.

### Cursor and other portable stdio hosts

Hosts that use the portable `mcpServers` format can use this minimal shape;
Cursor commonly stores it in a project or user MCP JSON file:

```json
{
  "mcpServers": {
    "clif": {
      "command": "C:\\Tools\\CLIF\\CLIF.Mcp.exe",
      "env": {
        "CLIF_MCP_ALLOWED_APPS": "C:\\Tools\\TestWpfApp\\TestWpfApp.exe",
        "CLIF_MCP_ALLOW_INPUT": "true"
      }
    }
  }
}
```

If a host has a different top-level key, keep the server entry's command,
arguments, working directory, and environment values but follow that host's
schema. The server must be launched as a long-lived stdio child process.

## Tool workflow

The safe, deterministic sequence is:

1. Launch an allow-listed app with `clif_launch`, or obtain a registered handle
   with `clif_list_windows` when enumeration is explicitly enabled.
2. Inspect that handle with `clif_snapshot` or narrow it with
   `clif_search_elements`.
3. Use the returned session-scoped element refs with `clif_click`, `clif_type`,
   `clif_fill`, `clif_interact`, or `clif_batch`.
4. Verify with `clif_get_text`, a fresh `clif_snapshot`, or `clif_screenshot`.
5. Close only a window owned by the workflow, and only when window close is
   explicitly enabled.

Window handles and element refs are invalid after a server restart, window
close, or a stale-provider recovery. Never substitute the foreground window.
After an invalid-ref error, snapshot again and use fresh refs. `clif_batch` has
at most 25 actions and a 30-second maximum duration.

## Example against TestWpfApp

After starting `TestWpfApp.exe`, ask the harness to perform this sequence. The
launch response supplies a handle such as `w1`; the search response supplies a
fresh ref such as `w1e12`. The suffixes are illustrative and must not be copied
between sessions.

```text
clif_launch
{ "app": "C:\\Users\\you\\src\\clif\\TestWpfApp\\bin\\Release\\net8.0-windows\\TestWpfApp.exe" }

clif_snapshot
{ "handle": "w1" }

clif_search_elements
{ "handle": "w1", "automationId": "TestTextBox" }

clif_fill
{ "ref": "w1e12", "value": "Hello from MCP" }

clif_click
{ "ref": "w1e13" }

clif_get_text
{ "ref": "w1e12" }

clif_screenshot
{ "handle": "w1" }
```

For multiple deterministic input steps, `clif_batch` accepts `click`, `type`,
`fill`, `wait`, and `snapshot` actions. `type` appends; `fill` replaces the
current value:

```json
{
  "actions": [
    { "action": "fill", "ref": "w1e12", "value": "Batch value" },
    { "action": "click", "ref": "w1e13" },
    { "action": "wait", "ms": 250 },
    { "action": "snapshot", "handle": "w1" }
  ],
  "stopOnError": true
}
```

`clif_validate_script` validates inline CLIF JSON and never executes it. The
CLI is the execution path for JSON files. This division lets a harness validate
a generated script before deciding whether to perform equivalent MCP calls.

## Tool reference

| Tool | Inputs | Capability |
| --- | --- | --- |
| `clif_launch` | `app`, optional `args` | allow-listed launch |
| `clif_snapshot` | `handle` | read-only |
| `clif_search_elements` | `handle`, optional `name`, `automationId`, `controlType`, `className` | read-only |
| `clif_get_text` | `ref` | read-only |
| `clif_screenshot` | `ref` or `handle`; optional `fullScreen` | read-only; full screen separately gated |
| `clif_click` | `ref`; optional `button`, `doubleClick` | input |
| `clif_type` | `ref`, `text`; optional `submit` | input |
| `clif_fill` | `ref`, `value` | input |
| `clif_interact` | `ref`, `controlType`, `action`; optional `value`, `index`, `column` | input |
| `clif_batch` | `actions`; optional `stopOnError` | input |
| `clif_focus` | `handle` | input policy |
| `clif_list_windows` | none | window enumeration |
| `clif_close` | `handle` | window close |
| `clif_validate_script` | inline `content` | read-only |

## Protocol and diagnostics

The server uses the official C# MCP SDK over stdio and supports the current
repository protocol contract (`2026-07-28`) while negotiating legacy clients
such as `2025-06-18`. Stdout is reserved for JSON-RPC. Lifecycle diagnostics are
one JSON object per line on stderr and omit typed values, script content,
screenshots, and command-line arguments by default.

Each tool call has a 30-second cooperative deadline. A synchronous native UIA
provider cannot always be forcibly interrupted; recover a stuck target at the
host/process level rather than sending retries against stale refs.

## Troubleshooting

- **Launch denied:** add the exact executable name or canonical path to
  `CLIF_MCP_ALLOWED_APPS` and restart the harness.
- **Input denied:** set `CLIF_MCP_ALLOW_INPUT=true` for that server process.
- **No tools discovered:** check that the host uses stdio, `dotnet` and the
  project path are available to the host, and stdout is not wrapped by logging.
- **Window not found:** launch the app through CLIF or enable enumeration only
  for a controlled local session.
- **Element ref not found:** snapshot again; refs are not stable identifiers.
- **No visible effect:** unlock the desktop, check elevation, and make sure the
  target is in the same interactive user session.
- **Stale tools in VS Code:** run **MCP: Reset Cached Tools**, then restart the
  server.
