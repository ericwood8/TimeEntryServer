namespace TimeEntry.ApiService.Security;

/// <summary> A row that hangs under another (a detail under its sheet): the parent's id and the employee who owns the parent. </summary>
public record ChildOwner(int ParentId, int EmployeeId);

/// <summary> The stored owner (and the like) of a row, read without tracking so the caller can still attach its own copy afterwards. Null when there is no such row. </summary>
public static class RowOwners
{
    public static Task<int?> TimeSheet(TimeEntryContext context, int id) =>
        context.E_TimeSheet.AsNoTracking().Where(t => t.TimeSheetId == id).Select(t => (int?)t.EmployeeId).FirstOrDefaultAsync();

    public static Task<int?> TimeSheetOwner(TimeEntryContext context, int timeSheetId) => TimeSheet(context, timeSheetId);

    public static Task<ChildOwner?> TimeSheetDetail(TimeEntryContext context, int id) =>
        (from d in context.E_TimeSheetDetail.AsNoTracking()
         join t in context.E_TimeSheet.AsNoTracking() on d.E_TimeSheetId equals t.TimeSheetId
         where d.TimeSheetDetailId == id
         select new ChildOwner(t.TimeSheetId, t.EmployeeId)).FirstOrDefaultAsync();

    public static Task<int?> ExpenseSheet(TimeEntryContext context, int id) =>
        context.E_RequestExpenseSheet.AsNoTracking().Where(s => s.RequestExpenseSheetId == id).Select(s => (int?)s.EmployeeId).FirstOrDefaultAsync();

    public static Task<ChildOwner?> ExpenseDetail(TimeEntryContext context, int id) =>
        (from d in context.E_RequestExpenseDetail.AsNoTracking()
         join s in context.E_RequestExpenseSheet.AsNoTracking() on d.E_RequestExpenseSheetId equals s.RequestExpenseSheetId
         where d.RequestExpenseDetailId == id
         select new ChildOwner(s.RequestExpenseSheetId, s.EmployeeId)).FirstOrDefaultAsync();

    public static Task<RequestOwner?> Request(TimeEntryContext context, int id) =>
        context.E_Request.AsNoTracking().Where(r => r.RequestId == id)
            .Select(r => new RequestOwner(r.EmployeeId, r.SY_RequestStatusTypeId)).FirstOrDefaultAsync();

    public static Task<DonationOwners?> Donation(TimeEntryContext context, int id) =>
        context.E_DonateLeave.AsNoTracking().Where(d => d.DonateLeaveId == id)
            .Select(d => new DonationOwners(d.DonateFrom_EmployeeId, d.DonateTo_EmployeeId)).FirstOrDefaultAsync();
}

public record RequestOwner(int EmployeeId, int StatusId);

public record DonationOwners(int FromEmployeeId, int ToEmployeeId);
