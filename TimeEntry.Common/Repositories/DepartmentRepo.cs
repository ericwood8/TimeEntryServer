namespace TimeEntry.Common.Repositories;

public class DepartmentRepo : NameActiveRepo<Department>
{
    public DepartmentRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<Department>> GetAllIncludeTeams()
    {
        return await _dbSet
            .Where(e => e.IsActive) // only fetch active
            .Include(d => d.Teams)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }

    /// <summary> The department with its teams (tracked), or null when there is no department with that id. </summary>
    public async Task<Department?> GetByIdIncludeTeams(int id)
    {
        return await _dbSet.Include(c => c.Teams).FirstOrDefaultAsync(c => c.DepartmentId == id);
    }
}