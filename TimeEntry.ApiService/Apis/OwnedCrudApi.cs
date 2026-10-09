using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

/// <summary>
/// A row that belongs to an employee: the caller may use it when it is theirs, or belongs to someone who reports to them, or when they are Admin or
/// Human Resources (<see cref="EmployeeScope"/>). Reading and deleting check that through <see cref="OwnerOfAsync"/>; a class adds what a create and an update
/// must do with the owner.
/// </summary>
public abstract class OwnedCrudApi<TEntity, TInput, TOutput> : CrudApi<TEntity, TInput, TOutput>
    where TEntity : class
    where TInput : class
    where TOutput : class
{
    /// <summary> The employee who owns the stored row, or null when there is no such row (that is a 404). </summary>
    protected abstract Task<int?> OwnerOfAsync(CrudCall call, int id);

    protected static Task<EmployeeScope> ScopeAsync(CrudCall call) => EmployeeScope.ForAsync(call.Context, call.User);

    /// <summary> 404 when there is no such owner, 403 when the caller may not use that employee's rows, else null. </summary>
    protected static async Task<IResult?> DenyAsync(CrudCall call, int? owner) => (await ScopeAsync(call)).Deny(owner);

    protected override async Task<IResult?> AuthorizeAsync(CrudCall call, int id, CrudAccess access) => await DenyAsync(call, await OwnerOfAsync(call, id));

    protected override async Task<IResult?> PrepareUpdateAsync(CrudCall call, int id, TEntity row, TInput input) =>
        await AuthorizeAsync(call, id, CrudAccess.Write);

    /// <summary>
    /// Whose row a new one is comes from the token; only Admin and Human Resources may enter one for somebody else. Null to go on, 403 for a caller who
    /// is not an employee.
    /// </summary>
    protected static IResult? OwnerFromToken(CrudCall call, Action<int> setOwner)
    {
        if (call.User.CanManageAll())
            return null;
        if (call.User.EmployeeId() is not { } own)
            return Results.Forbid();
        setOwner(own);
        return null;
    }
}
