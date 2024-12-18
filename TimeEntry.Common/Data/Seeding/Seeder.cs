using EFCore.BulkExtensions;

namespace TimeEntry.Common.Data.Seeding;
public class Seeder(TimeEntryContext context)
{
    public void Seed()
    {
        if (!context.Project.Any())
        {
            var strategy = context.Database.CreateExecutionStrategy();
            context.Project.AddRange(SeedData.GetProjects());

            strategy.Execute(() =>
            {
                context.BulkSaveChanges();
            });
        }
    }
}