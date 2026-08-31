# Release artifacts

Tagged releases are produced by [`.github/workflows/release.yml`](.github/workflows/release.yml).
The workflow currently publishes self-contained Windows archives for `win-x64`
and `win-arm64`, plus a SHA-256 checksum file and a CycloneDX JSON SBOM.

Each release also contains a machine-readable manifest named
`CLIF-v<version>.release-manifest.json`. The manifest is the integrity index
for that release and records:

- the release tag, semantic version, source commit, repository, and workflow run;
- every published archive, its project, runtime identifier, byte size, and SHA-256 digest;
- the checksum file and its SHA-256 digest;
- the SBOM filename, format, byte size, and SHA-256 digest.

The build job verifies `SHA256SUMS.txt` before the manifest is generated. The
build job also refuses to publish unless the tag is a supported SemVer 2.0 tag,
matches the canonical version in `CLIF.Mcp/CLIF.Mcp.csproj`, and has a dated
release entry in `CHANGELOG.md` rather than an `Unreleased` entry. The
publication job requires every expected archive, checksum, SBOM, and manifest
file; verifies the checksum file; and compares each archive digest with the
manifest. It also verifies that the manifest refers to the triggering tag,
version, and commit and includes the expected archive/checksum/SBOM
relationships. These checks provide integrity and provenance metadata without
requiring signing secrets; release signing remains a separate follow-up.

Before creating a tag, update the matching changelog heading, for example:

```text
## [0.1.0] - 2026-08-05
```

The release workflow intentionally fails if that entry is absent or still
marked `Unreleased`.

To inspect a downloaded release locally:

```text
sha256sum --check SHA256SUMS.txt
jq . CLIF-v<version>.release-manifest.json
```

On Windows PowerShell, use the following equivalent checks:

```powershell
$archive = Get-FileHash .\CLIF-<version>-win-x64.zip -Algorithm SHA256
$archive.Hash
Get-Content .\SHA256SUMS.txt
$manifest = Get-Content .\CLIF-v<version>.release-manifest.json -Raw | ConvertFrom-Json
$manifest.release
$manifest.archives
```

Compare the printed hash with the archive entry in `SHA256SUMS.txt`. The
manifest is an integrity index for the release; it does not contain its own
digest because that would be recursive. Its SHA-256 can be calculated after
download when a separate digest of the manifest itself is needed.
