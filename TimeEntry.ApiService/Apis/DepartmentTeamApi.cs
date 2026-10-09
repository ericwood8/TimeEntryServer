namespace TimeEntry.ApiService.Apis;

/// <summary> The teams of a department. They are listed under their department, not as one list. </summary>
public class DepartmentTeamApi : NamedCrudApi<DepartmentTeam, DepartmentTeam, DepartmentTeam>
{
    protected override CrudStore<DepartmentTeam> Store(TimeEntryContext context) => CrudStore.For(new DepartmentTeamRepo(context));
    protected override int KeyOf(DepartmentTeam row) => row.DepartmentTeamId;
    protected override DepartmentTeam ToEntity(DepartmentTeam input) => input;
    protected override DepartmentTeam ToOutput(DepartmentTeam row) => row;

    protected override bool HasList => false;

    protected override Task<List<DepartmentTeam>> FindByNameAsync(TimeEntryContext context, string name) => new DepartmentTeamRepo(context).GetByName(name);

    protected override void RegisterExtras(IEndpointRouteBuilder app, string route, string singular, string plural)
    {
        app.MapGet(route + "/department/{id}", async (TimeEntryContext context, int id) => TypedResults.Ok(await new DepartmentTeamRepo(context).GetAllOfDepartment(id)))
            .WithName($"Get{plural}OfDepartment")
            .Produces<IEnumerable<DepartmentTeam>>()
            .ProducesProblem(500);

        base.RegisterExtras(app, route, singular, plural);
    }
}
