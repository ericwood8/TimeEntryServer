using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

/// <summary> An employee's expense sheets, listed with their details (the details are saved through <see cref="E_RequestExpenseDetailApi"/>). </summary>
public class E_RequestExpenseSheetApi : OwnedCrudApi<E_RequestExpenseSheet, ExpenseSheetSave, ExpenseSheetDto>
{
    private static readonly SortMap<E_RequestExpenseSheet> sorts = new SortMap<E_RequestExpenseSheet>()
        .Add("requestExpenseSheetId", s => s.RequestExpenseSheetId).Add("employeeId", s => s.EmployeeId).Add("employee", s => s.Employee!.Name)
        .Add("projectId", s => s.ProjectId).Add("notes", s => s.Notes);

    protected override CrudStore<E_RequestExpenseSheet> Store(TimeEntryContext context) => CrudStore.For(new E_RequestExpenseSheetRepo(context));
    protected override int KeyOf(E_RequestExpenseSheet row) => row.RequestExpenseSheetId;
    protected override E_RequestExpenseSheet ToEntity(ExpenseSheetSave input) => input.ToEntity();
    protected override ExpenseSheetDto ToOutput(E_RequestExpenseSheet row) => ExpenseSheetDto.From(row);
    protected override Task<int?> OwnerOfAsync(CrudCall call, int id) => RowOwners.ExpenseSheet(call.Context, id);

    // the screens and the CLI know it as /expenseSheets
    protected override void Names(out string singular, out string plural, out string route)
    {
        singular = "ExpenseSheet";
        plural = singular + "s";
        route = "/expenseSheets";
    }

    protected override bool PagedList => true;

    /// <summary> The whole list with each sheet's details, or (with pageIndex / pageSize) one page of it; sort and search apply to both. See <see cref="ListQuery"/>. </summary>
    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query)
    {
        var scope = await ScopeAsync(call);
        int[] ids = scope.EmployeeIds;
        int[] named = await call.Context.EmployeeIdsNamedAsync(query);
        IQueryable<E_RequestExpenseSheet> rows = scope.All ? call.Context.E_RequestExpenseSheet : call.Context.E_RequestExpenseSheet.Where(s => ids.Contains(s.EmployeeId));
        // the page is taken from the sheets; each sheet on it brings its details
        return await rows.ToResultAsync(query, sorts, "requestExpenseSheetId:desc", s => s.RequestExpenseSheetId,
            text => s => (s.Notes != null && s.Notes.Contains(text)) || EF.Constant(named).Contains(s.EmployeeId), ExpenseSheetDto.From, q => q.Include(s => s.ExpenseDetails));
    }

    protected override Task<IResult?> PrepareCreateAsync(CrudCall call, E_RequestExpenseSheet row, ExpenseSheetSave input) =>
        Task.FromResult(OwnerFromToken(call, own => row.EmployeeId = own));

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, E_RequestExpenseSheet row, ExpenseSheetSave input)
    {
        int? owner = await RowOwners.ExpenseSheet(call.Context, id);
        if (await DenyAsync(call, owner) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        row.EmployeeId = owner!.Value; // the sheet cannot be handed to someone else
        return null;
    }
}
