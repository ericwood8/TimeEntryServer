namespace TimeEntry.ApiService.Apis;

/// <summary> Periods in which leave may not be taken. Latest end first. </summary>
public class RestrictLeaveApi : CrudApi<RestrictLeave, RestrictLeave, RestrictLeave>
{
    protected override CrudStore<RestrictLeave> Store(TimeEntryContext context) => CrudStore.For(new RestrictLeaveRepo(context));
    protected override int KeyOf(RestrictLeave row) => row.RestrictLeaveId;
    protected override RestrictLeave ToEntity(RestrictLeave input) => input;
    protected override RestrictLeave ToOutput(RestrictLeave row) => row;

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok(await new RestrictLeaveRepo(call.Context).GetAllOrderByDescending(c => c.ToDateTime));
}
