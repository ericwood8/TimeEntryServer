namespace TimeEntry.Common.Repositories;

public class ProjectTaskRepo : NameActiveRepo<ProjectTask>
{
    public ProjectTaskRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<ProjectTask>> GetAllOfProject(int id)
    {
        return await _dbSet
            .Where(t => t.ProjectId.Equals(id) && t.IsActive) // only fetch active
            .OrderBy(d => d.Name) // order by name
            .ToListAsync();
    }
}
