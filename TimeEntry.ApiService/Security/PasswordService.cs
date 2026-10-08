using Microsoft.AspNetCore.Identity;

namespace TimeEntry.ApiService.Security;

/// <summary> Hashes and checks passwords and security answers. Only the salted hash is stored. </summary>
public class PasswordService
{
    public const int MinimumPasswordLength = 8;

    private readonly PasswordHasher<TimeEntryUser> _hasher = new();

    public string Hash(TimeEntryUser user, string secret) => _hasher.HashPassword(user, secret);

    /// <summary> An answer is compared without regard to case or surrounding blanks. </summary>
    public string HashAnswer(TimeEntryUser user, string answer) => Hash(user, NormalizeAnswer(answer));

    public static string NormalizeAnswer(string answer) => answer.Trim().ToLowerInvariant();

    public PasswordVerificationResult Verify(TimeEntryUser user, string storedHash, string attempt)
    {
        try
        {
            return _hasher.VerifyHashedPassword(user, storedHash, attempt);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed; // the column does not hold a hash
        }
    }

    public static bool IsAcceptable(string? password) =>
        password is { Length: >= MinimumPasswordLength } && password.Any(char.IsLetter) && password.Any(char.IsDigit);
}
