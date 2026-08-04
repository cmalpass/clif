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

        var executableName = Path.GetFileName(application);
        return AllowedApplications.Contains(application) || AllowedApplications.Contains(executableName);
    }

    private static bool GetBooleanEnvironmentVariable(string name) =>
        bool.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value;
}
