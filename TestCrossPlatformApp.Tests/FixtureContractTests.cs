using Xunit;

namespace TestCrossPlatformApp.Tests;

public sealed class FixtureContractTests
{
    [Fact]
    public void Manifest_identifies_the_expected_fixture_version()
    {
        var manifest = FixtureContract.LoadManifest();

        Assert.Equal("TestCrossPlatformApp", manifest.Fixture);
        Assert.Equal("fixture-v1", manifest.Version);
    }

    [Fact]
    public void Manifest_contains_unique_non_empty_automation_ids()
    {
        var ids = FixtureContract.LoadManifest().AutomationIds;

        Assert.NotEmpty(ids);
        Assert.All(ids, id => Assert.False(string.IsNullOrWhiteSpace(id)));
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Fixture_xaml_declares_exactly_the_manifest_automation_ids()
    {
        var expected = FixtureContract.LoadManifest().AutomationIds
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        var actual = FixtureContract.ReadDeclaredAutomationIds()
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Fixture_contains_state_readouts_for_automation_verification()
    {
        var ids = FixtureContract.LoadManifest().AutomationIds;

        Assert.Contains("StatusTextBlock", ids);
        Assert.Contains("ActionCountText", ids);
        Assert.Contains("ActionLogTextBox", ids);
        Assert.Contains("SelectedDateText", ids);
        Assert.Contains("SelectedDataRowText", ids);
    }
}
