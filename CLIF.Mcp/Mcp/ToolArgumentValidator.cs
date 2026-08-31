// Licensed under the MIT License.

using System.Text.Json;

namespace CLIF.Mcp;

/// <summary>
/// Validates tool arguments against the JSON Schema subset exposed by CLIF tools.
/// </summary>
/// <remarks>
/// CLIF deliberately keeps schemas small and local. This validator enforces the
/// safety-relevant subset used by the tool contract before any UI automation is
/// dispatched: object shape, required values, primitive types, enumerations, and
/// common size and numeric bounds. Object schemas are closed by default so a client
/// cannot silently pass misspelled or undocumented parameters to a tool.
/// </remarks>
internal static class ToolArgumentValidator
{
    public static bool Validate(JsonElement? arguments, object inputSchema, out string error)
    {
        ArgumentNullException.ThrowIfNull(inputSchema);

        var schema = JsonSerializer.SerializeToElement(inputSchema);
        if (!arguments.HasValue)
        {
            using var emptyArguments = JsonDocument.Parse("{}");
            return ValidateValue(emptyArguments.RootElement, schema, "arguments", out error);
        }

        return ValidateValue(arguments.Value, schema, "arguments", out error);
    }

    private static bool ValidateValue(JsonElement value, JsonElement schema, string path, out string error)
    {
        if (!ValidateType(value, schema, path, out error) ||
            !ValidateEnum(value, schema, path, out error) ||
            !ValidateBounds(value, schema, path, out error))
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return ValidateObject(value, schema, path, out error);
        }

        if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var itemSchema))
        {
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                if (!ValidateValue(item, itemSchema, $"{path}[{index}]", out error))
                {
                    return false;
                }

                index++;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool ValidateObject(JsonElement value, JsonElement schema, string path, out string error)
    {
        var properties = schema.TryGetProperty("properties", out var declaredProperties)
            ? declaredProperties
            : default;

        if (schema.TryGetProperty("required", out var required))
        {
            foreach (var propertyName in required.EnumerateArray())
            {
                var name = propertyName.GetString();
                if (string.IsNullOrEmpty(name) || !value.TryGetProperty(name, out _))
                {
                    error = $"missing required argument '{name}'";
                    return false;
                }
            }
        }

        foreach (var property in value.EnumerateObject())
        {
            if (properties.ValueKind != JsonValueKind.Object || !properties.TryGetProperty(property.Name, out var propertySchema))
            {
                var allowsAdditionalProperties = schema.TryGetProperty("additionalProperties", out var additionalProperties) &&
                    additionalProperties.ValueKind == JsonValueKind.True;
                if (properties.ValueKind == JsonValueKind.Object && !allowsAdditionalProperties)
                {
                    error = $"unexpected argument '{property.Name}'";
                    return false;
                }

                continue;
            }

            if (!ValidateValue(property.Value, propertySchema, $"{path}.{property.Name}", out error))
            {
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool ValidateType(JsonElement value, JsonElement schema, string path, out string error)
    {
        if (!schema.TryGetProperty("type", out var typeProperty) || typeProperty.ValueKind != JsonValueKind.String)
        {
            error = string.Empty;
            return true;
        }

        var expected = typeProperty.GetString();
        var isValid = expected switch
        {
            "object" => value.ValueKind == JsonValueKind.Object,
            "array" => value.ValueKind == JsonValueKind.Array,
            "string" => value.ValueKind == JsonValueKind.String,
            "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "number" => value.ValueKind == JsonValueKind.Number,
            "integer" => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out _),
            "null" => value.ValueKind == JsonValueKind.Null,
            _ => true,
        };

        error = isValid ? string.Empty : $"{path} must be a {expected}";
        return isValid;
    }

    private static bool ValidateEnum(JsonElement value, JsonElement schema, string path, out string error)
    {
        if (!schema.TryGetProperty("enum", out var allowedValues) || allowedValues.ValueKind != JsonValueKind.Array)
        {
            error = string.Empty;
            return true;
        }

        foreach (var allowedValue in allowedValues.EnumerateArray())
        {
            if (value.GetRawText() == allowedValue.GetRawText())
            {
                error = string.Empty;
                return true;
            }
        }

        error = $"{path} is not an allowed value";
        return false;
    }

    private static bool ValidateBounds(JsonElement value, JsonElement schema, string path, out string error)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var length = value.GetString()?.Length ?? 0;
            if (schema.TryGetProperty("minLength", out var minimumLength) && length < minimumLength.GetInt32())
            {
                error = $"{path} must contain at least {minimumLength.GetInt32()} characters";
                return false;
            }

            if (schema.TryGetProperty("maxLength", out var maximumLength) && length > maximumLength.GetInt32())
            {
                error = $"{path} must contain at most {maximumLength.GetInt32()} characters";
                return false;
            }
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            var length = value.GetArrayLength();
            if (schema.TryGetProperty("minItems", out var minimumItems) && length < minimumItems.GetInt32())
            {
                error = $"{path} must contain at least {minimumItems.GetInt32()} items";
                return false;
            }

            if (schema.TryGetProperty("maxItems", out var maximumItems) && length > maximumItems.GetInt32())
            {
                error = $"{path} must contain at most {maximumItems.GetInt32()} items";
                return false;
            }
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            if (schema.TryGetProperty("minimum", out var minimum) && number < minimum.GetDouble())
            {
                error = $"{path} must be at least {minimum.GetRawText()}";
                return false;
            }

            if (schema.TryGetProperty("maximum", out var maximum) && number > maximum.GetDouble())
            {
                error = $"{path} must be at most {maximum.GetRawText()}";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}
