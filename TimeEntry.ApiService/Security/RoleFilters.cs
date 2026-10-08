namespace TimeEntry.ApiService.Security;

/// <summary> Anyone signed in may read; only Admin and Human Resources may add, change or delete. Used for the reference data (departments, projects, holidays, ...). </summary>
public class ManageWritesFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        bool isRead = HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method);
        if (!isRead && !http.User.CanManageAll())
            return Results.Forbid();
        return await next(context);
    }
}

/// <summary> Anyone signed in may read; Admin, Human Resources and managers may add, change or delete (a manager's response to a request). </summary>
public class ManagerWritesFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        bool isRead = HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method);
        if (!isRead && !(http.User.CanManageAll() || http.User.Role() == TimeEntry.Common.Enums.SY_Role.Manager))
            return Results.Forbid();
        return await next(context);
    }
}

/// <summary> Only Admin may call the endpoints in the group (user maintenance). </summary>
public class AdminOnlyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.User.IsAdmin())
            return Results.Forbid();
        return await next(context);
    }
}
