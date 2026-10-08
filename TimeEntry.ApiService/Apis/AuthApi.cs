using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TimeEntry.ApiService.Security;

namespace TimeEntry.ApiService.Apis;

/// <summary> Sign in (anonymous, rate limited), who am I, and change my own password. </summary>
public class AuthApi : IApi
{
    public const string LoginRateLimitPolicy = "login";

    // a valid hash of a throwaway password: checked when the user name is unknown so that "no such user" and "wrong password" take the same time
    private static readonly TimeEntryUser _noSuchUser = new()
    {
        TimeEntryUserId = 0, SY_RoleId = 0, Name = "", Pword = "", Hint = "", Answer = "", IsActive = false, IsTwoFactorAuth = false,
    };
    private static readonly Lazy<string> _noSuchUserHash = new(() => new PasswordService().Hash(_noSuchUser, Guid.NewGuid().ToString("N")));

    public void Register(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", Login)
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimitPolicy)
            .WithName("Login")
            .Produces<LoginResponse>()
            .ProducesProblem(400)
            .ProducesProblem(401)
            .ProducesProblem(429);

        app.MapGet("/auth/me", Me)
            .WithName("GetCurrentUser")
            .Produces<TimeEntryUserDto>()
            .ProducesProblem(401);

        app.MapPost("/auth/change-password", ChangePassword)
            .WithName("ChangeMyPassword")
            .ProducesProblem(400)
            .ProducesProblem(401);
    }

    private static async Task<IResult> Login([FromServices] TimeEntryContext context, [FromServices] PasswordService passwords, [FromServices] TokenService tokens, [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
            return Results.BadRequest();

        string userName = request.UserName.Trim();
        TimeEntryUser? user = await context.TimeEntryUser.FirstOrDefaultAsync(u => u.Name == userName);
        if (user == null)
        {
            passwords.Verify(_noSuchUser, _noSuchUserHash.Value, request.Password);
            return Results.Unauthorized();
        }

        PasswordVerificationResult result = passwords.Verify(user, user.Pword, request.Password);
        bool allowedIn = user.IsActive && (user.WhenLeft == null || user.WhenLeft > DateTime.Now);
        if (result == PasswordVerificationResult.Failed || !allowedIn)
            return Results.Unauthorized(); // the same answer for every reason, so it does not tell which part was wrong

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Pword = passwords.Hash(user, request.Password);
            await context.SaveChangesAsync();
        }

        var (token, expiresUtc) = tokens.Create(user);
        return Results.Ok(new LoginResponse(token, expiresUtc, TimeEntryUserDto.From(user)));
    }

    private static async Task<IResult> Me([FromServices] TimeEntryContext context, ClaimsPrincipal caller)
    {
        TimeEntryUser? user = await FindCaller(context, caller);
        return user == null ? Results.Unauthorized() : Results.Ok(TimeEntryUserDto.From(user));
    }

    private static async Task<IResult> ChangePassword([FromServices] TimeEntryContext context, [FromServices] PasswordService passwords, ClaimsPrincipal caller, [FromBody] ChangePasswordRequest request)
    {
        TimeEntryUser? user = await FindCaller(context, caller);
        if (user == null)
            return Results.Unauthorized();
        if (!PasswordService.IsAcceptable(request.NewPassword))
            return Results.BadRequest();
        if (passwords.Verify(user, user.Pword, request.CurrentPassword ?? "") == PasswordVerificationResult.Failed)
            return Results.BadRequest();

        user.Pword = passwords.Hash(user, request.NewPassword);
        await context.SaveChangesAsync();
        return Results.Ok();
    }

    private static async Task<TimeEntryUser?> FindCaller(TimeEntryContext context, ClaimsPrincipal caller)
    {
        int? id = caller.UserId();
        return id == null ? null : await context.TimeEntryUser.FirstOrDefaultAsync(u => u.TimeEntryUserId == id && u.IsActive);
    }
}
