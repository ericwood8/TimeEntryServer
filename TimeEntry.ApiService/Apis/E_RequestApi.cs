﻿using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Apis;

using static Microsoft.AspNetCore.Http.TypedResults;

public class E_RequestApi<T> : BaseApi<T> where T : class
{
    public override void Register(WebApplication app)
    {
        BreakIntoStrings(out string singular, out string plural, out string _apiSubDir);

        // Get all
        app.MapGet(_apiSubDir, GetAll)
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
        .ProducesProblem(500);

        // Create new 
        app.MapPost(_apiSubDir, CreateRow)
        .WithName($"Create{singular}")
        .WithOpenApi()
        .ProducesProblem(400)
        .ProducesProblem(500);

        // Update existing 
        app.MapPut(_apiSubDir + "/{id:int}", UpdateRow)
        .WithName($"Update{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);

        // Delete 
        app.MapDelete(_apiSubDir + "/{id:int}", DeleteRow)
        .WithName($"Delete{singular}")
        .WithOpenApi()
        .ProducesProblem(404)
        .ProducesProblem(500);
    }

    private static async Task<IResult> GetAll([FromServices] TimeEntryContext context)
    {
        GenericRepo<E_Request> repo = new(context);
        var rows = await repo.GetAllOrderByDescending(c => c.WhenRequested);
        return Ok(rows);
    }

    private static async Task<IResult> GetById([FromServices] TimeEntryContext context, int id)
    {
        GenericRepo<E_Request> repo = new(context);
        var row = await repo.GetByIdAsync(id);
        return row != null ? Results.Ok(row) : Results.NotFound();
    }

    private static async Task<IResult> CreateRow([FromServices] TimeEntryContext context, [FromBody] E_Request newRow)
    {
        if ((newRow.OvertimeHrsRequested != null) && (newRow.OvertimeHrsRequested <= 0))
        {
            return Results.BadRequest(); // 400 error if over time bad
        }
        else if ((newRow.LeaveTypeId != null) && (newRow.LeaveStart == null))
        {
            return Results.BadRequest(); // 400 error if bad
        }
        else if ((newRow.LeaveTypeId != null) && (newRow.LeaveEnd == null))
        {
            return Results.BadRequest(); // 400 error if bad
        }

        newRow.StatusDate = DateTime.Now;

        // Assume at least they are pending.
        if (newRow.SY_RequestStatusTypeId == 0)
        {            
            newRow.SY_RequestStatusTypeId = (int)SY_RequestStatusType.Pending;
        }

        // Pending so set WhenRequested date 
        if (newRow.SY_RequestStatusTypeId == (int)SY_RequestStatusType.Pending)
        {
            newRow.WhenRequested = DateTime.Now;
        }

        GenericRepo<E_Request> repo = new(context);
        bool success = await repo.AddAsync(newRow);
        if (success)
            return Results.Created($"/api{_apiSubDir}/{newRow.RequestId}", newRow);
        else
            return Results.NoContent();
    }

    private static async Task<IResult> UpdateRow([FromServices] TimeEntryContext context, int id, [FromBody] E_Request updatedRow)
    {
        //if (rowToUpdate.SY_RequestStatusTypeId != updatedRow.SY_RequestStatusTypeId)
        //{
        //    rowToUpdate.StatusDate = DateTime.Now;
        //}

        GenericRepo<E_Request> repo = new(context);
        var postUpdate = await repo.UpdateAsync(id, updatedRow);
        return Results.Ok(postUpdate);
    }

    private static async Task<IResult> DeleteRow([FromServices] TimeEntryContext context, int id)
    {
        GenericRepo<E_Request> repo = new(context);
        var successNum = await repo.DeleteAsync("E_Request", id);
        if (successNum == 0)
            return Results.Ok();
        else if (successNum == -1)
            return Results.NotFound(); // cannot delete because does not exist
        else
            return Results.BadRequest(); // cannot delete because "in use"
    }
}