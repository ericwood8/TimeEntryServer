using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TimeEntry.Cli;

/// <summary> Builds the JSON body of an add or an update from --file and --set. </summary>
public static class BodyBuilder
{
    /// <summary> true / false, a number or null become that JSON value; anything else is text. A value in quotes is always text. </summary>
    public static JsonNode? ReadValue(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length >= 2 && (trimmed[0] == '"' || trimmed[0] == '\'') && trimmed[^1] == trimmed[0])
            return JsonValue.Create(trimmed[1..^1]);
        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            return null;
        if (bool.TryParse(trimmed, out bool flag))
            return JsonValue.Create(flag);
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole))
            return JsonValue.Create(whole);
        if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
            return JsonValue.Create(number);
        return JsonValue.Create(text);
    }

    /// <summary> The properties from the file (when given), then the --set values over them. </summary>
    public static JsonObject ReadChanges(CliOptions options)
    {
        JsonObject changes = new();
        if (options.File != null)
        {
            if (!System.IO.File.Exists(options.File))
                throw new ArgumentParseException($"The file '{options.File}' does not exist.");
            JsonNode? parsed;
            try
            {
                parsed = JsonNode.Parse(System.IO.File.ReadAllText(options.File));
            }
            catch (JsonException ex)
            {
                throw new ArgumentParseException($"The file '{options.File}' is not valid JSON: {ex.Message}");
            }
            changes = parsed as JsonObject ?? throw new ArgumentParseException($"The file '{options.File}' must hold one JSON object.");
        }

        foreach (var (key, value) in options.Sets)
            Apply(changes, key, ReadValue(value));
        return changes;
    }

    /// <summary> Sets a property; if the object already has that property in other letter case, that spelling is kept. </summary>
    public static void Apply(JsonObject target, string key, JsonNode? value)
    {
        string existing = target.Select(p => p.Key).FirstOrDefault(k => k.Equals(key, StringComparison.OrdinalIgnoreCase)) ?? key;
        target[existing] = value;
    }

    /// <summary> add: the resource's defaults (id 0, active) under the caller's values. </summary>
    public static JsonObject ForAdd(Resource resource, JsonObject changes)
    {
        JsonObject body = resource.NewRowDefaults();
        foreach (var (key, value) in changes.ToList())
            Apply(body, key, value?.DeepClone());
        return body;
    }

    /// <summary> update: the stored row with the changed properties laid over it. The id cannot be changed. </summary>
    public static JsonObject ForUpdate(Resource resource, JsonObject stored, JsonObject changes)
    {
        foreach (var (key, value) in changes.ToList())
        {
            if (key.Equals(resource.IdProperty, StringComparison.OrdinalIgnoreCase))
                continue;
            Apply(stored, key, value?.DeepClone());
        }
        return stored;
    }
}
