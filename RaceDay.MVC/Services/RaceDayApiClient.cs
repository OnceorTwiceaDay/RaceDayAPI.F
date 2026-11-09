using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace RaceDay.MVC.Services;

public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext?.Session
        ?? throw new InvalidOperationException("No active MVC request session.");

    public async Task<ApiReply<T>> Get<T>(string path) => await Send<T>(HttpMethod.Get, path);
    public async Task<ApiReply<T>> Post<T>(string path, object? data) =>
        await Send<T>(HttpMethod.Post, path, data == null ? null : JsonContent.Create(data));
    public async Task<ApiReply<T>> Put<T>(string path, object data) =>
        await Send<T>(HttpMethod.Put, path, JsonContent.Create(data));
    public async Task<ApiReply<T>> Delete<T>(string path) => await Send<T>(HttpMethod.Delete, path);

    public async Task<ApiReply<T>> Upload<T>(string path, IFormFile file)
    {
        using var body = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var part = new StreamContent(stream);
        part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        body.Add(part, "file", file.FileName);
        return await Send<T>(HttpMethod.Post, path, body);
    }

    public async Task<(byte[]? Data, string? ContentType)> GetMedia(string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await _http.SendAsync(req);
        if (!response.IsSuccessStatusCode) return (null, null);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return (bytes, response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
    }

    private async Task<ApiReply<T>> Send<T>(HttpMethod method, string path, HttpContent? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = body };
            var cookie = Session.GetString("ApiCookie");
            if (!string.IsNullOrEmpty(cookie)) request.Headers.TryAddWithoutValidation(HeaderNames.Cookie, cookie);
            using var response = await _http.SendAsync(request);
            if (path == "api/auth/login" && response.IsSuccessStatusCode)
            {
                var raw = response.Headers.TryGetValues(HeaderNames.SetCookie, out var cookies)
                    ? cookies.FirstOrDefault(c => c.StartsWith("RaceDay.Session=", StringComparison.Ordinal))
                    : null;
                var sessionCookie = raw?.Split(';', 2)[0];
                if (!string.IsNullOrEmpty(sessionCookie)) Session.SetString("ApiCookie", sessionCookie);
                else return new ApiReply<T>(false, default, "API login did not return a session cookie.", response.StatusCode);
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized && path != "api/auth/login")
            {
                Session.Clear();
            }
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0) return new ApiReply<T>(true, default, null, response.StatusCode);
                var data = await response.Content.ReadFromJsonAsync<T>(Json);
                return new ApiReply<T>(true, data, null, response.StatusCode);
            }
            var error = "The request could not be completed.";
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("message", out var msg))
                error = msg.GetString() ?? error;
            return new ApiReply<T>(false, default, error, response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return new ApiReply<T>(false, default, "Cannot connect to the RaceDay API. Ensure it is running.", HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException)
        {
            return new ApiReply<T>(false, default, "The API request timed out.", HttpStatusCode.GatewayTimeout);
        }
        catch (JsonException ex)
        {
            return new ApiReply<T>(
                false,
                default,
                $"API response was not valid JSON: {ex.Message}",
                HttpStatusCode.BadGateway);
        }
    }
}

public sealed record ApiReply<T>(bool Success, T? Data, string? Error, HttpStatusCode StatusCode);
