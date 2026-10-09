namespace TimeEntry.ApiService.Apis;

/// <summary> The tasks of a project: the active ones as the list, and the tasks of one project under /project/{id}. </summary>
public class ProjectTaskApi : NamedCrudApi<ProjectTask, ProjectTask, ProjectTask>
{
    protected override CrudStore<ProjectTask> Store(TimeEntryContext context) => CrudStore.For(new ProjectTaskRepo(context));
    protected override int KeyOf(ProjectTask row) => row.ProjectTaskId;
    protected override ProjectTask ToEntity(ProjectTask input) => input;
    protected override ProjectTask ToOutput(ProjectTask row) => row;

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok(await new ProjectTaskRepo(call.Context).GetAllActive());

    protected override Task<List<ProjectTask>> FindByNameAsync(TimeEntryContext context, string name) => new ProjectTaskRepo(context).GetByName(name);

    protected override void RegisterExtras(IEndpointRouteBuilder app, string route, string singular, string plural)
    {
        app.MapGet(route + "/project/{id}", async (TimeEntryContext context, int id) => TypedResults.Ok(await new ProjectTaskRepo(context).GetAllOfProject(id)))
            .WithName($"Get{plural}OfProject")
            .Produces<IEnumerable<ProjectTask>>()
            .ProducesProblem(500);

        base.RegisterExtras(app, route, singular, plural);
    }
}
