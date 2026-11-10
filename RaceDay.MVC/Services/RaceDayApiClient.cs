using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Net.Http.Headers;

namespace RaceDay.MVC.Services;

/// <summary>
/// Server-to-server API client. API cookies are stored in the CURRENT VISITOR'S
/// MVC session, never in a shared HttpClient CookieContainer.
/// </summary>
public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApiClient> _logger;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor accessor, ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    private ISession Session => _accessor.HttpContext?.Session
        ?? throw new InvalidOperationException("An MVC request session is required.");

    public Task<ApiReply<T>> Get<T>(string path) => Send<T>(HttpMethod.Get, path);
    public Task<ApiReply<T>> Post<T>(string path, object? data) =>
        Send<T>(HttpMethod.Post, path, data == null ? null : JsonContent.Create(data));
    public Task<ApiReply<T>> Put<T>(string path, object data) =>
        Send<T>(HttpMethod.Put, path, JsonContent.Create(data));
    public Task<ApiReply<T>> Delete<T>(string path) => Send<T>(HttpMethod.Delete, path);

    public async Task<ApiReply<T>> Upload<T>(string path, IFormFile file)
    {
        using var multipart = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var part = new StreamContent(stream);
        part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        multipart.Add(part, "file", Path.GetFileName(file.FileName));
        return await Send<T>(HttpMethod.Post, path, multipart);
    }

    public async Task<(byte[]? Data, string? ContentType)> GetMedia(string path)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await _http.SendAsync(req);
            if (!response.IsSuccessStatusCode) return (null, null);
            return (await response.Content.ReadAsByteArrayAsync(),
                response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "API media request failed for {Path}", path);
            return (null, null);
        }
    }

    private async Task<ApiReply<T>> Send<T>(HttpMethod method, string path, HttpContent? content = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var cookie = Session.GetString("ApiCookie");
            if (!string.IsNullOrWhiteSpace(cookie))
                request.Headers.TryAddWithoutValidation(HeaderNames.Cookie, cookie);

            using var response = await _http.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            var status = response.StatusCode;
            _logger.LogInformation("RaceDay API {Method} {Path}: HTTP {Status}",
                method.Method, path, (int)status);

            if ((int)status is >= 300 and < 400)
            {
                var destination = response.Headers.Location?.ToString() ?? "(no location)";
                return Failure<T>(status, $"API redirected {method} {path} to {destination}. " +
                    "Check RaceDayApi:BaseUrl and use the API's actual HTTPS listening address.");
            }

            if (status == HttpStatusCode.Unauthorized && path != "api/auth/login")
                Session.Clear();

            if (!response.IsSuccessStatusCode)
                return Failure<T>(status, FormatError(status, method, path, raw));

            if (status == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(raw))
            {
                if (path == "api/auth/login")
                    return Failure<T>(status, "The API returned an empty login response.");
                return new ApiReply<T>(true, default, null, status);
            }

            T? data;
            try
            {
                data = JsonSerializer.Deserialize<T>(raw, Json);
            }
            catch (JsonException)
            {
                return Failure<T>(status, $"API returned HTTP {(int)status} for {method} {path}, " +
                    $"but the response was not JSON. Content type: " +
                    $"{response.Content.Headers.ContentType?.MediaType ?? "(missing)"}. " +
                    $"Response: {Preview(raw)}. Confirm the API URL and route.");
            }

            if (path == "api/auth/login")
            {
                if (data == null)
                    return Failure<T>(status, "Login returned an empty JSON object.");

                var sessionCookie = response.Headers.TryGetValues(HeaderNames.SetCookie, out var cookies)
                    ? cookies.Select(c => c.Split(';', 2)[0])
                        .FirstOrDefault(c => c.StartsWith("RaceDay.Session=", StringComparison.Ordinal))
                    : null;

                if (string.IsNullOrWhiteSpace(sessionCookie))
                    return Failure<T>(status, "The API accepted the login but did not set RaceDay.Session. " +
                        "Check that app.UseSession() is enabled in RaceDay.Api.");

                Session.SetString("ApiCookie", sessionCookie);
            }
            return new ApiReply<T>(true, data, null, status);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "RaceDay API is not reachable at {Url}", _http.BaseAddress);
            return Failure<T>(HttpStatusCode.ServiceUnavailable,
                $"Cannot connect to the RaceDay API at {_http.BaseAddress}. " +
                "Check that the API is running, its HTTPS certificate is trusted, and the port matches.");
        }
        catch (TaskCanceledException)
        {
            return new ApiReply<T>(false, default, "The API request timed out.", HttpStatusCode.GatewayTimeout);
        }
        catch (JsonException)
        {
            return new ApiReply<T>(false, default, "The API returned an unexpected response.", HttpStatusCode.BadGateway);
        }
    }
}

public sealed record ApiReply<T>(bool Success, T? Data, string? Error, HttpStatusCode StatusCode);
