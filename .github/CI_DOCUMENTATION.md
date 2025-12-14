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

### Steps

1. **Checkout Code**: Retrieves the repository code
2. **Setup .NET**: Installs .NET 8.0 SDK
3. **Restore Dependencies**: Restores NuGet packages for the solution
4. **Build Solution**: Compiles the entire solution in Release configuration
5. **Run Unit Tests**: Executes all unit tests with detailed logging
6. **Run Integration Tests**: Executes integration tests (marked as continue-on-error due to UI automation limitations in CI)
7. **Upload Test Results**: Uploads test result files (.trx) as artifacts
8. **Build CLIF CLI**: Publishes the CLIF CLI application
9. **Build TestWpfApp**: Builds the test WPF application
10. **Upload Artifacts**: Uploads both CLIF and TestWpfApp binaries

### Artifacts

The workflow produces the following artifacts (retained for 30 days):

1. **test-results**: Test result files in TRX format
2. **clif-cli**: Published CLIF CLI application (ready to run)
3. **test-wpf-app**: Built TestWpfApp application for testing

## Test Execution

### Unit Tests
- Run with filter: `FullyQualifiedName~Unit`
- Must pass for the workflow to succeed
- Logs saved to `unit-tests.trx`

### Integration Tests
- Run with filter: `FullyQualifiedName~Integration`
- Marked as `continue-on-error: true` because UI automation tests may not work reliably in headless CI environments
- Logs saved to `integration-tests.trx`

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

```bash
# Restore dependencies
dotnet restore clif.sln

# Build the solution
dotnet build clif.sln --configuration Release

# Run unit tests
dotnet test CLIF.Tests/CLIF.Tests.csproj --filter "FullyQualifiedName~Unit"

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

**Artifacts not uploading**
- Check that the artifact paths exist: `./publish/` and `TestWpfApp/bin/Release/net8.0-windows/`
- Verify the build steps completed successfully
- The `if: always()` condition ensures uploads happen even if tests fail

## Future Improvements

Potential enhancements to the CI pipeline:

1. **Code Coverage**: Add code coverage reporting using coverlet
2. **Release Automation**: Add workflow to create GitHub releases automatically
3. **Multi-Platform Testing**: Add smoke tests that can run on Linux (non-UI tests only)
4. **Performance Testing**: Add performance benchmarks for automation operations
5. **Documentation Generation**: Auto-generate API documentation
6. **Security Scanning**: Add dependency vulnerability scanning
