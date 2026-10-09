namespace TimeEntry.ApiService.Apis;

/// <summary> Employees, listed with their manager, department and team. </summary>
public class EmployeeApi : NamedCrudApi<Employee, Employee, Employee>
{
    protected override CrudStore<Employee> Store(TimeEntryContext context) => CrudStore.For(new EmployeeRepo(context));
    protected override int KeyOf(Employee row) => row.EmployeeId;
    protected override Employee ToEntity(Employee input) => input;
    protected override Employee ToOutput(Employee row) => row;

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok(await new EmployeeRepo(call.Context).GetAllIncludeDropdowns());

    protected override Task<List<Employee>> FindByNameAsync(TimeEntryContext context, string name) => new EmployeeRepo(context).GetByName(name);

    protected override void RegisterExtras(IEndpointRouteBuilder app, string route, string singular, string plural)
    {
        // Lookup: just the id and name of the active employees, for drop-downs and the name columns of the grids (E12). A literal segment wins over {name}.
        app.MapGet(route + "/lookup", GetLookup)
            .WithName($"Get{plural}Lookup")
            .Produces<List<EmployeeLookup>>()
            .ProducesProblem(500);

        base.RegisterExtras(app, route, singular, plural);
    }

    /// <summary> The active employees as id and name only: a Select of two columns, so SQL reads two columns and no manager, department or team is joined. </summary>
    private static async Task<IResult> GetLookup(TimeEntryContext context)
    {
        var rows = await context.Employee.AsNoTracking()
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name).ThenBy(e => e.EmployeeId)
            .Select(e => new EmployeeLookup(e.EmployeeId, e.Name))
            .ToListAsync();
        return TypedResults.Ok(rows);
    }
}
