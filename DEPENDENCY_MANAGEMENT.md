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
| MCP server | FlaUI.Core 5.0.0; FlaUI.UIA3 5.0.0; System.Text.Json and System.Drawing.Common 9.0.10 |
| .NET tests | Microsoft.NET.Test.Sdk 18.0.0; xunit 2.9.3; xunit.runner.visualstudio 3.1.5; coverlet.collector 6.0.4; FluentAssertions 8.8.0 |
| CLIF tests only | Moq 4.20.72; AutoFixture 4.18.1; Bogus 35.6.5 |
| Avalonia fixture | Avalonia, Avalonia.Desktop, and Avalonia.Themes.Fluent 11.2.3 |
| WPF fixture | Caliburn.Micro 4.0.212; MaterialDesignThemes 5.1.0; MaterialDesignColors 3.1.0 |

The repository does not currently track `Directory.Packages.props`,
`packages.lock.json`, `global.json`, or a repository-level `NuGet.config`.
Generated `obj/project.assets.json` files are not source-controlled. The
release workflow therefore performs a normal restore and deliberately does not
use locked restore yet.

## Reproducibility status and validated baseline

The current build is reproducible only when the machine supplies the same SDK,
Windows Desktop targeting pack, NuGet feeds, and resolved package graph. The
repository currently leaves the SDK and package graph partially implicit:

- All projects target `net8.0`; Windows projects target `net8.0-windows` and
  require the Windows Desktop targeting pack. The Avalonia fixture and its
  contract tests target `net8.0`.
- CI, the release workflow, and the Copilot setup use `8.0.x`, which selects a
  moving SDK patch rather than one repository-owned SDK version.
- The local audit host has SDKs `9.0.308` and `10.0.101`, but no .NET 8 SDK or
  Windows Desktop runtime. This is useful for project-graph inspection but is
  not sufficient to select or approve the repository's Windows SDK pin.
- On 2026-08-05, the complete solution restored successfully with:

  ```sh
  dotnet restore clif.sln -p:EnableWindowsTargeting=true --force-evaluate
  ```

  This validates the current unpinned restore path only. It does not validate
  locked restore, a selected `global.json`, or Windows UI test execution.

Do not add `global.json`, enable locked restore, or convert to central package
management in the same change. Each changes restore resolution and must be
validated on a Windows runner with the .NET 8 SDK and Windows Desktop targeting
pack installed.

## Audit findings

- There is no direct version drift among the duplicated shared references: the
  FlaUI, test SDK, xUnit, test adapter, coverage collector, and FluentAssertions
  versions are consistent wherever they are used.
- The main maintainability risk is version ownership, not floating versions.
  Updating a shared package requires editing multiple project files and makes a
  lockfile migration harder to review.
- A current NuGet check found newer releases for some dependencies, including
  Avalonia, System.CommandLine, coverlet.collector, FluentAssertions, and
  Microsoft.NET.Test.Sdk. These are not automatic upgrades: Avalonia has a
  major-version change, System.CommandLine would leave its beta line, and the
  test/coverage packages can alter discovery or CI output.
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

1. On `windows-latest`, record `dotnet --info`, `dotnet --list-sdks`, and the
   installed Windows Desktop targeting packs. Select the latest supported
   .NET 8 SDK patch that is available on that runner, then add a `global.json`
   with that exact SDK version. Use `rollForward: disable` unless the support
   policy intentionally permits patch roll-forward. Update every CI/setup
   `dotnet-version` input to install the version selected by `global.json`.
   Acceptance criteria: `dotnet --version` matches the policy in every job;
   solution restore/build succeeds on Windows; WPF, MCP, integration, and
   cross-platform fixture gates remain green.
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
3. After the lockfile baseline is green, add `Directory.Packages.props` while
   preserving the exact versions listed above. Convert project references to
   unversioned `PackageReference` entries and validate the complete Windows
   build/test matrix plus the cross-platform fixture jobs. Acceptance criteria:
   `dotnet list clif.sln package` reports the same direct versions before and
   after conversion, and the lockfile diff contains only the expected
   ownership/metadata changes.
4. Enable `--locked-mode` in every CI and release restore only after the
   lockfiles are tracked and the Windows runner has passed the locked restore.
   Keep `--force-evaluate` out of CI; use it only when intentionally updating
   the dependency graph.
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
