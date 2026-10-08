using Microsoft.EntityFrameworkCore.Design;

namespace TimeEntry.Common.Context;

/// <summary> Used only by "dotnet ef" at design time. Set ConnectionStrings__TimeEntryDb to point it at your server. </summary>
public class TimeEntryContextFactory : IDesignTimeDbContextFactory<TimeEntryContext>
{
    const string DefaultConnection = "Server=localhost;Database=TimeEntry;Trusted_Connection=True;TrustServerCertificate=True;";

    public TimeEntryContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__TimeEntryDb") ?? DefaultConnection;
        var optionsBuilder = new DbContextOptionsBuilder<TimeEntryContext>();
        optionsBuilder.UseSqlServer(connection);

        return new TimeEntryContext(optionsBuilder.Options);
    }
}
