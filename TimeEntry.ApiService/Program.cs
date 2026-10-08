using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using TimeEntry.ApiService.Apis;
using TimeEntry.ApiService.Extensions;
using TimeEntry.ApiService.Security;
using TimeEntry.Common.Context;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//builder.Services.AddHealthChecks().AddDbContextCheck<TimeEntryContext>();
builder.AddSqlServerDbContext<TimeEntryContext>("TimeEntryDb");

// ----- who may call: a signed-in user, proven by a token from /auth/login -----
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<TokenService, IOptions<JwtOptions>>((bearer, tokens, jwt) =>
    {
        bearer.MapInboundClaims = false; // keep the claim names the token was written with ("sub", "role", ...)
        bearer.TokenValidationParameters = new()
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = tokens.SigningKey,
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorizationBuilder()
    // an endpoint that does not say otherwise (AllowAnonymous) needs a signed-in user
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// the login endpoint is the one place a password can be guessed: slow that down per client address
int loginPermitsPerMinute = builder.Configuration.GetValue("RateLimit:LoginPermitsPerMinute", 5);
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddPolicy(AuthApi.LoginRateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = loginPermitsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "The token returned by POST /auth/login",
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
    });
});

// The UI reaches the API through its own server (/api is proxied), so the browser makes no cross-origin call and CORS stays off.
// Name an origin here only when something else must call the API from a browser.
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader()));
}

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
if (allowedOrigins.Length > 0)
{
    app.UseCors();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapApis();

await BootstrapAdmin.CreateIfNoUsersAsync(app);

app.Run();
