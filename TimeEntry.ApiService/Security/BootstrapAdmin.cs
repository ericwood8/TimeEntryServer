using System.Security.Cryptography;
using TimeEntry.Common.Enums;

namespace TimeEntry.ApiService.Security;

/// <summary>
///    A table with no users has no one who can sign in to add the first one. When it is empty and
///    Bootstrap:AdminUserName and Bootstrap:AdminPassword are set (user-secrets or environment variables, never a committed file),
///    one Admin is added. Once any user exists this does nothing, so the settings can then be removed.
/// </summary>
public static class BootstrapAdmin
{
    public static async Task CreateIfNoUsersAsync(WebApplication app)
    {
        string? userName = app.Configuration["Bootstrap:AdminUserName"];
        string? password = app.Configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            return;

        using IServiceScope scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TokenService>>();
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<TimeEntryContext>();
            if (await context.TimeEntryUser.AnyAsync())
                return;

            if (!PasswordService.IsAcceptable(password))
            {
                logger.LogError("Bootstrap:AdminPassword is too weak (at least {Minimum} characters with a letter and a digit); no admin was created.", PasswordService.MinimumPasswordLength);
                return;
            }

            var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
            TimeEntryUser admin = new()
            {
                TimeEntryUserId = 0,
                Name = userName.Trim(),
                SY_RoleId = (int)SY_Role.Admin,
                Pword = "",
                Hint = "",
                Answer = "",
                IsTwoFactorAuth = false,
                IsActive = true,
            };
            admin.Pword = passwords.Hash(admin, password);
            admin.Answer = passwords.HashAnswer(admin, Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))); // nobody knows it until an Admin sets one
            context.TimeEntryUser.Add(admin);
            await context.SaveChangesAsync();
            logger.LogWarning("The TimeEntryUser table was empty: added Admin user '{UserName}'. Remove the Bootstrap settings now.", admin.Name);
        }
        catch (Exception ex)
        {
            // most likely the Pword column is still nvarchar(50): run TimeEntryDB/WidenPasswordColumns.sql
            logger.LogError(ex, "Could not add the first Admin user.");
        }
    }
}
