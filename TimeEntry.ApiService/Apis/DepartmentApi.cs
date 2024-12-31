namespace TimeEntry.ApiService.Apis;

using Microsoft.IdentityModel.Tokens;
using static Microsoft.AspNetCore.Http.TypedResults;

public class DepartmentApi<T> : BaseApi<T> where T : BaseNameActiveEntity
{
    public DepartmentApi()
    {

    }

    public override void Register(WebApplication app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAllIncludeTeams)
       .WithName($"Get{plural}")
       .WithOpenApi()
       .Produces<IEnumerable<T>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(_apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .WithOpenApi()
        .Produces<T>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new 
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Get by Name
        app.MapGet(_apiSubDir + "/{name}", GetByName)
        .WithName($"Get{singular}ByName")
        .WithOpenApi()
        .Produces<T>()
        .ProducesValidationProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAllIncludeTeams([FromServices] TimeEntryContext context)
    {
        DepartmentRepo repo = new(context);
        var rows = await repo.GetAllIncludeTeams();
        return Ok(rows);
    }

    //private static async Task<Results<Ok<PaginatedItems<Department>>, BadRequest<string>>> GetPage([FromServices] TimeEntryContext context, [AsParameters] PaginationRequest paginationRequest)
    //{        
    //    var pageIndex = paginationRequest.PageIndex;
    //    var totalItems = await GetContext(context).LongCountAsync();
    //    var itemsOnPage = await GetContext(context)
    //        .OrderBy(c => c.Name)
    //        .Skip(_pageSize * pageIndex)
    //        .Take(_pageSize)
    //        .ToListAsync();

    //    return Ok(new PaginatedItems<Department>(pageIndex, totalItems, itemsOnPage));
    //}

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        DepartmentRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        DepartmentRepo repo = new(context);
        var rows = await repo.GetByName(name);
        return rows != null ? Results.Ok(rows) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] Department newRow)
    {
        newRow.Name = newRow.Name.Trim();
        if (newRow.Name.IsNameBad())
            return Results.BadRequest();  // 400 error if bad characters or empty

        DepartmentRepo repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api{_apiSubDir}/{newRow.DepartmentId}", newRow);
        else 
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] Department updatedRow)
    {
        if (updatedRow == null) 
            return Results.NotFound();

        updatedRow.Name = updatedRow.Name.Trim();

        if (updatedRow.Name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        //if (departmentRepo.IsDupOnUpdate(id, updatedRow.Name))
        //   return Results.UnprocessableEntity(); // 422 error if Duplicate Name

        List<DepartmentTeam> preUpdateTeams = (updatedRow.Teams.IsNullOrEmpty()) ? [] : new(updatedRow.Teams);

        DepartmentRepo repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);

        // ----- now fix the team(s) associated with the department ----
        DepartmentTeamRepo teamRepo = new(context);
        List<DepartmentTeam> postUpdateTeams = teamRepo.GetList(x => x.IsActive && x.DepartmentId.Equals(id));

        var teamsToDelete = postUpdateTeams?.Where(t => !preUpdateTeams!.Any(s => s.Name == t.Name)).ToList();
        if (teamsToDelete!.Count > 0)
        {
            teamRepo.RemoveRange(teamsToDelete!);
        }

        var adjustedRow = await repo.GetByIdIncludeTeams(id);
        if (adjustedRow != null)
        {
            adjustedRow.Teams = updatedRow.Teams;
            await context.SaveChangesAsync();
        }
        
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        DepartmentRepo repo = new(context);
        var successNum = await repo.DeleteAsync("Department", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}