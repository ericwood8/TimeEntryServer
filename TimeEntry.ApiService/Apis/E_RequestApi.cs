using TimeEntry.Common.Enums;

using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_RequestApi<T> : BaseApi<T> where T : class
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAll)
       .WithName($"Get{plural}")
       .WithOpenApi()
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(_apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .WithOpenApi()
        .Produces<T>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new 
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    /// <summary> Only someone other than the requester (or Admin / Human Resources) may approve, reject, reimburse or void a request. </summary>
    private static bool IsDecision(int statusId) =>
        statusId is (int)SY_RequestStatusType.Approved or (int)SY_RequestStatusType.Rejected
            or (int)SY_RequestStatusType.Reimbursed or (int)SY_RequestStatusType.Voided;

    private static bool MayDecideOwn(ClaimsPrincipal user, int ownerEmployeeId) =>
        user.CanManageAll() || user.EmployeeId() != ownerEmployeeId;

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context, ClaimsPrincipal user)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        int[] ids = scope.EmployeeIds;
        E_RequestRepo repo = new(context);
        var rows = await repo.GetAllOrderByDescending(c => c.WhenRequested, scope.All ? null : r => ids.Contains(r.EmployeeId));
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        E_RequestRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        if (row == null)
            return Results.NotFound();
        return scope.Deny(row.EmployeeId) ?? Results.Ok(row);
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, [FromBody] E_Request newRow)
    {
        if ((newRow.OvertimeHrsRequested != null) && (newRow.OvertimeHrsRequested <= 0))
        {
            return Results.BadRequest(); // 400 error if over time bad
        }
        else if ((newRow.LeaveTypeId != null) && (newRow.LeaveStart == null))
        {
            return Results.BadRequest(); // 400 error if bad
        }
        else if ((newRow.LeaveTypeId != null) && (newRow.LeaveEnd == null))
        {
            return Results.BadRequest(); // 400 error if bad
        }

        // whose request it is comes from the token; only Admin and Human Resources may enter one for somebody else
        if (!user.CanManageAll())
        {
            if (user.EmployeeId() == null)
                return Results.Forbid();
            newRow.EmployeeId = user.EmployeeId()!.Value;
        }

        newRow.StatusDate = DateTime.Now;

        // Assume at least they are pending.
        if (newRow.SY_RequestStatusTypeId == 0)
        {
            newRow.SY_RequestStatusTypeId = (int)SY_RequestStatusType.Pending;
        }

        // nobody approves their own request by creating it that way
        if (IsDecision(newRow.SY_RequestStatusTypeId) && !MayDecideOwn(user, newRow.EmployeeId))
            return Results.Forbid();

        // Pending so set WhenRequested date
        if (newRow.SY_RequestStatusTypeId == (int)SY_RequestStatusType.Pending)
        {
            newRow.WhenRequested = DateTime.Now;
        }

        E_RequestRepo repo = new(context);
        await repo.AddAsync(newRow);
        return Results.Created($"/api{_apiSubDir}/{newRow.RequestId}", newRow);
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id, [FromBody] E_Request updatedRow)
    {
        if (updatedRow.RequestId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        var scope = await EmployeeScope.ForAsync(context, user);
        var stored = await RowOwners.Request(context, id);
        if (scope.Deny(stored?.EmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        updatedRow.EmployeeId = stored!.EmployeeId; // the request cannot be handed to someone else
        updatedRow.WhenRequested = stored.WhenRequested; // an edit does not change when it was first asked

        if (updatedRow.SY_RequestStatusTypeId != stored.StatusId
            && IsDecision(updatedRow.SY_RequestStatusTypeId)
            && !MayDecideOwn(user, stored.EmployeeId))
            return Results.Forbid(); // a person cannot approve their own request

        // the status date moves only when the status does
        updatedRow.StatusDate = updatedRow.SY_RequestStatusTypeId != stored.StatusId ? DateTime.Now : stored.StatusDate;

        E_RequestRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, ClaimsPrincipal user, int id)
    {
        var scope = await EmployeeScope.ForAsync(context, user);
        if (scope.Deny((await RowOwners.Request(context, id))?.EmployeeId) is { } denied)
            return denied;

        E_RequestRepo repo = new(context);
        var successNum = await repo.DeleteAsync("E_Request", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}
