namespace TimeEntry.Common.Repositories;

public class TimeEntryUserRepo : NameActiveRepo<TimeEntryUser>
{
    public TimeEntryUserRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<TimeEntryUser>> GetAllActive()
    {
        return await _dbSet
            .Where(t => t.IsActive) // only fetch active
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}