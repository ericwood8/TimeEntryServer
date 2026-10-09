using TimeEntry.ApiService.Security;
using TimeEntry.Common.Models;

namespace TimeEntry.ApiService.Apis;

/// <summary>
/// The hours of one project task in a time sheet. A detail belongs to whoever owns its time sheet and cannot be moved to another; the caller gets it back with the
/// project and task names. Details are listed under their time sheet, not as one list.
/// </summary>
public class E_TimeSheetDetailApi : OwnedCrudApi<E_TimeSheetDetail, TimeSheetDetailSave, TimeSheetDetailRow>
{
    protected override CrudStore<E_TimeSheetDetail> Store(TimeEntryContext context) => CrudStore.For(new E_TimeSheetDetailRepo(context));
    protected override int KeyOf(E_TimeSheetDetail row) => row.TimeSheetDetailId;
    protected override E_TimeSheetDetail ToEntity(TimeSheetDetailSave input) => input.ToEntity();
    protected override async Task<int?> OwnerOfAsync(CrudCall call, int id) => (await RowOwners.TimeSheetDetail(call.Context, id))?.EmployeeId;

    protected override bool HasList => false;

    // the output carries the project and task names, which only the repository can read
    protected override Task<TimeSheetDetailRow?> ReadAsync(CrudCall call, int id) => new E_TimeSheetDetailRepo(call.Context).GetRowWithNames(id);

    protected override async Task<TimeSheetDetailRow> OutputAsync(CrudCall call, E_TimeSheetDetail saved) =>
        (await new E_TimeSheetDetailRepo(call.Context).GetRowWithNames(saved.TimeSheetDetailId))!;

    protected override async Task<IResult?> PrepareCreateAsync(CrudCall call, E_TimeSheetDetail row, TimeSheetDetailSave input)
    {
        // a detail may only be added to a time sheet the caller may use
        int? sheetOwner = await RowOwners.TimeSheet(call.Context, row.E_TimeSheetId);
        if (sheetOwner == null)
            return ApiProblems.Invalid("There is no time sheet with that id."); // 400 error if the time sheet does not exist
        return await DenyAsync(call, sheetOwner);
    }

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, E_TimeSheetDetail row, TimeSheetDetailSave input)
    {
        var owner = await RowOwners.TimeSheetDetail(call.Context, id);
        if (await DenyAsync(call, owner?.EmployeeId) is { } denied)
            return denied; // 404 if there is no row with that id, 403 if it is not the caller's
        row.E_TimeSheetId = owner!.ParentId; // the detail cannot be moved to another time sheet
        return null;
    }

    protected override void RegisterExtras(IEndpointRouteBuilder app, string route, string singular, string plural)
    {
        app.MapGet(route + "/timeSheet/{timesheetId:int}", GetAllOfTimesheet)
            .WithName($"Get{plural}OfTimeSheet")
            .Produces<IEnumerable<TimeSheetDetailRow>>()
            .ProducesProblem(403)
            .ProducesProblem(404)
            .ProducesProblem(500);

        base.RegisterExtras(app, route, singular, plural);
    }

    private static async Task<IResult> GetAllOfTimesheet(TimeEntryContext context, HttpContext http, int timesheetId)
    {
        var call = Call(context, http);
        if (await DenyAsync(call, await RowOwners.TimeSheet(context, timesheetId)) is { } denied)
            return denied;
        return TypedResults.Ok(await new E_TimeSheetDetailRepo(context).GetAllOfTimesheetWithNames(timesheetId));
    }
}
