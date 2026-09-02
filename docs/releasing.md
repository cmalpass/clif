# Releasing CLIF

CLIF ships as self-contained Windows ZIP archives through a GitHub Release.
The release workflow is [`.github/workflows/release.yml`](../.github/workflows/release.yml).
It is intentionally tag-driven: a maintainer prepares and reviews a release
commit on `main`, then pushes an annotated `v<MAJOR>.<MINOR>.<PATCH>` tag.

## Before tagging

Open a release pull request that:

1. moves the completed entries from `Unreleased` into a dated section in
   [`CHANGELOG.md`](../CHANGELOG.md);
2. updates the single product version in
   [`Directory.Build.props`](../Directory.Build.props);
3. verifies that the version is valid SemVer 2 and that all intended changes
   are represented in the changelog;
4. runs the normal build, tests, and the local Windows interactive smoke test;
5. checks that the release documentation and examples still match the
   shipped commands.

The tag and `Directory.Build.props` version must match exactly. For example,
`v0.1.0` requires `<Version>0.1.0</Version>` and a dated `## [0.1.0] - YYYY-MM-DD`
heading. A tag whose changelog entry is still `Unreleased` is rejected.

## Create the release

After the release pull request is merged:

```powershell
git switch main
git pull --ff-only
git tag -a v0.1.0 -m "release: v0.1.0"
git push origin v0.1.0
```

The workflow then:

- runs the release test suite on Windows, including WPF UI and MCP end-to-end
  tests;
- publishes self-contained `win-x64` and `win-arm64` CLI and MCP binaries;
- includes `LICENSE`, `NOTICE.md`, and `GETTING_STARTED.txt` in every archive;
- creates SHA-256 checksums, a CycloneDX SBOM, and a release manifest;
- generates GitHub artifact attestations for the archives, SBOM, checksum file,
  and manifest; and
- creates a **draft** GitHub Release with generated notes.

The workflow does not publish the draft automatically. This gives the release
owner a final review point for filenames, notes, checksums, and the attached
attestations.

## Review and publish manually

In the draft release, verify:

- the tag points to the intended `main` commit;
- all four ZIPs are present and are named for the correct version and RID;
- the changelog content is reflected in the release notes;
- the manifest lists four archives and the expected `win-x64`/`win-arm64`
  runtime identifiers;
- the archive checksums validate; and
- the extracted CLI and MCP executables start on a clean interactive Windows
  machine.

For a downloaded archive set:

```powershell
$expected = Get-Content .\SHA256SUMS.txt
$expected
Get-FileHash .\CLIF-0.1.0-win-x64.zip -Algorithm SHA256
Get-Content .\CLIF-v0.1.0.release-manifest.json -Raw | ConvertFrom-Json
```

If GitHub CLI is installed and authenticated, verify an archive attestation
with:

```powershell
gh attestation verify .\CLIF-0.1.0-win-x64.zip -R cmalpass/clif
```

Only after those checks should the maintainer click **Publish release**.

## Versioning and future work

Use Semantic Versioning: patch releases for compatible fixes, minor releases
for backward-compatible features, and major releases for breaking changes.
Keep the ZIP distribution as the primary channel until the CLI or MCP server
has a deliberate package/tool contract. Authenticode signing is a separate
follow-up that requires a protected Windows signing certificate and timestamp
service; artifact attestations do not replace Windows executable signing.
