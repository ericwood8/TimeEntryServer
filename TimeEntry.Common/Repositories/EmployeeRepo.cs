using TimeEntry.Common.Context;
using TimeEntry.Common.Entities;

namespace TimeEntry.Common.Repositories;

public class EmployeeRepo : NameActiveRepo<Employee>
{
    public EmployeeRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<Employee>> GetAllIncludeDropdowns()
    {
        return await _dbSet
            .Where(e => e.IsActive) // only fetch active
            .Include(e => e.Manager)
            .Include(e => e.Department)
            .Include(e => e.DepartmentTeam)
            .OrderBy(e => e.Name)
            .ToListAsync();
    }
}
