namespace TimeEntry.ApiService.Apis;

/// <summary> A manager's answer to a request. Everyone signed in reads them; Admin, Human Resources and managers write them (a filter on the group). </summary>
public class ResponseApi : CrudApi<Response, ResponseSave, ResponseDto>
{
    private static readonly SortMap<Response> sorts = new SortMap<Response>()
        .Add("whenResponded", r => r.WhenResponded).Add("responseId", r => r.ResponseId).Add("request", r => r.E_RequestId)
        .Add("manager", r => r.ManagerId).Add("responseType", r => r.ResponseTypeId);

    protected override CrudStore<Response> Store(TimeEntryContext context) => CrudStore.For(new ResponseRepo(context));
    protected override int KeyOf(Response row) => row.ResponseId;
    protected override Response ToEntity(ResponseSave input) => input.ToEntity();
    protected override ResponseDto ToOutput(Response row) => ResponseDto.From(row);

    protected override bool PagedList => true;

    /// <summary> The whole list, or (with pageIndex / pageSize) one page of it; sort applies to both. See <see cref="ListQuery"/>. </summary>
    protected override Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        call.Context.Response.ToResultAsync(query, sorts, "whenResponded:desc", r => r.ResponseId, null, ResponseDto.From);
}
