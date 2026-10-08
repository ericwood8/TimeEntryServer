using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using TimeEntry.Cli;

namespace TimeEntry.Tests;

/// <summary> The command-line client: argument parsing, body building, table output, and each command against a stand-in API. No server needed. </summary>
[TestClass]
public class CliTests
{
    private static CliOptions Parse(params string[] args) => ArgumentParser.Parse(args, _ => null);

    // ---------- ArgumentParser ----------

    [TestMethod]
    public void Parse_List_WithNameAndSignIn()
    {
        var o = Parse("-U", "Boss", "-P", "pw", "--url", "http://host:9/", "list", "employees", "--name", "Sm", "--json");
        Assert.AreEqual("list", o.Command);
        Assert.AreEqual("employees", o.Resource!.Name);
        Assert.AreEqual("Sm", o.Name);
        Assert.AreEqual("Boss", o.UserName);
        Assert.AreEqual("pw", o.Password);
        Assert.AreEqual("http://host:9", o.Url); // trailing slash dropped
        Assert.IsTrue(o.Json);
    }

    [TestMethod]
    public void Parse_UsesEnvironmentThenFlagWins()
    {
        string? Env(string key) => key switch { "TIMEENTRY_URL" => "http://env:1", "TIMEENTRY_USER" => "EnvUser", "TIMEENTRY_PASSWORD" => "EnvPw", _ => null };
        var fromEnv = ArgumentParser.Parse(["whoami"], Env);
        Assert.AreEqual("http://env:1", fromEnv.Url);
        Assert.AreEqual("EnvUser", fromEnv.UserName);
        Assert.AreEqual("EnvPw", fromEnv.Password);

        var flagWins = ArgumentParser.Parse(["whoami", "-U", "Flag"], Env);
        Assert.AreEqual("Flag", flagWins.UserName);
    }

    [TestMethod]
    public void Parse_Token_FromFlagOrEnvironment_AndLoginNeedsNothingElse()
    {
        Assert.AreEqual("abc", Parse("--token", "abc", "whoami").Token);
        Assert.AreEqual("fromenv", ArgumentParser.Parse(["whoami"], k => k == "TIMEENTRY_TOKEN" ? "fromenv" : null).Token);
        Assert.IsNull(Parse("whoami").Token);
        Assert.AreEqual("login", Parse("-U", "Boss", "login").Command);
    }

    [TestMethod]
    public void Table_ColumnWithANestedValueInAnyRow_IsLeftOut()
    {
        var rows = JsonNode.Parse("""[{"id":1,"manager":null,"name":"A"},{"id":2,"manager":{"id":1},"name":"B"}]""");
        using var writer = new StringWriter();
        TableWriter.Write(rows, null, writer);
        Assert.IsFalse(writer.ToString().Contains("manager"));
        StringAssert.Contains(writer.ToString(), "name");
    }

    [TestMethod]
    public void Parse_DefaultsToTheLocalApi()
    {
        Assert.AreEqual(CliOptions.DefaultUrl, Parse("whoami").Url);
    }

    [TestMethod]
    public void Parse_GetUpdateDelete_TakeAnId()
    {
        Assert.AreEqual(7, Parse("get", "departments", "7").Id);
        var update = Parse("update", "employees", "4", "--set", "availableLeaveHours=40");
        Assert.AreEqual(4, update.Id);
        Assert.AreEqual("availableLeaveHours", update.Sets[0].Key);
        Assert.IsTrue(Parse("delete", "holidays", "3", "--yes").Yes);
    }

    [TestMethod]
    public void Parse_Decide_TakesRequestIdAndDecision()
    {
        var o = Parse("decide", "12", "APPROVE");
        Assert.AreEqual(12, o.Id);
        Assert.AreEqual("approve", o.Decision);
        Assert.AreEqual("requests", o.Resource!.Name);
    }

    [TestMethod]
    [DataRow(new string[] { }, "command is required")]
    [DataRow(new[] { "fly" }, "Unknown command")]
    [DataRow(new[] { "list" }, "resource is required")]
    [DataRow(new[] { "list", "pets" }, "Unknown resource")]
    [DataRow(new[] { "get", "departments" }, "needs an id")]
    [DataRow(new[] { "get", "departments", "abc" }, "positive whole number")]
    [DataRow(new[] { "get", "departments", "0" }, "positive whole number")]
    [DataRow(new[] { "get", "departments", "-5" }, "positive whole number")]
    [DataRow(new[] { "get", "departments", "1", "2" }, "Unexpected argument")]
    [DataRow(new[] { "list", "timesheets", "--name", "x" }, "cannot be searched by name")]
    [DataRow(new[] { "list", "departments", "--set", "a=b" }, "for add and update")]
    [DataRow(new[] { "add", "projects" }, "at least one --set")]
    [DataRow(new[] { "add", "projects", "--set", "noequals" }, "key=value")]
    [DataRow(new[] { "add", "projects", "--set", "=x" }, "key=value")]
    [DataRow(new[] { "update", "projects", "1", "--name", "x", "--set", "a=1" }, "only for list")]
    [DataRow(new[] { "decide", "5" }, "needs a decision")]
    [DataRow(new[] { "decide", "5", "maybe" }, "Unknown decision")]
    [DataRow(new[] { "list", "departments", "--bogus" }, "Unrecognized argument")]
    [DataRow(new[] { "list", "departments", "--name" }, "Missing value")]
    [DataRow(new[] { "whoami", "extra" }, "Unexpected argument")]
    public void Parse_RejectsMistakes(string[] args, string expectedMessagePart)
    {
        var ex = Assert.ThrowsExactly<ArgumentParseException>(() => Parse(args));
        StringAssert.Contains(ex.Message, expectedMessagePart);
    }

    [TestMethod]
    public void EveryResource_HasARouteAndAnIdProperty_AndNamesAreUnique()
    {
        foreach (var r in Resources.All)
        {
            Assert.IsTrue(r.Route.StartsWith('/'), r.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(r.IdProperty), r.Name);
        }
        Assert.AreEqual(Resources.All.Count, Resources.All.Select(r => r.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    // ---------- BodyBuilder ----------

    [TestMethod]
    public void ReadValue_ReadsBooleansNumbersNullAndText()
    {
        Assert.AreEqual(true, BodyBuilder.ReadValue("true")!.GetValue<bool>());
        Assert.AreEqual(40L, BodyBuilder.ReadValue("40")!.GetValue<long>());
        Assert.AreEqual(2.5m, BodyBuilder.ReadValue("2.5")!.GetValue<decimal>());
        Assert.IsNull(BodyBuilder.ReadValue("null"));
        Assert.AreEqual("Bridge Repair", BodyBuilder.ReadValue("Bridge Repair")!.GetValue<string>());
        Assert.AreEqual("123", BodyBuilder.ReadValue("\"123\"")!.GetValue<string>()); // quoted: stays text
    }

    [TestMethod]
    public void ForAdd_AddsDefaults_ButTheCallersValuesWin()
    {
        var departments = Resources.Find("departments")!;
        var body = BodyBuilder.ForAdd(departments, new JsonObject { ["name"] = "Sales", ["IsActive"] = false });
        Assert.AreEqual(0, body["departmentId"]!.GetValue<int>());
        Assert.AreEqual("Sales", body["name"]!.GetValue<string>());
        Assert.IsFalse(body["isActive"]!.GetValue<bool>()); // the caller's value replaces the default
        Assert.IsNull(body["IsActive"]);                    // and the property is not added twice
        Assert.IsFalse(body["isDefault"]!.GetValue<bool>());
    }

    [TestMethod]
    public void ForUpdate_ChangesOnlyNamedProperties_AndNeverTheId()
    {
        var employees = Resources.Find("employees")!;
        var stored = new JsonObject { ["employeeId"] = 4, ["name"] = "Pat", ["availableLeaveHours"] = 8 };
        var body = BodyBuilder.ForUpdate(employees, stored, new JsonObject { ["AvailableLeaveHours"] = 40, ["employeeId"] = 99 });
        Assert.AreEqual(40, body["availableLeaveHours"]!.GetValue<int>()); // stored spelling kept
        Assert.AreEqual("Pat", body["name"]!.GetValue<string>());
        Assert.AreEqual(4, body["employeeId"]!.GetValue<int>());
    }

    [TestMethod]
    public void ReadChanges_SetOverridesFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"name":"FromFile","isDefault":true}""");
            var options = Parse("add", "projects", "--file", path, "--set", "name=FromSet");
            var changes = BodyBuilder.ReadChanges(options);
            Assert.AreEqual("FromSet", changes["name"]!.GetValue<string>());
            Assert.IsTrue(changes["isDefault"]!.GetValue<bool>());
        }
        finally { File.Delete(path); }
    }

    [TestMethod]
    public void ReadChanges_RejectsMissingAndBrokenFiles()
    {
        Assert.ThrowsExactly<ArgumentParseException>(() => BodyBuilder.ReadChanges(Parse("add", "projects", "--file", "C:\\no\\such\\file.json")));
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "[1,2]");
            Assert.ThrowsExactly<ArgumentParseException>(() => BodyBuilder.ReadChanges(Parse("add", "projects", "--file", path)));
            File.WriteAllText(path, "{oops");
            Assert.ThrowsExactly<ArgumentParseException>(() => BodyBuilder.ReadChanges(Parse("add", "projects", "--file", path)));
        }
        finally { File.Delete(path); }
    }

    // ---------- TableWriter ----------

    [TestMethod]
    public void Table_PutsTheIdFirst_SkipsNestedValues_AndCutsLongText()
    {
        var rows = JsonNode.Parse("""
            [{"name":"Admin","departmentId":3,"teams":[{"x":1}],"isActive":true,"note":null},
             {"name":"A very long department name that goes on and on and on","departmentId":10,"teams":[],"isActive":false,"note":"n"}]
            """);
        using var writer = new StringWriter();
        TableWriter.Write(rows, Resources.Find("departments"), writer);
        string[] lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        StringAssert.StartsWith(lines[0], "departmentId");
        Assert.IsFalse(lines[0].Contains("teams"));
        StringAssert.Contains(lines[2], "Admin");
        StringAssert.Contains(lines[3], "...");
        Assert.AreEqual("2 rows", lines[^1]);
    }

    [TestMethod]
    public void Table_EmptyListAndSingleRow()
    {
        using var empty = new StringWriter();
        TableWriter.Write(new JsonArray(), null, empty);
        Assert.AreEqual("0 rows", empty.ToString().Trim());

        using var one = new StringWriter();
        TableWriter.Write(JsonNode.Parse("""{"name":"Pat","teams":[1,2]}"""), null, one);
        StringAssert.Contains(one.ToString(), "[2 items]");
        StringAssert.Contains(one.ToString(), "Pat");
    }

    // ---------- ApiResult ----------

    [TestMethod]
    public void Problem_UsesDetail_ThenFieldErrors_ThenTitle()
    {
        var detail = ApiResult.From(HttpStatusCode.UnprocessableEntity, """{"title":"Duplicate name","status":422,"detail":"An active row with that name already exists."}""");
        Assert.AreEqual("An active row with that name already exists.", detail.Problem);

        var fields = ApiResult.From(HttpStatusCode.BadRequest, """{"title":"One or more validation errors occurred.","errors":{"MondayHours":["must be between 0 and 24"]}}""");
        StringAssert.Contains(fields.Problem, "MondayHours: must be between 0 and 24");

        var titleOnly = ApiResult.From(HttpStatusCode.NotFound, """{"title":"Not Found","status":404}""");
        Assert.AreEqual("Not Found", titleOnly.Problem);

        var empty = ApiResult.From(HttpStatusCode.Forbidden, "");
        Assert.AreEqual("Forbidden", empty.Problem);
    }

    // ---------- CommandRunner against a stand-in API ----------

    private sealed class FakeApi : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string? Body)> Calls { get; } = [];
        public Func<HttpMethod, string, (HttpStatusCode, string)> Answer { get; set; } = (_, _) => (HttpStatusCode.OK, "[]");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request.Method, request.RequestUri!.AbsolutePath, body));
            var (status, text) = Answer(request.Method, request.RequestUri.AbsolutePath);
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    private static async Task<(int Code, string Out, string Err, FakeApi Api)> Run(Func<HttpMethod, string, (HttpStatusCode, string)> answer, string input, params string[] args)
    {
        var fake = new FakeApi { Answer = answer };
        using var client = new ApiClient("http://api.test", fake);
        using var output = new StringWriter();
        using var error = new StringWriter();
        int code = await CommandRunner.RunAsync(ArgumentParser.Parse(args, _ => null), client, new StringReader(input), output, error);
        return (code, output.ToString(), error.ToString(), fake);
    }

    [TestMethod]
    public async Task List_WithName_CallsTheNameSearch()
    {
        var run = await Run((_, _) => (HttpStatusCode.OK, """[{"departmentId":1,"name":"IT"}]"""), "", "list", "departments", "--name", "I T");
        Assert.AreEqual(0, run.Code);
        Assert.AreEqual("/departments/I%20T", run.Api.Calls[0].Path);
        StringAssert.Contains(run.Out, "IT");
    }

    [TestMethod]
    public async Task Add_PostsDefaultsAndValues()
    {
        var run = await Run((_, _) => (HttpStatusCode.Created, """{"projectId":9,"name":"Bridge Repair","isActive":true,"isDefault":false}"""), "", "add", "projects", "--set", "name=Bridge Repair");
        Assert.AreEqual(0, run.Code);
        var sent = JsonNode.Parse(run.Api.Calls[0].Body!)!;
        Assert.AreEqual(HttpMethod.Post, run.Api.Calls[0].Method);
        Assert.AreEqual(0, sent["projectId"]!.GetValue<int>());
        Assert.AreEqual("Bridge Repair", sent["name"]!.GetValue<string>());
        Assert.IsTrue(sent["isActive"]!.GetValue<bool>());
    }

    [TestMethod]
    public async Task Update_ReadsTheRow_ThenPutsItWithOnlyTheChange()
    {
        var run = await Run((method, _) => (HttpStatusCode.OK, """{"employeeId":4,"name":"Pat","availableLeaveHours":8}"""), "", "update", "employees", "4", "--set", "availableLeaveHours=40");
        Assert.AreEqual(0, run.Code);
        Assert.AreEqual(HttpMethod.Get, run.Api.Calls[0].Method);
        Assert.AreEqual(HttpMethod.Put, run.Api.Calls[1].Method);
        Assert.AreEqual("/employees/4", run.Api.Calls[1].Path);
        var sent = JsonNode.Parse(run.Api.Calls[1].Body!)!;
        Assert.AreEqual(40, sent["availableLeaveHours"]!.GetValue<int>());
        Assert.AreEqual("Pat", sent["name"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task Update_StopsWhenTheRowIsMissing()
    {
        var run = await Run((_, _) => (HttpStatusCode.NotFound, """{"title":"Not found","detail":"There is no row with that id."}"""), "", "update", "employees", "4", "--set", "name=X");
        Assert.AreEqual(ExitCodes.ApiRefused, run.Code);
        Assert.AreEqual(1, run.Api.Calls.Count); // never sent a PUT
        StringAssert.Contains(run.Err, "There is no row with that id.");
    }

    [TestMethod]
    public async Task Delete_AsksFirst_AndOnlyYesDeletes()
    {
        var declined = await Run((_, _) => (HttpStatusCode.OK, ""), "n\n", "delete", "holidays", "3");
        Assert.AreEqual(0, declined.Code);
        Assert.AreEqual(0, declined.Api.Calls.Count);
        StringAssert.Contains(declined.Out, "Nothing deleted");

        var confirmed = await Run((_, _) => (HttpStatusCode.OK, ""), "y\n", "delete", "holidays", "3");
        Assert.AreEqual(HttpMethod.Delete, confirmed.Api.Calls[0].Method);

        var forced = await Run((_, _) => (HttpStatusCode.OK, ""), "", "delete", "holidays", "3", "--yes");
        Assert.AreEqual(1, forced.Api.Calls.Count);
    }

    [TestMethod]
    public async Task Delete_ShowsTheServersInUseMessage()
    {
        var run = await Run((_, _) => (HttpStatusCode.BadRequest, """{"title":"In use","detail":"It cannot be deleted because it is in use."}"""), "", "delete", "departments", "1", "--yes");
        Assert.AreEqual(ExitCodes.ApiRefused, run.Code);
        StringAssert.Contains(run.Err, "in use");
    }

    [TestMethod]
    public async Task Decide_SetsTheStatusAndPuts()
    {
        var run = await Run((_, _) => (HttpStatusCode.OK, """{"requestId":12,"employeeId":2,"sY_RequestStatusTypeId":1,"reason":"x"}"""), "", "decide", "12", "approve");
        Assert.AreEqual(0, run.Code);
        var sent = JsonNode.Parse(run.Api.Calls[1].Body!)!;
        Assert.AreEqual(2, sent["sY_RequestStatusTypeId"]!.GetValue<int>()); // Approved
        Assert.AreEqual("/requests/12", run.Api.Calls[1].Path);
    }

    [TestMethod]
    public async Task Json_PrintsTheServersJson()
    {
        var run = await Run((_, _) => (HttpStatusCode.OK, """[{"departmentId":1,"name":"IT"}]"""), "", "list", "departments", "--json");
        StringAssert.Contains(run.Out, "\"departmentId\": 1");
    }
}
