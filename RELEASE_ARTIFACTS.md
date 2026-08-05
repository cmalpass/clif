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
publication job verifies that the manifest refers to the triggering tag and
commit, contains both expected runtime identifiers, and includes the expected
archive/checksum/SBOM relationships. These checks provide integrity and
provenance metadata without requiring signing secrets; release signing remains
a separate follow-up.

To inspect a downloaded release locally:

```text
sha256sum --check SHA256SUMS.txt
jq . CLIF-v<version>.release-manifest.json
```

The manifest does not contain its own digest because that would be recursive.
Its SHA-256 can be calculated after download when a separate digest of the
manifest itself is needed.
