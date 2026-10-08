namespace TimeEntry.Cli;

public class CliOptions
{
    /// <summary> The first word: list, get, add, update, delete, decide, login, whoami or resources. </summary>
    public required string Command { get; set; }
    public Resource? Resource { get; set; }
    /// <summary> The row id (get, update, delete) or the request id (decide). </summary>
    public int? Id { get; set; }
    /// <summary> decide: approve, reject, reimburse, void or cancel. </summary>
    public string? Decision { get; set; }
    /// <summary> list --name: rows whose name starts with this. </summary>
    public string? Name { get; set; }
    /// <summary> add / update --file: a JSON file holding the body (or the properties to change). </summary>
    public string? File { get; set; }
    /// <summary> add / update --set key=value, in the order given. </summary>
    public List<KeyValuePair<string, string>> Sets { get; } = [];

    public string Url { get; set; } = DefaultUrl;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    /// <summary> --token: a token from "timeentry login"; when given, no sign-in call is made. </summary>
    public string? Token { get; set; }
    /// <summary> --json: print the server's JSON instead of a table. </summary>
    public bool Json { get; set; }
    /// <summary> --yes: delete without asking. </summary>
    public bool Yes { get; set; }

    public const string DefaultUrl = "http://localhost:5084";
}
