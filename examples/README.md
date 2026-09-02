# Example scripts

These JSON files are CLI scripts for `TestWpfApp`. Start with
[`getting-started-wpf.json`](getting-started-wpf.json), then follow
[`docs/examples.md`](../docs/examples.md) for setup, validation, and the full
catalog.

The repository contains older regression payloads as well as current scripts.
Some older files intentionally exercise legacy field aliases or were written
for manual investigation and are not part of the canonical getting-started
path. Validate a specific script before running it:

```powershell
.\tools\validate-examples.ps1 -InputPath .\examples\getting-started-wpf.json -CanonicalOnly
```
