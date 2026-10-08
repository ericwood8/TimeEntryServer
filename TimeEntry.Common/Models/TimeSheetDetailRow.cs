namespace TimeEntry.Common.Models;

/// <summary> A time sheet detail plus the names of its project and task, so a grid does not need a second call to show them. </summary>
public record TimeSheetDetailRow(
    int TimeSheetDetailId, int E_TimeSheetId, int ProjectId, string ProjectName, int ProjectTaskId, string ProjectTaskName,
    decimal SundayHours, decimal MondayHours, decimal TuesdayHours, decimal WednesdayHours,
    decimal ThursdayHours, decimal FridayHours, decimal SaturdayHours, string Notes);
