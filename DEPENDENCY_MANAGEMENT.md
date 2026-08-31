# Dependency and Release Management

This document records the repository's dependency and release baseline. It is
intended to keep dependency updates deliberate while CLIF's Windows UI
automation and MCP surfaces are still evolving.

## Current baseline

All direct package references currently use exact versions. Shared references
are consistent across the projects that consume them, but versions are declared
in individual project files rather than centrally.

| Area | Packages and current versions |
| --- | --- |
| CLIF CLI | FlaUI.Core 5.0.0; FlaUI.UIA3 5.0.0; Microsoft.Extensions.Configuration, Microsoft.Extensions.Configuration.Json, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging.Console, System.Text.Json, and System.Drawing.Common 9.0.10; System.CommandLine 2.0.0-beta4.22272.1; StyleCop.Analyzers 1.2.0-beta.556 |
| MCP server | FlaUI.Core 5.0.0; FlaUI.UIA3 5.0.0; Microsoft.Extensions.DependencyInjection and System.Text.Json 10.0.10; System.Drawing.Common 9.0.10 |
| .NET tests | Microsoft.NET.Test.Sdk 18.0.0; xunit 2.9.3; xunit.runner.visualstudio 3.1.5; coverlet.collector 6.0.4; FluentAssertions 8.8.0 |
| CLIF tests only | Moq 4.20.72; AutoFixture 4.18.1; Bogus 35.6.5 |
| Avalonia fixture | Avalonia, Avalonia.Desktop, and Avalonia.Themes.Fluent 11.2.3 |
| WPF fixture | Caliburn.Micro 4.0.212; MaterialDesignThemes 5.1.0; MaterialDesignColors 3.1.0 |

The repository does not currently track `Directory.Packages.props` or a
repository-level `NuGet.config`. It tracks `global.json` to pin the .NET SDK and one
`packages.lock.json` per restoring project; generated `obj/project.assets.json`
files remain out of source control. CI, release, and Copilot setup restores use
`--locked-mode` so an unexpected package-graph change fails before build or
publication.

## Reproducibility status and validated baseline

The current build is reproducible only when the machine supplies the pinned SDK,
Windows Desktop targeting pack, NuGet feeds, and resolved package graph. The
repository currently leaves package ownership partially implicit:

- All projects target `net8.0`; Windows projects target `net8.0-windows` and
  require the Windows Desktop targeting pack. The Avalonia fixture and its
  contract tests target `net8.0`.
- `global.json` pins the repository to SDK `8.0.424` with patch roll-forward
  disabled. CI, release, CodeQL, and Copilot setup all install that exact SDK.
- The local validation host has SDKs `8.0.424` and `10.0.303`, plus the
  corresponding .NET 8 Windows Desktop runtime. The repository's pinned SDK is
  therefore directly testable on the development host.
- On 2026-08-05, the complete solution restored successfully with:

  ```sh
  dotnet restore clif.sln -p:EnableWindowsTargeting=true --force-evaluate
  ```

  A subsequent locked restore of the complete solution also passed on this
  branch with `--locked-mode`. This validates the lockfile graph and the pinned
  SDK on the Windows host; it does not replace interactive Windows UI test
  execution.

The SDK pin and package lockfiles are independent controls. A package-management
migration (for example, adding `Directory.Packages.props`) must remain a separate
change and be validated on a Windows runner with the pinned SDK and Windows
Desktop targeting pack installed.

## Audit findings

- There is no direct version drift among the duplicated test and FlaUI
  references. The MCP SDK requires the 10.0.10 Microsoft.Extensions/System.Text.Json
  family, while the CLI remains on the 9.0.10 family; this is an intentional
  framework-package boundary and should be upgraded as a tested family.
- The main maintainability risk is version ownership, not floating versions.
  Updating a shared package requires editing multiple project files and makes a
  lockfile migration harder to review.
- A current NuGet check found newer releases for some dependencies, including
  Avalonia, System.CommandLine, coverlet.collector, FluentAssertions, and
  Microsoft.NET.Test.Sdk. These are not automatic upgrades: Avalonia has a
  major-version change, System.CommandLine would leave its beta line, and the
  test/coverage packages can alter discovery or CI output.
- The generated lockfiles expose three high-severity transitive NuGet audit
  findings that remain open for remediation: `System.Net.Http` 4.3.0 and
  `System.Text.RegularExpressions` 4.3.0 arrive through the legacy
  `Fare`/`NETStandard.Library` path used by AutoFixture tests, while
  `Tmds.DBus.Protocol` 0.20.0 arrives through Avalonia's `Avalonia.FreeDesktop`
  dependency. Locked restore intentionally records these versions; it does
  not make them safe. Do not turn dependency-audit warnings into a release
  blocker until replacement or upgrade candidates have been tested on the full
  Windows and cross-platform matrix.
- `CLIF.Mcp.csproj` contains the only project-local `0.1.0` application version.
  Tagged release builds override both published applications from the tag, so
  release artifacts receive the intended version, but local project metadata is
  not yet governed by one version source.
- Release artifacts are self-contained Windows x64 and arm64 archives with
  checksums and a CycloneDX SBOM. They are explicitly unsigned, and the current
  release process does not yet publish a human-readable third-party license
  inventory alongside the SBOM.
- The public-repository baseline is in good shape: MIT licensing, attribution,
  security and support policies, contribution guidance, code of conduct,
  Dependabot configuration, CODEOWNERS, issue templates, and a pull-request
  checklist are present.

## Adoption plan

Changes should be made in separate, reviewable steps:

1. The repository now pins SDK `8.0.424` in `global.json` with
   `rollForward: disable`. Every CI, release, CodeQL, and Copilot setup step
   requests the same exact SDK. Acceptance criteria: `dotnet --version` matches
   `8.0.424` in every job; solution restore/build succeeds on Windows; WPF, MCP,
   integration, and cross-platform fixture gates remain green. If the hosted
   runner no longer provides this patch, update `global.json` and every setup
   step together in a dedicated SDK-baseline change.
2. Generate a lockfile migration in a dedicated change, preserving the current
   package versions:

   ```sh
   dotnet restore clif.sln \
     -p:EnableWindowsTargeting=true \
     -p:RestorePackagesWithLockFile=true \
     --force-evaluate
   ```

   Review and track one `packages.lock.json` for each project that restores
   packages, including both fixture projects. Acceptance criteria: a clean
   checkout restores with `--locked-mode`; no lockfile changes occur on a
   second locked restore; Windows CI and the macOS/Linux fixture jobs pass.
   This branch has generated and validated the seven project lockfiles, and
   the workflow restore gates now enforce them.
3. After the lockfile baseline is green, add `Directory.Packages.props` while
   preserving the exact versions listed above. Convert project references to
   unversioned `PackageReference` entries and validate the complete Windows
   build/test matrix plus the cross-platform fixture jobs. Acceptance criteria:
   `dotnet list clif.sln package` reports the same direct versions before and
   after conversion, and the lockfile diff contains only the expected
   ownership/metadata changes.
4. Keep `--locked-mode` in every CI and release restore. Keep
   `--force-evaluate` out of CI; use it only when intentionally updating the
   dependency graph and reviewing the resulting lockfile diff.
5. Evaluate package upgrades one family at a time. Start with tooling-only
   updates, then test System.CommandLine and Avalonia separately. Record
   compatibility results and any license changes in the changelog.
6. Generate a release license/notice inventory from the locked dependency graph
   and ship it with the SBOM. Keep `NOTICE.md` as the repository-level
   attribution policy, not as a substitute for the generated inventory.
7. Before calling a release production-ready, add artifact signing and verify
   that the tag, application version, changelog entry, SBOM, notices, checksums,
   and published binaries all describe the same release.

## Dependency update checklist

- Confirm the package's supported target frameworks and license.
- Check whether the update is a patch, minor, major, or prerelease transition.
- Confirm the selected SDK and Windows Desktop targeting pack with
  `dotnet --info` on the Windows runner before interpreting a restore/build
  failure as a package regression.
- For lockfile work, run a normal restore with `--force-evaluate`, review every
  generated lockfile, then run a second restore with `--locked-mode` and require
  a clean `git diff`.
- Run the full Windows build, unit tests, integration tests, WPF UI tests, and
  MCP end-to-end test when the affected package can influence those surfaces.
- For Avalonia or other fixture packages, run the macOS and Linux fixture jobs
  as well as Windows validation.
- Review the dependency graph and generated lockfile diff for unexpected
  transitive changes.
- Update `CHANGELOG.md` when behavior, support, licensing, or release contents
  change.
