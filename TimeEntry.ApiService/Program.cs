using TimeEntry.ApiService.Extensions;
using TimeEntry.Common.Data;


var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
//builder.Services.AddHealthChecks().AddDbContextCheck<TimeEntryContext>();
builder.AddSqlServerDbContext<TimeEntryContext>("TimeEntryDb");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "false");

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(static builder => 
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.MapApis();

app.Run();
