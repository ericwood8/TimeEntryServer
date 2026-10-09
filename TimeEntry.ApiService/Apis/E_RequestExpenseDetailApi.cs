using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

/// <summary> One line of an expense sheet. It belongs to whoever owns its sheet, and cannot be moved to another sheet. </summary>
public class E_RequestExpenseDetailApi : OwnedCrudApi<E_RequestExpenseDetail, ExpenseDetailSave, ExpenseDetailDto>
{
    private static readonly SortMap<E_RequestExpenseDetail> sorts = new SortMap<E_RequestExpenseDetail>()
        .Add("expenseDate", d => d.ExpenseDate).Add("requestExpenseDetailId", d => d.RequestExpenseDetailId).Add("sheet", d => d.E_RequestExpenseSheetId)
        .Add("expenseType", d => d.ExpenseTypeId).Add("amount", d => d.ReimbursableAmount).Add("vendor", d => d.VendorName);

    protected override CrudStore<E_RequestExpenseDetail> Store(TimeEntryContext context) => CrudStore.For(new E_RequestExpenseDetailRepo(context));
    protected override int KeyOf(E_RequestExpenseDetail row) => row.RequestExpenseDetailId;
    protected override E_RequestExpenseDetail ToEntity(ExpenseDetailSave input) => input.ToEntity();
    protected override ExpenseDetailDto ToOutput(E_RequestExpenseDetail row) => ExpenseDetailDto.From(row);
    protected override async Task<int?> OwnerOfAsync(CrudCall call, int id) => (await RowOwners.ExpenseDetail(call.Context, id))?.EmployeeId;

    // the screens and the CLI know it as /expenseDetails
    protected override void Names(out string singular, out string plural, out string route)
    {
        singular = "ExpenseDetail";
        plural = singular + "s";
        route = "/expenseDetails";
    }

    protected override bool PagedList => true;

    /// <summary> The whole list, or (with pageIndex / pageSize) one page of it; sort and search apply to both. See <see cref="ListQuery"/>. </summary>
    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query)
    {
        var scope = await ScopeAsync(call);
        int[] ids = scope.EmployeeIds;
        TimeEntryContext context = call.Context;
        // a detail belongs to whoever owns its sheet
        IQueryable<E_RequestExpenseDetail> rows = scope.All ? context.E_RequestExpenseDetail
            : context.E_RequestExpenseDetail.Where(d => context.E_RequestExpenseSheet.Any(s => s.RequestExpenseSheetId == d.E_RequestExpenseSheetId && ids.Contains(s.EmployeeId)));
        return await rows.ToResultAsync(query, sorts, "expenseDate:desc", d => d.RequestExpenseDetailId,
            text => d => (d.VendorName != null && d.VendorName.Contains(text)) || (d.Notes != null && d.Notes.Contains(text)), ExpenseDetailDto.From);
    }

    protected override async Task<IResult?> PrepareCreateAsync(CrudCall call, E_RequestExpenseDetail row, ExpenseDetailSave input)
    {
        // a detail may only be added to an expense sheet the caller may use
        int? sheetOwner = await RowOwners.ExpenseSheet(call.Context, row.E_RequestExpenseSheetId);
        if (sheetOwner == null)
            return ApiProblems.Invalid("There is no expense sheet with that id."); // 400 error if the expense sheet does not exist
        return await DenyAsync(call, sheetOwner);
    }

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, E_RequestExpenseDetail row, ExpenseDetailSave input)
    {
        var owner = await RowOwners.ExpenseDetail(call.Context, id);
        if (await DenyAsync(call, owner?.EmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        row.E_RequestExpenseSheetId = owner!.ParentId; // the detail cannot be moved to another sheet
        return null;
    }
}
