# CLIF MCP Server

An Anthropic-style Model Context Protocol (MCP) server for [CLIF](https://github.com/clif-tool/clif), a .NET CLI tool for automating WPF applications.

This MCP server allows LLMs (like Claude) to interact with WPF applications on Windows by wrapping the CLIF CLI tool.

## Features

- **List Processes**: Find running WPF applications.
- **Inspect UI Tree**: View the element hierarchy of a target application.
- **Interact**: Click, type, select, toggle, and more on UI elements.
- **Session Capture**: Returns text output and screenshots (before/after action) to the LLM.
- **Scripting**: Execute CLIF automation scripts.

## Prerequisites

- **Windows OS**: CLIF and UI Automation require Windows.
- **Node.js**: Version 16 or higher.
- **CLIF**: The `clif` tool must be built and accessible.

## Installation

1.  **Clone the repository** (if not already done).
2.  **Build CLIF**:
    Follow the instructions in the root [README.md](../README.md) to build `clif`.
    ```powershell
    dotnet build -c Release
    ```
    Ensure the `clif` executable is in your PATH, or note its location.

3.  **Install MCP Server Dependencies**:
    ```bash
    cd clif-mcp
    npm install
    npm run build
    ```

## Configuration

The server needs to know where the `clif` executable is. You can set this via the `CLIF_PATH` environment variable.

## Usage with Claude Desktop

To use this with Claude Desktop, add the following to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "clif": {
      "command": "node",
      "args": [
        "C:/path/to/repo/clif-mcp/dist/index.js"
      ],
      "env": {
        "CLIF_PATH": "C:/path/to/repo/clif/bin/Release/net8.0/clif.exe"
      }
    }
  }
}
```

*Note: Replace paths with the actual absolute paths on your system.*

## Available Tools

- `list_processes`: Lists available WPF processes.
- `tree`: Shows the UI element tree for a process.
- `click`: Clicks a UI element.
- `type`: Types text into a UI element.
- `interact`: Performs advanced interactions (combobox selection, grid operations, etc.).
- `script`: Runs a pre-defined automation script.

## Development

To build the server:
```bash
npm run build
```

To run it locally for testing (uses stdin/stdout):
```bash
node dist/index.js
```
