using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace TestCrossPlatformApp.Tests;

internal sealed record FixtureManifest(
    [property: JsonPropertyName("fixture")] string Fixture,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("automationIds")] IReadOnlyList<string> AutomationIds);

internal static class FixtureContract
{
    private const string AutomationIdAttribute = "AutomationProperties.AutomationId";

    public static FixtureManifest LoadManifest()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "AutomationIdManifest.json");
        var json = File.ReadAllText(manifestPath);
        return JsonSerializer.Deserialize<FixtureManifest>(json)
            ?? throw new InvalidOperationException($"Could not deserialize fixture manifest: {manifestPath}");
    }

    public static IReadOnlyList<string> ReadDeclaredAutomationIds()
    {
        var xamlPath = LocateFixtureXaml();
        var document = XDocument.Load(xamlPath, LoadOptions.PreserveWhitespace);

        return document.Root is null
            ? Array.Empty<string>()
            : document.Root
            .DescendantsAndSelf()
            .SelectMany(element => element.Attributes())
            .Where(attribute => attribute.Name.LocalName == AutomationIdAttribute)
            .Select(attribute => attribute.Value)
            .ToArray();
    }

    private static string LocateFixtureXaml()
    {
        var configuredPath = Environment.GetEnvironmentVariable("TEST_CROSS_PLATFORM_APP_XAML");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "TestCrossPlatformApp", "MainWindow.axaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate TestCrossPlatformApp/MainWindow.axaml. "
            + "Run the test from the repository or set TEST_CROSS_PLATFORM_APP_XAML.");
    }
}
