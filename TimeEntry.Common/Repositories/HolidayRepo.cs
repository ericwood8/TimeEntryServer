namespace TimeEntry.Common.Repositories;

public class HolidayRepo : GenericRepo<Holiday>
{
    public HolidayRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<Holiday>> GetByName(string name)
    {
        return await _dbSet
            .Where(d => d.Name.StartsWith(name))
            .ToListAsync();
    }
}