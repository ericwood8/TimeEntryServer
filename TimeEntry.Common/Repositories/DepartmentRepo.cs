using TimeEntry.Common.Context;
using TimeEntry.Common.Entities;

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

    public async Task<Department> GetByIdIncludeTeams(int id)
    {
        return await _dbSet.Include(c => c.Teams).FirstAsync(c => c.DepartmentId == id);
    }
}