using Microsoft.EntityFrameworkCore.Design;

namespace TimeEntry.Common.Context;

public class TimeEntryContextFactory : IDesignTimeDbContextFactory<TimeEntryContext>
{
    public TimeEntryContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TimeEntryContext>();

        // Hard coding dev server since this is only used to create migrations
        optionsBuilder.UseSqlServer(@"Server=WIN-4PF20KOTHOG;Database=TimeEntry;Trusted_Connection=True;");

        return new TimeEntryContext(optionsBuilder.Options);
    }
}