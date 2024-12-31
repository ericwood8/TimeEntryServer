namespace TimeEntry.Common.Repositories;

public class ProjectRepo : NameActiveRepo<Project>
{
    public ProjectRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<Project>> GetAllIncludeTasks()
    {
        try
        {
            return await _dbSet
                .Where(t => t.IsActive) // only fetch active
                .Include(p => p.Tasks)
                .OrderBy(d => d.Name) // order by name
                .ToListAsync();
        }
        catch 
        {
            throw new Exception("Unable to establish connection OR no attached database.");
        }
    }
}