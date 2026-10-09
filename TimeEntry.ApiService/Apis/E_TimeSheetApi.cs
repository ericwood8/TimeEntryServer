using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

/// <summary> An employee's time sheets (the hours are in the details, see <see cref="E_TimeSheetDetailApi"/>). </summary>
public class E_TimeSheetApi : OwnedCrudApi<E_TimeSheet, TimeSheetSave, TimeSheetDto>
{
    private static readonly SortMap<E_TimeSheet> sorts = new SortMap<E_TimeSheet>()
        .Add("whenEntered", t => t.WhenEntered).Add("timeSheetId", t => t.TimeSheetId).Add("employeeId", t => t.EmployeeId)
        .Add("employee", t => t.Employee!.Name).Add("notes", t => t.Notes);

    protected override CrudStore<E_TimeSheet> Store(TimeEntryContext context) => CrudStore.For(new E_TimeSheetRepo(context));
    protected override int KeyOf(E_TimeSheet row) => row.TimeSheetId;
    protected override E_TimeSheet ToEntity(TimeSheetSave input) => input.ToEntity();
    protected override TimeSheetDto ToOutput(E_TimeSheet row) => TimeSheetDto.From(row);
    protected override Task<int?> OwnerOfAsync(CrudCall call, int id) => RowOwners.TimeSheet(call.Context, id);

    protected override bool PagedList => true;

    /// <summary> The whole list, or (with pageIndex / pageSize) one page of it; sort and search apply to both. See <see cref="ListQuery"/>. </summary>
    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query)
    {
        var scope = await ScopeAsync(call);
        int[] ids = scope.EmployeeIds;
        int[] named = await call.Context.EmployeeIdsNamedAsync(query);
        IQueryable<E_TimeSheet> rows = scope.All ? call.Context.E_TimeSheet : call.Context.E_TimeSheet.Where(t => ids.Contains(t.EmployeeId));
        return await rows.ToResultAsync(query, sorts, "whenEntered:desc", t => t.TimeSheetId,
            text => t => (t.Notes != null && t.Notes.Contains(text)) || EF.Constant(named).Contains(t.EmployeeId), TimeSheetDto.From);
    }

    protected override Task<IResult?> PrepareCreateAsync(CrudCall call, E_TimeSheet row, TimeSheetSave input) =>
        Task.FromResult(OwnerFromToken(call, own => row.EmployeeId = own));

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, E_TimeSheet row, TimeSheetSave input)
    {
        int? owner = await RowOwners.TimeSheet(call.Context, id);
        if (await DenyAsync(call, owner) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        if (!call.User.CanManageAll())
            row.EmployeeId = owner!.Value; // the row cannot be handed to someone else
        return null;
    }
}
