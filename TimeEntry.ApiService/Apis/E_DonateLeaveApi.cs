using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

/// <summary> Leave hours one employee gives another. Both may see the donation; only the giver (or Admin / Human Resources) may change or delete it. </summary>
public class E_DonateLeaveApi : OwnedCrudApi<E_DonateLeave, DonateLeaveSave, DonateLeaveDto>
{
    private static readonly SortMap<E_DonateLeave> sorts = new SortMap<E_DonateLeave>()
        .Add("whenDonated", d => d.WhenDonated).Add("donateLeaveId", d => d.DonateLeaveId).Add("hoursDonated", d => d.HoursDonated)
        .Add("from", d => d.DonateFrom_EmployeeId).Add("to", d => d.DonateTo_EmployeeId).Add("note", d => d.Note);

    protected override CrudStore<E_DonateLeave> Store(TimeEntryContext context) => CrudStore.For(new E_DonateLeaveRepo(context));
    protected override int KeyOf(E_DonateLeave row) => row.DonateLeaveId;
    protected override E_DonateLeave ToEntity(DonateLeaveSave input) => input.ToEntity();
    protected override DonateLeaveDto ToOutput(E_DonateLeave row) => DonateLeaveDto.From(row);
    protected override async Task<int?> OwnerOfAsync(CrudCall call, int id) => (await RowOwners.Donation(call.Context, id))?.FromEmployeeId;

    protected override bool PagedList => true;

    /// <summary> The whole list, or (with pageIndex / pageSize) one page of it; sort and search apply to both. See <see cref="ListQuery"/>. </summary>
    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query)
    {
        // a donation is visible to whoever gave and whoever received
        var scope = await ScopeAsync(call);
        int[] ids = scope.EmployeeIds;
        IQueryable<E_DonateLeave> rows = scope.All ? call.Context.E_DonateLeave
            : call.Context.E_DonateLeave.Where(d => ids.Contains(d.DonateFrom_EmployeeId) || ids.Contains(d.DonateTo_EmployeeId));
        return await rows.ToResultAsync(query, sorts, "whenDonated:desc", d => d.DonateLeaveId,
            text => d => d.Note != null && d.Note.Contains(text), DonateLeaveDto.From);
    }

    protected override async Task<IResult?> AuthorizeAsync(CrudCall call, int id, CrudAccess access)
    {
        var owners = await RowOwners.Donation(call.Context, id);
        var scope = await ScopeAsync(call);
        if (access == CrudAccess.Write)
            return scope.Deny(owners?.FromEmployeeId);
        if (owners == null)
            return Results.NotFound();
        return scope.Allows(owners.FromEmployeeId) || scope.Allows(owners.ToEmployeeId) ? null : Results.Forbid();
    }

    protected override Task<IResult?> PrepareCreateAsync(CrudCall call, E_DonateLeave row, DonateLeaveSave input) =>
        // the hours leave the caller's balance: who gives comes from the token
        Task.FromResult(OwnerFromToken(call, own => row.DonateFrom_EmployeeId = own));

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, E_DonateLeave row, DonateLeaveSave input)
    {
        var stored = await RowOwners.Donation(call.Context, id);
        if (await DenyAsync(call, stored?.FromEmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if the caller is not the giver
        row.DonateFrom_EmployeeId = stored!.FromEmployeeId; // the giver cannot be changed
        return null;
    }
}
