# CI/CD Documentation

## Overview

This project uses GitHub Actions for continuous integration and delivery. The CI pipeline is configured to run on Windows runners since CLIF is a Windows-specific application that uses WPF UI automation.

## Workflow Configuration

### File Location
`.github/workflows/build-and-test.yml`

### Triggers
The workflow runs on:
- Push to `main` or `develop` branches
- Pull requests targeting `main` or `develop` branches
- Manual trigger via GitHub Actions UI (workflow_dispatch)

## Build Pipeline

The workflow is split into three parallel jobs for faster feedback:

### Job 1: Build Solution (`build`)
1. **Checkout Code**: Retrieves the repository code
2. **Setup .NET**: Installs .NET 8.0 SDK
3. **Display .NET info**: Logs SDK, SDK list, and runtime info for diagnostics
4. **Restore Dependencies**: Restores NuGet packages for the solution
5. **Build Solution**: Compiles the entire solution in Release configuration
6. **Publish CLIF CLI**: Publishes the CLIF CLI application
7. **Upload Artifacts**: Uploads both CLIF CLI and TestWpfApp binaries

### Job 2: Unit Tests (`unit-tests`)
Runs after `build` succeeds.
1. **Checkout, Setup .NET, Restore, Build**: Prepares the environment
2. **Run Unit Tests with Coverage**: Executes unit tests with code coverage collection via coverlet
3. **Upload Test Results**: Uploads TRX results and Cobertura coverage XML as artifacts
4. **Generate Test Report**: Uses `dorny/test-reporter` to create a GitHub check with detailed test results visible on the PR

### Job 3: Integration Tests (`integration-tests`)
Runs after `build` succeeds, in parallel with unit tests.
1. **Checkout, Setup .NET, Restore, Build**: Prepares the environment
2. **Run Integration Tests**: Executes integration tests with `continue-on-error: true` (UI automation tests may not work in headless CI)
3. **Upload Test Results**: Uploads TRX results and coverage data as artifacts
4. **Generate Test Report**: Creates a GitHub check with integration test results (non-blocking)
5. **Outcome Summary**: Logs a helpful warning if integration tests failed, explaining the headless CI limitation

### Artifacts

The workflow produces the following artifacts (retained for 30 days):

1. **clif-cli**: Published CLIF CLI application (ready to run)
2. **test-wpf-app**: Built TestWpfApp application for testing
3. **unit-test-results**: Unit test TRX files and code coverage data (Cobertura XML)
4. **integration-test-results**: Integration test TRX files and code coverage data

### Test Reports

The workflow uses [dorny/test-reporter](https://github.com/dorny/test-reporter) to generate rich test reports as GitHub Checks:
- **Unit Test Results**: Displayed as a check on the PR, blocking merge on failure
- **Integration Test Results**: Displayed as a check on the PR, non-blocking

These reports provide per-test pass/fail details, duration, and error messages directly on the pull request.

## Code Coverage

Code coverage is collected via [coverlet](https://github.com/coverlet-coverage/coverlet) during test runs using the `--collect:"XPlat Code Coverage"` flag. Coverage reports are generated in Cobertura XML format and uploaded as artifacts.

## Copilot Agent Environment

### File Location
`.github/copilot-setup-steps.yml`

This file configures the Copilot coding agent's development environment to run on `windows-latest` with .NET 8.0, ensuring it can build and test the full solution including WPF dependencies.

## Test Execution

### Unit Tests
- Run with filter: `FullyQualifiedName~Unit`
- Must pass for the workflow to succeed
- Logs saved to `unit-tests.trx`
- Code coverage collected in Cobertura XML format

### Integration Tests
- Run with filter: `FullyQualifiedName~Integration`
- Marked as `continue-on-error: true` because UI automation tests may not work reliably in headless CI environments
- Logs saved to `integration-tests.trx`
- Code coverage collected in Cobertura XML format

## Windows-Specific Requirements

CLIF targets `net8.0-windows` and uses Windows-specific APIs:
- Windows Presentation Foundation (WPF)
- UI Automation APIs via FlaUI
- Windows Desktop App framework

**Note**: This application cannot be built on Linux or macOS runners. The workflow must use `windows-latest`.

## Status Badge

The repository README includes a status badge showing the current build status:

[![Build and Test](https://github.com/cmalpass/clif/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/cmalpass/clif/actions/workflows/build-and-test.yml)

## Local Testing

To run the same tests locally on Windows:

```powershell
# Restore dependencies
dotnet restore clif.sln

# Build the solution
dotnet build clif.sln --configuration Release

# Run unit tests with coverage
dotnet test CLIF.Tests/CLIF.Tests.csproj --filter "FullyQualifiedName~Unit" --collect:"XPlat Code Coverage"

# Run integration tests (requires Windows with UI)
dotnet test CLIF.Tests/CLIF.Tests.csproj --filter "FullyQualifiedName~Integration"

# Publish CLIF CLI
dotnet publish CLIF/CLIF.csproj --configuration Release --output ./publish

# Build TestWpfApp
dotnet build TestWpfApp/TestWpfApp.csproj --configuration Release
```

## Troubleshooting

### Common Issues

**Build fails with "NETSDK1100: To build a project targeting Windows..."**
- This means the workflow is not running on a Windows runner
- Verify `runs-on: windows-latest` is set in the workflow file

**Integration tests fail in CI**
- This is expected behavior and marked with `continue-on-error: true`
- UI automation tests require an interactive Windows desktop environment
- These tests should be run manually with TestWpfApp running

**Test reporter check not appearing on PR**
- Verify `checks: write` permission is set on the test jobs
- The `dorny/test-reporter` action requires this permission to create check runs

**Artifacts not uploading**
- Check that the artifact paths exist: `./publish/` and `TestWpfApp/bin/Release/net8.0-windows/`
- Verify the build steps completed successfully
- The `if: always()` condition ensures test result uploads happen even if tests fail
