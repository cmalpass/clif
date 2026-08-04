# TestCrossPlatformApp.Tests

This is a display-free contract test project for `TestCrossPlatformApp`.

It intentionally does not use `Avalonia.Headless`: the repository currently targets Avalonia 11.2.3, but no compatible `Avalonia.Headless` package or existing headless harness is available in the local dependency cache. The tests therefore validate the source-level fixture contract without requiring a windowing system.

The tests compare `AutomationIdManifest.json` with the actual `TestCrossPlatformApp/MainWindow.axaml` file. They detect duplicate or missing automation IDs and protect the fixture's stable state-readout anchors.

Run from the repository root:

```bash
dotnet restore TestCrossPlatformApp.Tests/TestCrossPlatformApp.Tests.csproj
dotnet test TestCrossPlatformApp.Tests/TestCrossPlatformApp.Tests.csproj --no-restore
```

Set `TEST_CROSS_PLATFORM_APP_XAML` when the fixture source is outside the normal repository layout.
