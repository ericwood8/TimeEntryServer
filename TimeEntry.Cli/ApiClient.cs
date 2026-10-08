using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TimeEntry.Cli;

/// <summary> What the API answered: the status, the JSON body when there was one, and a message when it was an error. </summary>
public sealed record ApiResult(HttpStatusCode Status, JsonNode? Body, string? Problem)
{
    public bool IsSuccess => (int)Status is >= 200 and < 300;

    /// <summary> Wraps one answer. A problem document gives its detail, or each field error of a validation problem. </summary>
    public static ApiResult From(HttpStatusCode status, string text)
    {
        JsonNode? body = null;
        if (!string.IsNullOrWhiteSpace(text))
        {
            try { body = JsonNode.Parse(text); }
            catch (JsonException) { /* not JSON: keep the text as the message below */ }
        }

        if ((int)status is >= 200 and < 300)
            return new(status, body, null);
        return new(status, body, DescribeProblem(body) ?? (string.IsNullOrWhiteSpace(text) ? status.ToString() : text.Trim()));
    }

    public static string? DescribeProblem(JsonNode? body)
    {
        if (body is not JsonObject problem)
            return null;

        if (problem["errors"] is JsonObject errors)
        {
            var lines = errors.SelectMany(e => (e.Value as JsonArray ?? []).Select(m => $"{e.Key}: {m}")).ToList();
            if (lines.Count > 0)
                return string.Join(Environment.NewLine, lines);
        }
        string? detail = problem["detail"]?.GetValue<string>();
        string? title = problem["title"]?.GetValue<string>();
        return !string.IsNullOrWhiteSpace(detail) ? detail : title;
    }
}

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private string? _token;

    public ApiClient(string baseUrl, HttpMessageHandler? handler = null)
    {
        _http = handler == null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public bool IsSignedIn => _token != null;
    public string? Token => _token;

    /// <summary> Uses a token from an earlier sign-in instead of signing in. </summary>
    public void UseToken(string token) => _token = token;

    /// <summary> Signs in; the token is kept for this client's later calls only. </summary>
    public async Task<ApiResult> LoginAsync(string userName, string password)
    {
        var result = await SendAsync(HttpMethod.Post, "auth/login", new JsonObject { ["userName"] = userName, ["password"] = password });
        if (result.IsSuccess)
            _token = result.Body?["token"]?.GetValue<string>();
        return result;
    }

    public Task<ApiResult> GetAsync(string path) => SendAsync(HttpMethod.Get, path, null);
    public Task<ApiResult> PostAsync(string path, JsonNode body) => SendAsync(HttpMethod.Post, path, body);
    public Task<ApiResult> PutAsync(string path, JsonNode body) => SendAsync(HttpMethod.Put, path, body);
    public Task<ApiResult> DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path, null);

    private async Task<ApiResult> SendAsync(HttpMethod method, string path, JsonNode? body)
    {
        using var request = new HttpRequestMessage(method, path.TrimStart('/'));
        if (_token != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (body != null)
            request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request);
        return ApiResult.From(response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public void Dispose() => _http.Dispose();
}
