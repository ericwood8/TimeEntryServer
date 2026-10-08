using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace TimeEntry.ApiService.Security;

/// <summary> Creates the signed token and holds the key that validates it. </summary>
public class TokenService
{
    public const string EmployeeIdClaim = "employeeId";

    private readonly JwtOptions _options;

    public SymmetricSecurityKey SigningKey { get; }

    public TokenService(IOptions<JwtOptions> options, IHostEnvironment environment, ILogger<TokenService> logger)
    {
        _options = options.Value;

        byte[] keyBytes;
        if (!string.IsNullOrWhiteSpace(_options.Key))
        {
            keyBytes = System.Text.Encoding.UTF8.GetBytes(_options.Key);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException("Jwt:Key must be at least 32 characters.");
        }
        else if (environment.IsDevelopment())
        {
            // a key that lives only as long as this process: every restart signs everybody out
            keyBytes = RandomNumberGenerator.GetBytes(64);
            logger.LogWarning("Jwt:Key is not set; using a temporary key. Set it with user-secrets or the Jwt__Key environment variable to keep sign-ins across restarts.");
        }
        else
        {
            throw new InvalidOperationException("Jwt:Key must be set outside of Development.");
        }
        SigningKey = new SymmetricSecurityKey(keyBytes);
    }

    public (string Token, DateTime ExpiresUtc) Create(TimeEntryUser user)
    {
        DateTime expires = DateTime.UtcNow.AddMinutes(_options.ExpiresMinutes);
        var claims = new Dictionary<string, object>
        {
            ["sub"] = user.TimeEntryUserId.ToString(),
            ["name"] = user.Name,
            ["role"] = user.SY_Role.ToString(),
        };
        if (user.EmployeeId != null)
            claims[EmployeeIdClaim] = user.EmployeeId.Value.ToString();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
