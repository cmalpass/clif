# MCP production-readiness review

Reviewed: 2026-08-31

## Implemented migration controls

- The server runs on the official `ModelContextProtocol` C# SDK 2.2.0.
- The SDK negotiates legacy initialize sessions and modern `2026-07-28`
  stateless requests. Modern `server/discover`, `tools/list`, cache metadata,
  result envelopes, and protocol errors are covered by executable black-box
  tests.
- Tool definitions are registered through an explicit CLIF adapter, sorted by
  ordinal name, and annotated from the existing capability policy.
- Tool calls are serialized through a cancellable UI dispatcher. The stdio
  cancellation notification is exercised against a real long-running batch.
- Window and element references are bounded canonical handles. Graceful
  process close escalates to process-tree termination to prevent orphaned
  automation processes.
- Release publishing restores each target RID before `--no-restore` publish;
  both `win-x64` and `win-arm64` self-contained MCP publishes were verified
  locally.

## Validation evidence

The final local gate was run sequentially on Windows:

```text
dotnet restore CLIF.Mcp/CLIF.Mcp.csproj --locked-mode
dotnet restore CLIF.Mcp.Tests/CLIF.Mcp.Tests.csproj --locked-mode
dotnet build clif.sln -c Release --no-restore -t:Rebuild -v:minimal
dotnet test CLIF.Mcp.Tests/CLIF.Mcp.Tests.csproj -c Release --no-restore
dotnet test CLIF.Tests/CLIF.Tests.csproj -c Release --no-build --filter Category=McpUI
dotnet list CLIF.Mcp/CLIF.Mcp.csproj package --vulnerable --include-transitive
```

Observed results: solution rebuild succeeded with 0 warnings/0 errors; 150 MCP
unit tests passed; 6 MCP UI/modern protocol tests passed; and no vulnerable
packages were reported. `git diff --check` was clean. The full all-tools WPF
test remains part of the MCP UI filter.

## Release gates still external to this repository

1. Hosted GitHub Actions runs currently fail before executing jobs because the
   account billing/spend-limit gate is unavailable. A maintainer must restore
   GitHub Actions billing and rerun Build and Test, CodeQL, and release checks.
2. Published binaries are currently unsigned. Production distribution should
   add a trusted Windows code-signing certificate, timestamped signatures, and
   signature verification to the release workflow.
3. The repository is Windows-only for MCP desktop automation by design. A
   Windows interactive-session smoke test is required for every release; the
   local WPF fixture provides that coverage but cannot replace deployment-host
   validation.

These are release-owner/environment gates, not unvalidated protocol behavior.
