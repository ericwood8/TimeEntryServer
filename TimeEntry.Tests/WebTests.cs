namespace TimeEntry.Tests;

[TestClass]
public class WebTests
{
    [TestMethod]
    public async Task ApiHealthReturnsOkStatusCode()
    {
        // Arrange
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.TimeEntry_AppHost>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync();
        var resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();

        // Act
        // "timeentryapi" is the API project in TimeEntry.AppHost (needs the .NET Aspire host to run; SQL Server is not needed for /health)
        var httpClient = app.CreateHttpClient("timeentryapi");
        await resourceNotificationService.WaitForResourceAsync("timeentryapi", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(60));
        var response = await httpClient.GetAsync("/health");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
