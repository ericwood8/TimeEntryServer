using TimeEntry.ApiService.Security;
using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class TimeEntryUserApi<T> : BaseApi<T> where T : class
{
    public override void Register(IEndpointRouteBuilder app)
    {
        BreakIntoStrings(out string singular, out string plural, out string apiSubDir);

        // Get all
        app.MapGet(apiSubDir, GetAll)
       .WithName($"Get{plural}")
       .WithOpenApi()
       .Produces<IEnumerable<TimeEntryUserDto>>()
       .ProducesProblem(404)
       .ProducesProblem(500);

        // Get by ID
        app.MapGet(apiSubDir + "/{id:int}", GetById)
        .WithName($"Get{singular}ById")
        .WithOpenApi()
        .Produces<TimeEntryUserDto>()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Create new 
        app.MapPost(apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(422)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Get by Name
        app.MapGet(apiSubDir + "/{name}", GetByName)
        .WithName($"Get{singular}ByName")
        .WithOpenApi()
        .Produces<TimeEntryUserDto>()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context)
    {
        TimeEntryUserRepo repo = new(context);
        var rows = await repo.GetAllActive();
        return Ok(rows.Select(TimeEntryUserDto.From));
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        TimeEntryUserRepo repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(TimeEntryUserDto.From(row)) : Results.NotFound();
    }

    private static async Task<IResult> GetByName([FromServices] TimeEntryContext context, string name)
    {
        if (name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty

        TimeEntryUserRepo repo = new(context);
        var rows = await repo.GetByName(name);
        return Results.Ok(rows.Select(TimeEntryUserDto.From));
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromServices] PasswordService passwords, [FromBody] TimeEntryUserSave save)
    {
        string name = (save.Name ?? "").Trim();
        if (name.IsNameBad())
            return Results.BadRequest();  // 400 error if bad characters or empty
        if (!PasswordService.IsAcceptable(save.Password) || string.IsNullOrWhiteSpace(save.Answer))
            return Results.BadRequest(); // a new user needs a password of the minimum strength and a security answer
        if (!Enum.IsDefined(typeof(SY_Role), save.SY_RoleId))
            return Results.BadRequest();

        TimeEntryUser newRow = new()
        {
            TimeEntryUserId = 0,
            Name = name,
            SY_RoleId = save.SY_RoleId,
            EmployeeId = save.EmployeeId,
            Pword = "",
            Hint = save.Hint?.Trim() ?? "",
            Answer = "",
            IsTwoFactorAuth = save.IsTwoFactorAuth,
            PhoneNumber = save.PhoneNumber,
            IsActive = save.IsActive,
            WhenLeft = save.WhenLeft,
        };
        newRow.Pword = passwords.Hash(newRow, save.Password!);
        newRow.Answer = passwords.HashAnswer(newRow, save.Answer);

        TimeEntryUserRepo repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api{_apiSubDir}/{newRow.TimeEntryUserId}", TimeEntryUserDto.From(newRow));
        else
            return Results.UnprocessableEntity(); // 422 error if Duplicate Name
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, [FromServices] PasswordService passwords, int id, [FromBody] TimeEntryUserSave save)
    {
        if (save.TimeEntryUserId != id)
            return Results.BadRequest(); // 400 error if the id in the URL and the id in the body disagree

        string name = (save.Name ?? "").Trim();
        if (name.IsNameBad())
            return Results.BadRequest(); // 400 error if bad characters or empty
        if (!Enum.IsDefined(typeof(SY_Role), save.SY_RoleId))
            return Results.BadRequest();
        if (!string.IsNullOrEmpty(save.Password) && !PasswordService.IsAcceptable(save.Password))
            return Results.BadRequest();

        TimeEntryUserRepo repo = new(context);
        TimeEntryUser? row = await repo.GetByIdAsync(id);
        if (row == null)
            return Results.NotFound(); // 404 error if there is no row with that id

        // only the listed columns are copied, so the stored Pword and Answer survive unless a new one is sent
        row.Name = name;
        row.SY_RoleId = save.SY_RoleId;
        row.EmployeeId = save.EmployeeId;
        row.Hint = save.Hint?.Trim() ?? row.Hint;
        row.IsTwoFactorAuth = save.IsTwoFactorAuth;
        row.PhoneNumber = save.PhoneNumber;
        row.IsActive = save.IsActive;
        row.WhenLeft = save.WhenLeft;
        if (!string.IsNullOrEmpty(save.Password))
            row.Pword = passwords.Hash(row, save.Password);
        if (!string.IsNullOrWhiteSpace(save.Answer))
            row.Answer = passwords.HashAnswer(row, save.Answer);

        await context.SaveChangesAsync();
        return Results.Ok(TimeEntryUserDto.From(row));
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        TimeEntryUserRepo repo = new(context);
        var successNum = await repo.DeleteAsync("TimeEntryUser", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}
