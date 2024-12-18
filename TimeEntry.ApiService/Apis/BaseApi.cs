namespace TimeEntry.ApiService.Apis;

public abstract class BaseApi<T> : IApi where T : class
{
    protected static readonly string? _apiSubDir;  // example value is "/departments" when class is Department

    public abstract void Register(WebApplication app);

    /// <summary>
    ///    Take model and breaks name into usuable strings.
    /// </summary>
    /// <param name="singular">When class is Department, value is "Department"</param>
    /// <param name="plural">When class is Department, value is "Departments"</param>
    /// <param name="apiSubDir">When class is Department, value is "/departments"</param>
    protected static void BreakIntoStrings(out string singular, out string plural, out string apiSubDir)
    {
        Type type = typeof(T);
        string name = type.Name;

        // Strip E_ from Employee tables
        if (name.StartsWith("E_"))
        {
            name = name.Replace("E_", ""); // match found, so strip
        }

        // Strip SY_ from System tables
        if (name.StartsWith("SY_"))
        {
            name = name.Replace("SY_", ""); // match found, so strip
        }

        singular = $"{name}"; // leave as proper
        plural = $"{singular}s";
        apiSubDir = $"/{name.ToLower()}s"; // do as all lower and plural
    }
}
