// Licensed under the MIT License.

using CLIF.Mcp.Security;
using FluentAssertions;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Verifies that MCP application launch allowlisting distinguishes executable names from paths.
/// </summary>
public sealed class McpSafetyPolicyTests
{
    private static readonly object EnvironmentLock = new();

    [Fact]
    public void ExactExecutableName_AllowsOnlyTheExactName()
    {
        WithPolicy("fixture.exe", policy =>
        {
            policy.IsApplicationAllowed("fixture.exe").Should().BeTrue();
            policy.IsApplicationAllowed("  fixture.exe  ").Should().BeTrue();
            policy.IsApplicationAllowed("other.exe").Should().BeFalse();
            policy.IsApplicationAllowed(Path.Combine(Path.GetTempPath(), "fixture.exe")).Should().BeFalse();
        });
    }

    [Fact]
    public void ExactPath_AllowsOnlyTheCanonicalPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "clif-mcp-policy-tests");
        var allowedPath = Path.Combine(root, "fixture.exe");
        var equivalentPath = Path.Combine(root, "nested", "..", "fixture.exe");
        var differentPath = Path.Combine(root, "other", "..", "other.exe");

        WithPolicy($"  {allowedPath}  ", policy =>
        {
            policy.IsApplicationAllowed(allowedPath).Should().BeTrue();
            policy.IsApplicationAllowed(equivalentPath).Should().BeTrue();
            policy.IsApplicationAllowed(differentPath).Should().BeFalse();
            policy.IsApplicationAllowed("fixture.exe").Should().BeFalse();
        });
    }

    [Fact]
    public void TraversalOutsideAnAllowedPath_DoesNotMatchTheAllowedPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "clif-mcp-policy-tests", "approved");
        var allowedPath = Path.Combine(root, "fixture.exe");
        var escapedPath = Path.Combine(root, "..", "fixture.exe");

        WithPolicy(allowedPath, policy =>
        {
            policy.IsApplicationAllowed(escapedPath).Should().BeFalse();
        });
    }

    [Fact]
    public void MismatchedAllowlistEntries_AreRejected()
    {
        var allowedPath = Path.Combine(Path.GetTempPath(), "clif-mcp-policy-tests", "fixture.exe");

        WithPolicy($"name.exe;{allowedPath}", policy =>
        {
            policy.IsApplicationAllowed("different.exe").Should().BeFalse();
            policy.IsApplicationAllowed(Path.Combine(Path.GetTempPath(), "name.exe")).Should().BeFalse();
            policy.IsApplicationAllowed(Path.Combine(Path.GetTempPath(), "clif-mcp-policy-tests", "other.exe"))
                .Should().BeFalse();
        });
    }

    private static void WithPolicy(string allowedApplications, Action<McpSafetyPolicy> assertion)
    {
        lock (EnvironmentLock)
        {
            var previousValue = Environment.GetEnvironmentVariable("CLIF_MCP_ALLOWED_APPS");
            try
            {
                Environment.SetEnvironmentVariable("CLIF_MCP_ALLOWED_APPS", allowedApplications);
                assertion(McpSafetyPolicy.FromEnvironment());
            }
            finally
            {
                Environment.SetEnvironmentVariable("CLIF_MCP_ALLOWED_APPS", previousValue);
            }
        }
    }
}
