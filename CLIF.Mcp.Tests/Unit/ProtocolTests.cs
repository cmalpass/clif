using System.Text.Json;
using FluentAssertions;
using CLIF.Mcp;

namespace CLIF.Mcp.Tests.Unit;

/// <summary>
/// Tests for MCP protocol types and JSON serialization/deserialization.
/// </summary>
public class ProtocolTests
{
    [Fact]
    public void JsonRpcRequest_Deserialize_FullRequest()
    {
        var json = """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"clif_click","arguments":{"ref":"w1e5"}}}""";

        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, McpProtocol.JsonOptions);

        request.Should().NotBeNull();
        request!.JsonRpc.Should().Be("2.0");
        request.Method.Should().Be("tools/call");
        request.Id.Should().NotBeNull();
        request.Id!.Value.GetInt32().Should().Be(1);
        request.Params.Should().NotBeNull();
    }

    [Fact]
    public void JsonRpcRequest_Deserialize_NotificationWithNoId()
    {
        var json = """{"jsonrpc":"2.0","method":"notifications/initialized"}""";

        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, McpProtocol.JsonOptions);

        request.Should().NotBeNull();
        request!.Method.Should().Be("notifications/initialized");
        request.Id.Should().BeNull();
        request.Params.Should().BeNull();
    }

    [Fact]
    public void JsonRpcRequest_Deserialize_WithStringId()
    {
        var json = """{"jsonrpc":"2.0","id":"abc-123","method":"initialize"}""";

        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, McpProtocol.JsonOptions);

        request.Should().NotBeNull();
        request!.Id.Should().NotBeNull();
        request.Id!.Value.GetString().Should().Be("abc-123");
    }

    [Fact]
    public void JsonRpcResponse_Serialize_SuccessResult()
    {
        var response = new JsonRpcResponse
        {
            Id = JsonSerializer.SerializeToElement(42),
            Result = new McpInitializeResult(),
        };

        var json = JsonSerializer.Serialize(response, McpProtocol.JsonOptions);

        json.Should().Contain("\"jsonrpc\":\"2.0\"");
        json.Should().Contain("\"id\":42");
        json.Should().Contain("\"result\"");
        json.Should().NotContain("\"error\""); // null error should be omitted
    }

    [Fact]
    public void JsonRpcResponse_Serialize_ErrorResult()
    {
        var response = new JsonRpcResponse
        {
            Id = JsonSerializer.SerializeToElement(1),
            Error = new JsonRpcError
            {
                Code = -32603,
                Message = "Internal error",
            },
        };

        var json = JsonSerializer.Serialize(response, McpProtocol.JsonOptions);

        json.Should().Contain("\"error\"");
        json.Should().Contain("-32603");
        json.Should().Contain("Internal error");
        json.Should().NotContain("\"result\""); // null result should be omitted
    }

    [Fact]
    public void McpProtocol_JsonOptions_UsesCamelCase()
    {
        McpProtocol.JsonOptions.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void McpProtocol_JsonOptions_IgnoresNullWhenWriting()
    {
        McpProtocol.JsonOptions.DefaultIgnoreCondition
            .Should().Be(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);
    }

    [Fact]
    public void McpProtocol_JsonOptions_NotIndented()
    {
        McpProtocol.JsonOptions.WriteIndented.Should().BeFalse();
    }

    [Fact]
    public void McpInitializeResult_HasCorrectDefaults()
    {
        var result = new McpInitializeResult();

        result.ProtocolVersion.Should().Be("2024-11-05");
        result.Capabilities.Should().NotBeNull();
        result.ServerInfo.Should().NotBeNull();
        result.ServerInfo.Name.Should().Be("clif-mcp");
        result.ServerInfo.Version.Should().Be("0.1.0");
    }

    [Fact]
    public void McpInitializeResult_Serialize_RoundTrip()
    {
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new McpCapabilities
            {
                Tools = new ToolsCapability { ListChanged = false },
            },
            ServerInfo = new McpServerInfo
            {
                Name = "clif-mcp",
                Version = "0.1.0",
            },
        };

        var json = JsonSerializer.Serialize(result, McpProtocol.JsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpInitializeResult>(json, McpProtocol.JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ProtocolVersion.Should().Be("2024-11-05");
        deserialized.ServerInfo.Name.Should().Be("clif-mcp");
        deserialized.ServerInfo.Version.Should().Be("0.1.0");
        deserialized.Capabilities.Tools.Should().NotBeNull();
        deserialized.Capabilities.Tools!.ListChanged.Should().BeFalse();
    }

    [Fact]
    public void McpToolCallParams_Deserialize_WithArguments()
    {
        var json = """{"name":"clif_click","arguments":{"ref":"w1e5","button":"left"}}""";

        var callParams = JsonSerializer.Deserialize<McpToolCallParams>(json, McpProtocol.JsonOptions);

        callParams.Should().NotBeNull();
        callParams!.Name.Should().Be("clif_click");
        callParams.Arguments.Should().NotBeNull();
        callParams.Arguments!.Value.GetProperty("ref").GetString().Should().Be("w1e5");
        callParams.Arguments!.Value.GetProperty("button").GetString().Should().Be("left");
    }

    [Fact]
    public void McpToolCallParams_Deserialize_WithoutArguments()
    {
        var json = """{"name":"clif_list_windows"}""";

        var callParams = JsonSerializer.Deserialize<McpToolCallParams>(json, McpProtocol.JsonOptions);

        callParams.Should().NotBeNull();
        callParams!.Name.Should().Be("clif_list_windows");
        callParams.Arguments.Should().BeNull();
    }

    [Fact]
    public void McpToolResult_Serialize_TextContent()
    {
        var result = new McpToolResult
        {
            Content = new List<McpContent>
            {
                new() { Type = "text", Text = "Hello World" },
            },
        };

        var json = JsonSerializer.Serialize(result, McpProtocol.JsonOptions);

        json.Should().Contain("\"type\":\"text\"");
        json.Should().Contain("\"text\":\"Hello World\"");
        json.Should().NotContain("\"isError\""); // null should be omitted
    }

    [Fact]
    public void McpToolResult_Serialize_ErrorContent()
    {
        var result = new McpToolResult
        {
            Content = new List<McpContent>
            {
                new() { Type = "text", Text = "Something went wrong" },
            },
            IsError = true,
        };

        var json = JsonSerializer.Serialize(result, McpProtocol.JsonOptions);

        json.Should().Contain("\"isError\":true");
        json.Should().Contain("Something went wrong");
    }

    [Fact]
    public void McpToolResult_Serialize_ImageContent()
    {
        var result = new McpToolResult
        {
            Content = new List<McpContent>
            {
                new()
                {
                    Type = "image",
                    Data = "iVBORw0KGgoAAAANSUhEUg==",
                    MimeType = "image/png",
                },
            },
        };

        var json = JsonSerializer.Serialize(result, McpProtocol.JsonOptions);

        json.Should().Contain("\"type\":\"image\"");
        json.Should().Contain("\"mimeType\":\"image/png\"");
        json.Should().Contain("\"data\":\"iVBORw0KGgoAAAANSUhEUg==\"");
    }

    [Fact]
    public void McpTool_Serialize_WithInputSchema()
    {
        var tool = new McpTool
        {
            Name = "clif_click",
            Description = "Click an element",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    @ref = new { type = "string", description = "Element ref" },
                },
                required = new[] { "ref" },
            },
        };

        var json = JsonSerializer.Serialize(tool, McpProtocol.JsonOptions);

        json.Should().Contain("\"name\":\"clif_click\"");
        json.Should().Contain("\"description\":\"Click an element\"");
        json.Should().Contain("\"inputSchema\"");
        json.Should().Contain("\"required\":[\"ref\"]");
    }

    [Fact]
    public void McpToolsListResult_Serialize_MultipleTools()
    {
        var result = new McpToolsListResult
        {
            Tools = new List<McpTool>
            {
                new() { Name = "clif_click", Description = "Click" },
                new() { Name = "clif_type", Description = "Type" },
            },
        };

        var json = JsonSerializer.Serialize(result, McpProtocol.JsonOptions);
        var deserialized = JsonSerializer.Deserialize<McpToolsListResult>(json, McpProtocol.JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Tools.Should().HaveCount(2);
        deserialized.Tools[0].Name.Should().Be("clif_click");
        deserialized.Tools[1].Name.Should().Be("clif_type");
    }

    [Fact]
    public void McpContent_NullFieldsOmittedInJson()
    {
        var content = new McpContent { Type = "text", Text = "hello" };
        var json = JsonSerializer.Serialize(content, McpProtocol.JsonOptions);

        json.Should().Contain("\"text\":\"hello\"");
        json.Should().NotContain("\"data\"");
        json.Should().NotContain("\"mimeType\"");
    }

    [Fact]
    public void JsonRpcError_DefaultValuesCorrect()
    {
        var error = new JsonRpcError();

        error.Code.Should().Be(0);
        error.Message.Should().BeEmpty();
        error.Data.Should().BeNull();
    }

    [Fact]
    public void JsonRpcRequest_DefaultValuesCorrect()
    {
        var request = new JsonRpcRequest();

        request.JsonRpc.Should().Be("2.0");
        request.Method.Should().BeEmpty();
        request.Id.Should().BeNull();
        request.Params.Should().BeNull();
    }

    [Fact]
    public void JsonRpcResponse_DefaultValuesCorrect()
    {
        var response = new JsonRpcResponse();

        response.JsonRpc.Should().Be("2.0");
        response.Id.Should().BeNull();
        response.Result.Should().BeNull();
        response.Error.Should().BeNull();
    }
}
