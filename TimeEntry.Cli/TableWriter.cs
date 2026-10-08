using System.Text.Json.Nodes;

namespace TimeEntry.Cli;

/// <summary> Prints API JSON as a table (a list) or as name / value lines (one row). </summary>
public static class TableWriter
{
    public const int MaxColumns = 8;
    public const int MaxCellWidth = 40;

    public static void Write(JsonNode? node, Resource? resource, TextWriter output)
    {
        switch (node)
        {
            case JsonArray rows:
                WriteRows(rows, resource, output);
                break;
            case JsonObject row:
                WriteRow(row, output);
                break;
            case null:
                output.WriteLine("(nothing)");
                break;
            default:
                output.WriteLine(node.ToJsonString());
                break;
        }
    }

    private static void WriteRows(JsonArray rows, Resource? resource, TextWriter output)
    {
        var objects = rows.OfType<JsonObject>().ToList();
        if (objects.Count == 0)
        {
            output.WriteLine("0 rows");
            return;
        }

        // the id first, then the other plain columns in the order the API sends them
        List<string> columns = objects[0].Select(p => p.Key).Where(key => objects.All(o => IsScalar(o[key]))).ToList();
        string? idColumn = columns.FirstOrDefault(c => resource != null && c.Equals(resource.IdProperty, StringComparison.OrdinalIgnoreCase));
        if (idColumn != null)
        {
            columns.Remove(idColumn);
            columns.Insert(0, idColumn);
        }
        columns = columns.Take(MaxColumns).ToList();

        var cells = objects.Select(o => columns.Select(c => Cell(o[c])).ToArray()).ToList();
        int[] widths = columns.Select((c, i) => Math.Max(c.Length, cells.Max(r => r[i].Length))).ToArray();

        output.WriteLine(string.Join("  ", columns.Select((c, i) => c.PadRight(widths[i]))).TrimEnd());
        output.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
        foreach (var row in cells)
            output.WriteLine(string.Join("  ", row.Select((cell, i) => cell.PadRight(widths[i]))).TrimEnd());
        output.WriteLine($"{objects.Count} row{(objects.Count == 1 ? "" : "s")}");
    }

    private static void WriteRow(JsonObject row, TextWriter output)
    {
        int width = row.Count == 0 ? 0 : row.Max(p => p.Key.Length);
        foreach (var (key, value) in row)
        {
            string text = value switch
            {
                JsonArray list => $"[{list.Count} item{(list.Count == 1 ? "" : "s")}]",
                JsonObject => "{...}",
                _ => Cell(value),
            };
            output.WriteLine($"{key.PadRight(width)}  {text}");
        }
    }

    private static bool IsScalar(JsonNode? node) => node is not (JsonArray or JsonObject);

    /// <summary> A cell: empty for null, true / false, a long value cut with "...". </summary>
    public static string Cell(JsonNode? node)
    {
        string text = node switch
        {
            null => "",
            JsonValue value when value.TryGetValue(out string? s) => s,
            _ => node.ToJsonString(),
        };
        text = text.Replace('\r', ' ').Replace('\n', ' ');
        return text.Length <= MaxCellWidth ? text : text[..(MaxCellWidth - 3)] + "...";
    }
}
