namespace TimeEntry.ApiService.Apis;

/// <summary> Holidays: a name is checked, but two holidays may share one. Newest first. </summary>
public class HolidayApi : NamedCrudApi<Holiday, Holiday, Holiday>
{
    protected override CrudStore<Holiday> Store(TimeEntryContext context) => CrudStore.For(new HolidayRepo(context));
    protected override int KeyOf(Holiday row) => row.HolidayId;
    protected override Holiday ToEntity(Holiday input) => input;
    protected override Holiday ToOutput(Holiday row) => row;

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok(await new HolidayRepo(call.Context).GetAllOrderByDescending(c => c.Date));

    protected override Task<List<Holiday>> FindByNameAsync(TimeEntryContext context, string name) => new HolidayRepo(context).GetByName(name);
}
