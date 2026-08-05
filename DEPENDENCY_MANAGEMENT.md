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

1. Choose and document the supported .NET SDK patch/feature band, then add a
   `global.json` that the Windows CI setup installs. Do this only after checking
   the selected SDK against the Windows Desktop targeting packs.
2. Add `Directory.Packages.props` while preserving the versions listed above.
   Convert project references to unversioned `PackageReference` entries and
   validate the complete Windows build and test matrix.
3. Generate and review a `packages.lock.json` for every restored project,
   including the cross-platform fixture. Enable `--locked-mode` in CI and the
   release workflow only after all lockfiles are tracked and reproducible.
4. Evaluate package upgrades one family at a time. Start with tooling-only
   updates, then test System.CommandLine and Avalonia separately. Record
   compatibility results and any license changes in the changelog.
5. Generate a release license/notice inventory from the locked dependency graph
   and ship it with the SBOM. Keep `NOTICE.md` as the repository-level
   attribution policy, not as a substitute for the generated inventory.
6. Before calling a release production-ready, add artifact signing and verify
   that the tag, application version, changelog entry, SBOM, notices, checksums,
   and published binaries all describe the same release.

## Dependency update checklist

- Confirm the package's supported target frameworks and license.
- Check whether the update is a patch, minor, major, or prerelease transition.
- Run the full Windows build, unit tests, integration tests, WPF UI tests, and
  MCP end-to-end test when the affected package can influence those surfaces.
- For Avalonia or other fixture packages, run the macOS and Linux fixture jobs
  as well as Windows validation.
- Review the dependency graph and generated lockfile diff for unexpected
  transitive changes.
- Update `CHANGELOG.md` when behavior, support, licensing, or release contents
  change.
