namespace CLIF.Tests.McpUI;

/// <summary>
/// Serializes black-box MCP UI tests because they control a real desktop process.
/// </summary>
[CollectionDefinition("McpUI", DisableParallelization = true)]
public sealed class McpUiCollection : ICollectionFixture<McpProcessFixture>
{
}
