namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class ProjectTaskApi<T> : BaseApi<T> where T : BaseNameActiveEntity
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string apiSubDir);

        // Get all 
        app.MapGet(apiSubDir, GetAllActive)
          .WithName($"Get{plural}")
          .Produces<IEnumerable<T>>()
          .ProducesProblem(404)
          .ProducesProblem(500);

        // special - Get all of Task 
        app.MapGet(apiSubDir + "/project/{id}", GetAllOfProject)
        .WithName($"Get{plural}OfProject")
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

    private static async Task<IResult> GetAllActive([FromServices] TimeEntryContext context)
    {
        ProjectTaskRepo repo = new(context);
        var rows = await repo.GetAllActive();
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        ProjectTaskRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> GetAllOfProject([FromServices] TimeEntryContext context, int id)
    {
        ProjectTaskRepo repo = new(context);
        var rows = await repo.GetAllOfProject(id);
        return Ok(rows);
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        NameActiveRepo<ProjectTask> repo = new(context);
        var rows = await repo.GetByName(name);
        return Results.Ok(rows); // no match is an empty list, not a 404
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] ProjectTask newRow)
    {
        newRow.Name = newRow.Name.Trim();
        if (newRow.Name.IsNameBad())
            return Results.BadRequest();  // 400 error if bad characters or empty

        ProjectTaskRepo repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api/projectTasks/{newRow.ProjectTaskId}", newRow);
        else
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name           
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] ProjectTask updatedRow)
    {
        if (updatedRow == null)
            return Results.NotFound();
        if (updatedRow.ProjectTaskId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        updatedRow.Name = updatedRow.Name.Trim();
        if (updatedRow.Name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        ProjectTaskRepo repo = new(context);
        if (!await repo.ExistsAsync(id))
            return Results.NotFound(); // 404 error if there is no row with that id
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        if (postUpdate == null)
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name

        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        ProjectTaskRepo repo = new(context);
        var successNum = await repo.DeleteAsync("ProjectTask", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}