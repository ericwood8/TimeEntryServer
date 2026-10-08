using TimeEntry.Common.Models;

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

    /// <summary> Same rows as GetAllOfTimesheet, with the project and task names filled in. </summary>
    public async Task<List<TimeSheetDetailRow>> GetAllOfTimesheetWithNames(int timesheetId)
    {
        return await _dbSet
            .Where(t => t.E_TimeSheetId == timesheetId)
            .OrderBy(t => t.ProjectId)
            .Select(t => new TimeSheetDetailRow(
                t.TimeSheetDetailId, t.E_TimeSheetId, t.ProjectId, t.Project!.Name, t.ProjectTaskId, t.ProjectTask!.Name,
                t.SundayHours, t.MondayHours, t.TuesdayHours, t.WednesdayHours, t.ThursdayHours, t.FridayHours, t.SaturdayHours, t.Notes))
            .ToListAsync();
    }
}
