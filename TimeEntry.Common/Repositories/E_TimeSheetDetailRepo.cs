namespace TimeEntry.Common.Repositories;

public class E_TimeSheetDetailRepo : GenericRepo<E_TimeSheetDetail>
{
    public E_TimeSheetDetailRepo(TimeEntryContext context) : base(context)
    {
    }

    public async Task<List<E_TimeSheetDetail>> GetAllOfTimesheet(int timesheetId)
    {
        return await _dbSet
            .Where(t => t.E_TimeSheetId.Equals(timesheetId))
          .OrderBy(c => c.ProjectId)
          .ToListAsync();
    }
}
