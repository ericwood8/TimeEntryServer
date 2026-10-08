namespace TimeEntry.Cli;

public class ArgumentParseException(string message) : Exception(message);

public static class ArgumentParser
{
    public static readonly string[] Decisions = ["approve", "reject", "reimburse", "void", "cancel"];

    private static readonly string[] Commands = ["list", "get", "add", "update", "delete", "decide", "login", "whoami", "resources"];

    /// <summary> Reads the command line. The defaults for --url, --user and --password come from TIMEENTRY_URL, TIMEENTRY_USER and TIMEENTRY_PASSWORD. </summary>
    public static CliOptions Parse(string[] args, Func<string, string?>? environment = null)
    {
        environment ??= Environment.GetEnvironmentVariable;

        string? command = null;
        var positionals = new List<string>();
        string? url = environment("TIMEENTRY_URL"), user = environment("TIMEENTRY_USER"), password = environment("TIMEENTRY_PASSWORD"), token = environment("TIMEENTRY_TOKEN");
        string? name = null, file = null;
        var sets = new List<KeyValuePair<string, string>>();
        bool json = false, yes = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            string Value()
            {
                if (i + 1 >= args.Length)
                    throw new ArgumentParseException($"Missing value for '{arg}'.");
                return args[++i];
            }

            switch (arg)
            {
                case "--url": url = Value(); break;
                case "-U": case "--user": user = Value(); break;
                case "-P": case "--password": password = Value(); break;
                case "--token": token = Value(); break;
                case "--json": json = true; break;
                case "-y": case "--yes": yes = true; break;
                case "--name": name = Value(); break;
                case "--file": file = Value(); break;
                case "--set":
                    string setting = Value();
                    int equals = setting.IndexOf('=');
                    if (equals <= 0)
                        throw new ArgumentParseException($"--set takes key=value, not '{setting}'.");
                    sets.Add(new(setting[..equals].Trim(), setting[(equals + 1)..]));
                    break;
                default:
                    if (arg.StartsWith('-') && arg.Length > 1 && !int.TryParse(arg, out _))
                        throw new ArgumentParseException($"Unrecognized argument: '{arg}'.");
                    if (command is null)
                        command = arg.ToLowerInvariant();
                    else
                        positionals.Add(arg);
                    break;
            }
        }

        if (command is null)
            throw new ArgumentParseException("A command is required.");
        if (!Commands.Contains(command))
            throw new ArgumentParseException($"Unknown command '{command}'. Commands: {string.Join(", ", Commands)}.");

        var options = new CliOptions
        {
            Command = command,
            Url = string.IsNullOrWhiteSpace(url) ? CliOptions.DefaultUrl : url.Trim().TrimEnd('/'),
            UserName = user,
            Password = password,
            Token = string.IsNullOrWhiteSpace(token) ? null : token.Trim(),
            Json = json,
            Yes = yes,
            Name = name,
            File = file,
        };
        options.Sets.AddRange(sets);

        switch (command)
        {
            case "list":
                options.Resource = TakeResource(positionals);
                ExpectNoMore(positionals, command);
                if (name != null && !options.Resource.NameSearch)
                    throw new ArgumentParseException($"'{options.Resource.Name}' cannot be searched by name.");
                RejectBodyFlags(options, command);
                break;
            case "get":
            case "delete":
                options.Resource = TakeResource(positionals);
                options.Id = TakeId(positionals, command);
                ExpectNoMore(positionals, command);
                RejectBodyFlags(options, command);
                break;
            case "add":
                options.Resource = TakeResource(positionals);
                ExpectNoMore(positionals, command);
                RequireBody(options, command);
                break;
            case "update":
                options.Resource = TakeResource(positionals);
                options.Id = TakeId(positionals, command);
                ExpectNoMore(positionals, command);
                RequireBody(options, command);
                break;
            case "decide":
                options.Id = TakeId(positionals, command);
                if (positionals.Count == 0)
                    throw new ArgumentParseException($"decide needs a decision: {string.Join(", ", Decisions)}.");
                string decision = positionals[0].ToLowerInvariant();
                positionals.RemoveAt(0);
                if (!Decisions.Contains(decision))
                    throw new ArgumentParseException($"Unknown decision '{decision}'. Use one of: {string.Join(", ", Decisions)}.");
                options.Decision = decision;
                options.Resource = Resources.Find("requests");
                ExpectNoMore(positionals, command);
                break;
            default: // login, whoami, resources
                ExpectNoMore(positionals, command);
                break;
        }

        return options;
    }

    private static Resource TakeResource(List<string> positionals)
    {
        if (positionals.Count == 0)
            throw new ArgumentParseException($"A resource is required. Resources: {string.Join(", ", Resources.All.Select(r => r.Name))}.");
        string name = positionals[0];
        positionals.RemoveAt(0);
        return Resources.Find(name)
            ?? throw new ArgumentParseException($"Unknown resource '{name}'. Resources: {string.Join(", ", Resources.All.Select(r => r.Name))}.");
    }

    private static int TakeId(List<string> positionals, string command)
    {
        if (positionals.Count == 0)
            throw new ArgumentParseException($"{command} needs an id.");
        if (!int.TryParse(positionals[0], out int id) || id <= 0)
            throw new ArgumentParseException($"The id must be a positive whole number, not '{positionals[0]}'.");
        positionals.RemoveAt(0);
        return id;
    }

    private static void ExpectNoMore(List<string> positionals, string command)
    {
        if (positionals.Count > 0)
            throw new ArgumentParseException($"Unexpected argument '{positionals[0]}' for {command}.");
    }

    private static void RequireBody(CliOptions options, string command)
    {
        if (options.File == null && options.Sets.Count == 0)
            throw new ArgumentParseException($"{command} needs at least one --set key=value, or a --file.");
        if (options.Name != null)
            throw new ArgumentParseException($"--name is only for list; use --set name=... for {command}.");
    }

    private static void RejectBodyFlags(CliOptions options, string command)
    {
        if (options.File != null || options.Sets.Count > 0)
            throw new ArgumentParseException($"--set and --file are for add and update, not {command}.");
    }

    public static void PrintUsage(TextWriter writer)
    {
        writer.WriteLine("""
            timeentry - command-line client for the TimeEntry API

            It signs in with your TimeEntry user, calls the API and prints the answer. It never opens the database,
            so the API's rules (validation, who may see which rows, name checks) always apply. Nothing is saved
            between runs: no token file, no stored password.

            Usage:
              timeentry [options] list   <resource> [--name <prefix>]
              timeentry [options] get    <resource> <id>
              timeentry [options] add    <resource> (--set key=value ... | --file body.json)
              timeentry [options] update <resource> <id> (--set key=value ... | --file body.json)
              timeentry [options] delete <resource> <id> [--yes]
              timeentry [options] decide <requestId> approve|reject|reimburse|void|cancel
              timeentry [options] login
              timeentry [options] whoami
              timeentry resources

            Options:
              --url <address>     The API (default: TIMEENTRY_URL, else http://localhost:5084). Use the API's own address; the UI's /api prefix is not needed.
              -U, --user <name>   The TimeEntry user to sign in as (default: TIMEENTRY_USER).
              -P, --password <pw> That user's password (default: TIMEENTRY_PASSWORD; asked for, hidden, when not given).
              --token <token>     A token from "timeentry login" (default: TIMEENTRY_TOKEN). With a token there is no sign-in, so no -U or -P.
              --json              Print the server's JSON instead of a table.
              -y, --yes           delete: do not ask first.
              -h, --help          This text.

            add and update:
              --set key=value    A property to set (repeatable). Values read as true/false, numbers, null or text; quote a number to keep it text.
                                 update changes only the named properties and keeps the rest of the row.
              --file <path>      A JSON file with the body (add) or with the properties to change (update). --set wins over the file.

            Scripts: every run signs in again, and the API allows about 5 sign-ins a minute per computer. For more calls than that, sign in once:
              export TIMEENTRY_TOKEN=$(timeentry -U Boss login)        (PowerShell: $env:TIMEENTRY_TOKEN = timeentry -U Boss login)
            The token lasts as long as the API says (see "login --json"); nothing is written to disk.

            Examples:
              timeentry -U Boss list departments
              timeentry -U Boss list employees --name Sm
              timeentry -U Boss add projects --set name="Bridge Repair"
              timeentry -U Boss update employees 4 --set availableLeaveHours=40
              timeentry -U Boss decide 12 approve
              timeentry -U Boss delete holidays 3 --yes

            Exit codes: 0 done, 1 wrong command line, 2 the API refused or failed, 3 sign-in failed, 4 the API could not be reached.
            """);
    }
}
