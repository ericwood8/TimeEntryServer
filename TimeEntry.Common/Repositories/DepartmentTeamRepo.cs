namespace TimeEntry.Common.Repositories;

public class DepartmentTeamRepo : NameActiveRepo<DepartmentTeam>
{
    public DepartmentTeamRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<DepartmentTeam>> GetAllOfDepartment(int id)
    {
        return await _dbSet
            .Where(t => t.DepartmentId.Equals(id) && t.IsActive) // only fetch active of department
            .OrderBy(d => d.Name) // order by name
            .ToListAsync();
    }
}
