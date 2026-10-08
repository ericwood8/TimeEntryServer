namespace TimeEntry.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
        {
            ArgumentParser.PrintUsage(Console.Out);
            return args.Length == 0 ? ExitCodes.Usage : ExitCodes.Done;
        }

        CliOptions options;
        try
        {
            options = ArgumentParser.Parse(args);
        }
        catch (ArgumentParseException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine();
            ArgumentParser.PrintUsage(Console.Error);
            return ExitCodes.Usage;
        }

        if (options.Command == "resources")
        {
            foreach (var resource in Resources.All)
                Console.WriteLine($"{resource.Name,-15} {resource.Route,-18} {resource.Description}{(resource.NameSearch ? "  (list --name)" : "")}");
            return ExitCodes.Done;
        }

        using var api = new ApiClient(options.Url);
        if (options.Token != null && options.Command != "login")
            api.UseToken(options.Token);
        else if (string.IsNullOrWhiteSpace(options.UserName))
        {
            Console.Error.WriteLine("Error: -U/--user is required (or set TIMEENTRY_USER, or give --token).");
            return ExitCodes.Usage;
        }

        try
        {
            if (!api.IsSignedIn)
            {
                string? password = options.Password;
                if (password is null)
                {
                    if (Console.IsInputRedirected)
                    {
                        Console.Error.WriteLine("Error: -P/--password is required when input is redirected (or set TIMEENTRY_PASSWORD).");
                        return ExitCodes.Usage;
                    }
                    password = ConsolePasswordReader.Read($"Password for {options.UserName}: ");
                }

                var login = await api.LoginAsync(options.UserName!, password);
                if (!login.IsSuccess)
                {
                    string why = login.Status == System.Net.HttpStatusCode.TooManyRequests
                        ? "too many sign-ins in the last minute. Wait a minute, or sign in once with \"timeentry login\" and pass --token."
                        : login.Problem ?? login.Status.ToString();
                    Console.Error.WriteLine($"Sign-in failed: {why}");
                    return ExitCodes.SignInFailed;
                }

                if (options.Command == "login")
                {
                    if (options.Json)
                        Console.WriteLine(login.Body?.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                    else
                        Console.WriteLine(api.Token); // only the token, so $(timeentry login) can capture it
                    return ExitCodes.Done;
                }
            }

            return await CommandRunner.RunAsync(options, api, Console.In, Console.Out, Console.Error);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"Error: could not reach the API at {options.Url}: {ex.Message}");
            return ExitCodes.Unreachable;
        }
        catch (TaskCanceledException)
        {
            Console.Error.WriteLine($"Error: the API at {options.Url} did not answer in time.");
            return ExitCodes.Unreachable;
        }
    }
}
