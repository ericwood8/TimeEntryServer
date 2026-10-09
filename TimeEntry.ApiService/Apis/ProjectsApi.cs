namespace TimeEntry.ApiService.Apis;

/// <summary> Projects, listed with their tasks. </summary>
public class ProjectsApi : NamedCrudApi<Project, Project, Project>
{
    protected override CrudStore<Project> Store(TimeEntryContext context) => CrudStore.For(new ProjectRepo(context));
    protected override int KeyOf(Project row) => row.ProjectId;
    protected override Project ToEntity(Project input) => input;
    protected override Project ToOutput(Project row) => row;

    protected override async Task<IResult> ListAsync(CrudCall call, ListQuery query) =>
        TypedResults.Ok(await new ProjectRepo(call.Context).GetAllIncludeTasks());

    protected override Task<List<Project>> FindByNameAsync(TimeEntryContext context, string name) => new ProjectRepo(context).GetByName(name);
}
