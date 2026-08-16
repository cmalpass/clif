// Licensed under the MIT License.

namespace CLIF.Mcp.Security;

/// <summary>
/// Defines the minimum permissions and resource limits for a local CLIF MCP session.
/// The default is intentionally restrictive: an agent must be explicitly granted access
/// to applications and sensitive desktop operations by the host environment.
/// </summary>
public sealed class McpSafetyPolicy
{
    /// <summary>
    /// Maximum number of actions accepted by a single batch request.
    /// </summary>
    public const int MaximumBatchActions = 25;

    /// <summary>
    /// Maximum time an MCP batch wait action may block for.
    /// </summary>
    public const int MaximumWaitMilliseconds = 5_000;

    /// <summary>
    /// Maximum nodes emitted by one accessibility snapshot.
    /// </summary>
    public const int MaximumSnapshotNodes = 1_000;

    /// <summary>
    /// Maximum PNG payload returned from a screenshot request.
    /// </summary>
    public const int MaximumScreenshotBytes = 8 * 1024 * 1024;

    private McpSafetyPolicy(
        IReadOnlySet<string> allowedApplications,
        bool allowWindowEnumeration,
        bool allowWindowClose,
        bool allowFullScreenCapture)
    {
        AllowedApplications = allowedApplications;
        AllowWindowEnumeration = allowWindowEnumeration;
        AllowWindowClose = allowWindowClose;
        AllowFullScreenCapture = allowFullScreenCapture;
    }

    /// <summary>
    /// Gets the exact executable names or paths an MCP session may launch.
    /// An empty set denies all launches.
    /// </summary>
    public IReadOnlySet<string> AllowedApplications { get; }

    /// <summary>
    /// Gets a value indicating whether a session may enumerate desktop windows.
    /// </summary>
    public bool AllowWindowEnumeration { get; }

    /// <summary>
    /// Gets a value indicating whether a session may close a registered window.
    /// </summary>
    public bool AllowWindowClose { get; }

    /// <summary>
    /// Gets a value indicating whether a session may capture the entire desktop.
    /// </summary>
    public bool AllowFullScreenCapture { get; }

    /// <summary>
    /// Creates the policy used by the production MCP host.
    /// </summary>
    public static McpSafetyPolicy FromEnvironment()
    {
        var allowedApplications = (Environment.GetEnvironmentVariable("CLIF_MCP_ALLOWED_APPS") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new McpSafetyPolicy(
            allowedApplications,
            GetBooleanEnvironmentVariable("CLIF_MCP_ALLOW_WINDOW_ENUMERATION"),
            GetBooleanEnvironmentVariable("CLIF_MCP_ALLOW_WINDOW_CLOSE"),
            GetBooleanEnvironmentVariable("CLIF_MCP_ALLOW_FULL_SCREEN_CAPTURE"));
    }

    /// <summary>
    /// Determines whether the supplied executable is explicitly approved for launch.
    /// </summary>
    public bool IsApplicationAllowed(string application)
    {
        if (string.IsNullOrWhiteSpace(application))
        {
            return false;
        }

        var candidate = application.Trim();
        var candidateIsPath = IsPath(candidate);

        foreach (var allowedApplication in AllowedApplications)
        {
            var allowed = allowedApplication.Trim();
            if (string.IsNullOrWhiteSpace(allowed) || IsPath(allowed) != candidateIsPath)
            {
                continue;
            }

            if (!candidateIsPath && string.Equals(candidate, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (candidateIsPath && PathsEqual(candidate, allowed))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPath(string value) =>
        value.IndexOfAny(['/', '\\']) >= 0 ||
        Path.IsPathRooted(value) ||
        (value.Length > 1 && value[1] == ':');

    private static bool PathsEqual(string candidate, string allowed)
    {
        try
        {
            var normalizedCandidate = NormalizePath(candidate);
            var normalizedAllowed = NormalizePath(allowed);
            return string.Equals(normalizedCandidate, normalizedAllowed, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            // Invalid paths are never considered an exact match.
            return false;
        }
    }

    private static string NormalizePath(string path)
    {
        // Normalize both separator styles so the policy behaves consistently
        // when a Windows path is supplied with forward slashes.
        var separatorsNormalized = path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(separatorsNormalized));
    }

    private static bool GetBooleanEnvironmentVariable(string name) =>
        bool.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value;
}
