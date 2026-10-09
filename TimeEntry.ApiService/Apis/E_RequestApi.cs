using TimeEntry.ApiService.Security;
using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Apis;

/// <summary> An employee's requests (clearance, overtime, leave, expenses) and the rules about who may decide them. </summary>
public class E_RequestApi : OwnedCrudApi<E_Request, RequestSave, RequestDto>
{
    private static readonly SortMap<E_Request> sorts = new SortMap<E_Request>()
        .Add("whenRequested", r => r.WhenRequested).Add("requestId", r => r.RequestId).Add("employeeId", r => r.EmployeeId)
        .Add("employee", r => r.Employee!.Name).Add("status", r => r.SY_RequestStatusTypeId).Add("leaveStart", r => r.LeaveStart)
        .Add("leaveEnd", r => r.LeaveEnd).Add("statusDate", r => r.StatusDate).Add("reason", r => r.Reason);

    protected override CrudStore<E_Request> Store(TimeEntryContext context) => CrudStore.For(new E_RequestRepo(context));
    protected override int KeyOf(E_Request row) => row.RequestId;
    protected override E_Request ToEntity(RequestSave input) => input.ToEntity();
    protected override RequestDto ToOutput(E_Request row) => RequestDto.From(row);
    protected override async Task<int?> OwnerOfAsync(CrudCall call, int id) => (await RowOwners.Request(call.Context, id))?.EmployeeId;

    protected override bool PagedList => true;

    /// <summary> Only someone other than the requester (or Admin / Human Resources) may approve, reject, reimburse or void a request. </summary>
    private static bool IsDecision(int statusId) =>
        statusId is (int)SY_RequestStatusType.Approved or (int)SY_RequestStatusType.Rejected
            or (int)SY_RequestStatusType.Reimbursed or (int)SY_RequestStatusType.Voided;

    private static bool MayDecideOwn(ClaimsPrincipal user, int ownerEmployeeId) =>
        user.CanManageAll() || user.EmployeeId() != ownerEmployeeId;

    /// <summary> The whole list, or (with pageIndex / pageSize) one page of it; sort and search apply to both. See <see cref="ListQuery"/>. </summary>
    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query)
    {
        var scope = await ScopeAsync(call);
        int[] ids = scope.EmployeeIds;
        int[] named = await call.Context.EmployeeIdsNamedAsync(query);
        IQueryable<E_Request> rows = scope.All ? call.Context.E_Request : call.Context.E_Request.Where(r => ids.Contains(r.EmployeeId));
        return await rows.ToResultAsync(query, sorts, "whenRequested:desc", r => r.RequestId,
            text => r => (r.Reason != null && r.Reason.Contains(text)) || EF.Constant(named).Contains(r.EmployeeId), RequestDto.From);
    }

    protected override Task<IResult?> PrepareCreateAsync(CrudCall call, E_Request newRow, RequestSave input)
    {
        if ((newRow.OvertimeHrsRequested != null) && (newRow.OvertimeHrsRequested <= 0))
            return Task.FromResult<IResult?>(ApiProblems.Invalid("Overtime hours requested must be more than zero."));
        if ((newRow.LeaveTypeId != null) && (newRow.LeaveStart == null))
            return Task.FromResult<IResult?>(ApiProblems.Invalid("A leave request needs a start date."));
        if ((newRow.LeaveTypeId != null) && (newRow.LeaveEnd == null))
            return Task.FromResult<IResult?>(ApiProblems.Invalid("A leave request needs an end date."));

        if (OwnerFromToken(call, own => newRow.EmployeeId = own) is { } forbidden)
            return Task.FromResult<IResult?>(forbidden);

        newRow.StatusDate = DateTime.Now;

        // Assume at least they are pending.
        if (newRow.SY_RequestStatusTypeId == 0)
            newRow.SY_RequestStatusTypeId = (int)SY_RequestStatusType.Pending;

        // nobody approves their own request by creating it that way
        if (IsDecision(newRow.SY_RequestStatusTypeId) && !MayDecideOwn(call.User, newRow.EmployeeId))
            return Task.FromResult<IResult?>(Results.Forbid());

        newRow.WhenRequested = DateTime.Now; // the server sets it
        return Task.FromResult<IResult?>(null);
    }

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, E_Request updatedRow, RequestSave input)
    {
        var stored = await RowOwners.Request(call.Context, id);
        if (await DenyAsync(call, stored?.EmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        updatedRow.EmployeeId = stored!.EmployeeId; // the request cannot be handed to someone else
        updatedRow.WhenRequested = stored.WhenRequested; // an edit does not change when it was first asked

        if (updatedRow.SY_RequestStatusTypeId != stored.StatusId
            && IsDecision(updatedRow.SY_RequestStatusTypeId)
            && !MayDecideOwn(call.User, stored.EmployeeId))
            return ApiProblems.Forbidden("A person cannot approve, reject, reimburse or void their own request.");

        // the status date moves only when the status does
        updatedRow.StatusDate = updatedRow.SY_RequestStatusTypeId != stored.StatusId ? DateTime.Now : stored.StatusDate;
        return null;
    }
}
