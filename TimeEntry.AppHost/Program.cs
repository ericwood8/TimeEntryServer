var builder = DistributedApplication.CreateBuilder(args);

//var connectionString = builder.AddConnectionString("TimeEntryDb");
//var sql = builder.AddSqlServer("TimeEntrySql");
//var sqldb = sql.AddDatabase("TimeEntryDatabase");

var timeentryApi = builder.AddProject<Projects.TimeEntry_ApiService>("timeentryapi")
    .WithExternalHttpEndpoints();
    //.WithReference(sqldb)
    //.WithReference(connectionString);

builder.AddNpmApp("angular", "../../TimeEntryUI")
    .WithReference(timeentryApi)
    .WaitFor(timeentryApi)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();


builder.Build().Run();
