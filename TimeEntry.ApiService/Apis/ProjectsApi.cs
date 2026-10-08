namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class ProjectsApi<T> : BaseApi<T> where T : BaseNameActiveEntity
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string apiSubDir);

        // Get all 
        app.MapGet(apiSubDir, GetAllIncludeTasks)
          .WithName($"Get{plural}")
          .Produces<IEnumerable<T>>()
          .ProducesProblem(404)
          .ProducesProblem(500);

        // Get by ID
        app.MapGet(apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .Produces<T>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new
        app.MapPost(apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .ProducesProblem(400)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Get by Name
        app.MapGet(apiSubDir + "/{name}", GetByName)
        .WithName($"Get{singular}ByName")
        .Produces<List<T>>()
        .ProducesProblem(400)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllIncludeTasks([FromServices]TimeEntryContext context)
    {
        ProjectRepo repo = new(context);
        var rows = await repo.GetAllIncludeTasks();
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        ProjectRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return ApiProblems.BadName(); // 400 error if bad characters or empty

        ProjectRepo repo = new(context);
        var rows = await repo.GetByName(name);
        return Results.Ok(rows); // no match is an empty list, not a 404
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] Project newRow)
    {
        newRow.Name = newRow.Name.Trim();

        if (newRow.Name.IsNameBad())
            return ApiProblems.BadName();  // 400 error if bad characters or empty

        ProjectRepo repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api/projects/{newRow.ProjectId}", newRow);
        else
            return ApiProblems.DuplicateName(); // 422 error if Duplicate Name       
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] Project updatedRow)
    {
        if (updatedRow == null)
            return Results.NotFound();
        if (updatedRow.ProjectId != id)
            return ApiProblems.IdMismatch(); // 400 error if the id in the URL and the id in the body disagree

        updatedRow.Name = updatedRow.Name.Trim();
        if (updatedRow.Name.IsNameBad())
            return ApiProblems.BadName(); // 400 error if bad characters or empty

        ProjectRepo repo = new(context);
        if (!await repo.ExistsAsync(id))
            return ApiProblems.NotFound(); // 404 error if there is no row with that id
        var postUpdate = await repo.UpdateAsync(id, updatedRow);

        if (postUpdate == null)
            return ApiProblems.DuplicateName(); // 422 error if Duplicate Name

        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        ProjectRepo repo = new(context);
        var successNum = await repo.DeleteAsync("Project", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return ApiProblems.NotFound(); // cannot delete because does not exist
        else
            return ApiProblems.InUse(); // cannot delete because "in use"
    }
}