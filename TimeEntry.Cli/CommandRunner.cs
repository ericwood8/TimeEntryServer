using System.Text.Json;
using System.Text.Json.Nodes;

namespace TimeEntry.Cli;

public static class ExitCodes
{
    public const int Done = 0;
    public const int Usage = 1;
    public const int ApiRefused = 2;
    public const int SignInFailed = 3;
    public const int Unreachable = 4;
}

/// <summary> Runs one parsed command against a signed-in <see cref="ApiClient"/>. </summary>
public static class CommandRunner
{
    private static readonly Dictionary<string, int> DecisionStatusIds = new()
    {
        ["approve"] = 2,   // SY_RequestStatusType.Approved
        ["reject"] = 3,    // Rejected
        ["cancel"] = 4,    // Canceled
        ["reimburse"] = 5, // Reimbursed
        ["void"] = 6,      // Voided
    };

    public static async Task<int> RunAsync(CliOptions options, ApiClient api, TextReader input, TextWriter output, TextWriter error)
    {
        try
        {
            return options.Command switch
            {
                "list" => await ListAsync(options, api, output, error),
                "get" => await GetAsync(options, api, output, error),
                "add" => await AddAsync(options, api, output, error),
                "update" => await UpdateAsync(options, api, output, error),
                "delete" => await DeleteAsync(options, api, input, output, error),
                "decide" => await DecideAsync(options, api, output, error),
                "whoami" => await WhoAmIAsync(options, api, output, error),
                _ => Fail(error, $"Unknown command '{options.Command}'.", ExitCodes.Usage),
            };
        }
        catch (ArgumentParseException ex)
        {
            return Fail(error, ex.Message, ExitCodes.Usage);
        }
    }

    private static async Task<int> ListAsync(CliOptions options, ApiClient api, TextWriter output, TextWriter error)
    {
        var resource = options.Resource!;
        string path = string.IsNullOrWhiteSpace(options.Name) ? resource.Route : $"{resource.Route}/{Uri.EscapeDataString(options.Name.Trim())}";
        return Show(await api.GetAsync(path), options, resource, output, error);
    }

    private static async Task<int> GetAsync(CliOptions options, ApiClient api, TextWriter output, TextWriter error)
    {
        var resource = options.Resource!;
        return Show(await api.GetAsync($"{resource.Route}/{options.Id}"), options, resource, output, error);
    }

    private static async Task<int> AddAsync(CliOptions options, ApiClient api, TextWriter output, TextWriter error)
    {
        var resource = options.Resource!;
        JsonObject body = BodyBuilder.ForAdd(resource, BodyBuilder.ReadChanges(options));
        var result = await api.PostAsync(resource.Route, body);
        if (result.IsSuccess && !options.Json)
            output.WriteLine($"Added to {resource.Name}:");
        return Show(result, options, resource, output, error);
    }

    private static async Task<int> UpdateAsync(CliOptions options, ApiClient api, TextWriter output, TextWriter error)
    {
        var resource = options.Resource!;
        JsonObject changes = BodyBuilder.ReadChanges(options);

        var stored = await api.GetAsync($"{resource.Route}/{options.Id}");
        if (!stored.IsSuccess)
            return FailResult(error, stored);
        if (stored.Body is not JsonObject row)
            return Fail(error, "The API did not return a row to change.", ExitCodes.ApiRefused);

        var result = await api.PutAsync($"{resource.Route}/{options.Id}", BodyBuilder.ForUpdate(resource, row, changes));
        if (result.IsSuccess && !options.Json)
            output.WriteLine($"Changed {resource.Name} {options.Id}:");
        return Show(result, options, resource, output, error);
    }

    private static async Task<int> DeleteAsync(CliOptions options, ApiClient api, TextReader input, TextWriter output, TextWriter error)
    {
        var resource = options.Resource!;
        if (!options.Yes)
        {
            output.Write($"Delete {resource.Name} {options.Id}? (y/N) ");
            string? answer = input.ReadLine();
            if (!"y".Equals(answer?.Trim(), StringComparison.OrdinalIgnoreCase) && !"yes".Equals(answer?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine("Nothing deleted.");
                return ExitCodes.Done;
            }
        }

        var result = await api.DeleteAsync($"{resource.Route}/{options.Id}");
        if (!result.IsSuccess)
            return FailResult(error, result);
        output.WriteLine($"Deleted {resource.Name} {options.Id}.");
        return ExitCodes.Done;
    }

    private static async Task<int> DecideAsync(CliOptions options, ApiClient api, TextWriter output, TextWriter error)
    {
        var requests = options.Resource!;
        var stored = await api.GetAsync($"{requests.Route}/{options.Id}");
        if (!stored.IsSuccess)
            return FailResult(error, stored);
        if (stored.Body is not JsonObject row)
            return Fail(error, "The API did not return a request.", ExitCodes.ApiRefused);

        BodyBuilder.Apply(row, "sY_RequestStatusTypeId", DecisionStatusIds[options.Decision!]);
        var result = await api.PutAsync($"{requests.Route}/{options.Id}", row);
        if (result.IsSuccess && !options.Json)
            output.WriteLine($"Request {options.Id}: {options.Decision} done.");
        return Show(result, options, requests, output, error);
    }

    private static async Task<int> WhoAmIAsync(CliOptions options, ApiClient api, TextWriter output, TextWriter error) =>
        Show(await api.GetAsync("auth/me"), options, null, output, error);

    /// <summary> Prints a good answer as a table or JSON; an error answer goes to the error stream. </summary>
    private static int Show(ApiResult result, CliOptions options, Resource? resource, TextWriter output, TextWriter error)
    {
        if (!result.IsSuccess)
            return FailResult(error, result);

        if (options.Json)
            output.WriteLine(result.Body?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null");
        else
            TableWriter.Write(result.Body, resource, output);
        return ExitCodes.Done;
    }

    /// <summary> An error answer: 401 means the token is bad or has run out (sign in again); anything else is the API refusing. </summary>
    private static int FailResult(TextWriter error, ApiResult result)
    {
        if (result.Status == System.Net.HttpStatusCode.Unauthorized)
            return Fail(error, "not signed in, or the token has expired. Sign in again (timeentry login).", ExitCodes.SignInFailed);
        return Fail(error, result.Problem ?? result.Status.ToString(), ExitCodes.ApiRefused);
    }

    private static int Fail(TextWriter error, string message, int code)
    {
        error.WriteLine($"Error: {message}");
        return code;
    }
}
