# TestCrossPlatformApp

`TestCrossPlatformApp` is the first cross-platform fixture for CLIF. It is built with Avalonia and targets `net8.0`, so it can run on Windows, macOS, and Linux from the same project.

CLIF itself currently uses FlaUI/UI Automation and remains Windows-specific. The immediate purpose of this fixture is to provide a consistent application contract for:

- cross-platform UI smoke tests;
- future CLIF backends;
- selector and script contract tests;
- accessibility-tree experiments;
- MCP or other automation adapters.

## Run

```bash
dotnet run --project TestCrossPlatformApp/TestCrossPlatformApp.csproj
```

The first restore requires network access to NuGet. Avalonia packages are pinned to `11.2.3` for reproducible fixture builds.

## Fixture contract

Controls use stable `AutomationProperties.AutomationId` values. The initial contract includes:

- `MainWindow` as the application window;
- `TestButton`, `ToggleButton`, `TestTextBox`, and `FilledTextBox`;
- `TestCheckBox`, `RadioButton1`, and `RadioButton2`;
- `TestComboBox` and `TestListBox`;
- `TestSlider`, `TestProgressBar`, and `SliderValueText`;
- `TestExpander` and `HiddenButton`;
- `TestTreeView` with `RootNode`, `Child1`, `Grandchild1`, `Grandchild2`, and `Child2`;
- `TestTabControl` with `Tab1`, `Tab2`, and `Tab3`;
- `TestDatePicker` with the visible `SelectedDateText` readout;
- `TestDataTable` with `DataRow1`, `DataRow2`, `DataRow3`, and the visible `SelectedDataRowText` readout;
- `StatusTextBlock`, `ActionCountText`, and `ActionLogTextBox` for deterministic state verification;
- `ResetMenuItem`, `AboutMenuItem`, and `ExitMenuItem`.

`TestDataTable` is intentionally a deterministic selectable list/grid surface in v1, not a full Avalonia `DataGrid`. Editing, sorting, and column behaviors will be added only when they have a corresponding CLIF contract and end-to-end test.

New fixture controls should include a stable automation ID, a visible state or result, and a behavior test before they are used by CLIF examples.

## Windows CLIF smoke tests

On Windows, after building the solution, run the FlaUI/UIA3 provider and CLIF-service tests with:

```powershell
dotnet test CLIF.Tests/CLIF.Tests.csproj --configuration Release --filter "Category=CrossPlatformUI"
```

The direct provider tests verify that Avalonia exposes the expected accessibility surface. The CLIF-backed tests additionally attach through `AutomationService`, resolve selectors such as `id=TestButton`, exercise controls, and run a script against the fixture.

## Framework compatibility

CLIF uses standard UI Automation patterns first (`Invoke`, `Value`, `Selection`, and `Toggle`) so the same script can drive both WPF and Avalonia where each framework exposes an equivalent pattern. The Windows CI suite runs native WPF service tests and Avalonia service tests independently.

Avalonia's `DatePicker` is discoverable through UI Automation but currently rejects the native `DateTimePicker` setter exposed by FlaUI. The fixture therefore provides `SetKnownDateButton` as an explicit `Invoke`-based date action and verifies the visible date readout. WPF's native `DatePicker` is tested through `SetDatePickerAsync` and `GetDatePickerValueAsync` directly.
