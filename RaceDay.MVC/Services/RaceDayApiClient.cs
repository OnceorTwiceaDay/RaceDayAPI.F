
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.MVC.Services;

public sealed class RaceDayApiClient
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private readonly ILogger<RaceDayApiClient> _logger;

    private static readonly JsonSerializerOptions Json =
        new(JsonSerializerDefaults.Web);

    public RaceDayApiClient(
        HttpClient http,
        IHttpContextAccessor accessor,
        ILogger<RaceDayApiClient> logger)
    {
        _http = http;
        _accessor = accessor;
        _logger = logger;
    }

    private ISession Session =>
        _accessor.HttpContext?.Session
        ?? throw new InvalidOperationException(
            "No active MVC session was found.");

    // GET requests
    public Task<ApiReply<T>> Get<T>(string path)
    {
        return Send<T>(HttpMethod.Get, path);
    }

    // POST requests
    public Task<ApiReply<T>> Post<T>(
        string path, object? data)
    {
        return Send<T>(
            HttpMethod.Post,
            path,
            data == null ? null : JsonContent.Create(data));
    }

    // PUT requests
    public Task<ApiReply<T>> Put<T>(
        string path, object data)
    {
        return Send<T>(
            HttpMethod.Put,
            path,
            JsonContent.Create(data));
    }

    // DELETE requests
    public Task<ApiReply<T>> Delete<T>(string path)
    {
        return Send<T>(HttpMethod.Delete, path);
    }

    // Upload images to the API
    public async Task<ApiReply<T>> Upload<T>(
        string path, IFormFile file)
    {
        using var multipart = new MultipartFormDataContent();

        await using var stream = file.OpenReadStream();

        using var part = new StreamContent(stream);

        part.Headers.ContentType =
            new MediaTypeHeaderValue(file.ContentType);

        multipart.Add(
            part,
            "file",
            Path.GetFileName(file.FileName));

        return await Send<T>(
            HttpMethod.Post,
            path,
            multipart);
    }

    // Retrieve images from the API
    public async Task<(byte[]? Data, string? ContentType)>
        GetMedia(string path)
    {
        try
        {
            using var request =
                new HttpRequestMessage(HttpMethod.Get, path);

            using var response =
                await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Media request failed: {Path}, HTTP {Status}",
                    path,
                    (int)response.StatusCode);

                return (null, null);
            }

            var bytes =
                await response.Content.ReadAsByteArrayAsync();

            var contentType =
                response.Content.Headers.ContentType?.MediaType
                ?? "application/octet-stream";

            return (bytes, contentType);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to retrieve media: {Path}",
                path);

            return (null, null);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "Media request timed out: {Path}",
                path);

            return (null, null);
        }
    }

    // Main method for communicating with RaceDay API
    private async Task<ApiReply<T>> Send<T>(
        HttpMethod method,
        string path,
        HttpContent? content = null)
    {
        try
        {
            using var request =
                new HttpRequestMessage(method, path)
                {
                    Content = content
                };

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            // Forward the current user's API session cookie
            var cookie = Session.GetString("ApiCookie");

            if (!string.IsNullOrWhiteSpace(cookie))
            {
                request.Headers.TryAddWithoutValidation(
                    "Cookie",
                    cookie);
            }

            // Send request to the API
            using var response =
                await _http.SendAsync(request);

            var status = response.StatusCode;

            var raw =
                await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "RaceDay API {Method} {Path}: HTTP {Status}",
                method.Method,
                path,
                (int)status);

            // Handle HTTP redirects
            if ((int)status >= 300 && (int)status < 400)
            {
                var location =
                    response.Headers.Location?.ToString()
                    ?? "Unknown destination";

                return Failure<T>(
                    status,
                    $"API redirected to {location}. " +
                    "Check the RaceDay API BaseUrl.");
            }

            // Clear expired sessions
            if (status == HttpStatusCode.Unauthorized &&
                path != "api/auth/login")
            {
                Session.Clear();
            }

            // Handle API errors
            if (!response.IsSuccessStatusCode)
            {
                var error = FormatError(
                    status,
                    method,
                    path,
                    raw);

                return Failure<T>(status, error);
            }

            // Handle empty responses
            if (status == HttpStatusCode.NoContent ||
                string.IsNullOrWhiteSpace(raw))
            {
                if (path == "api/auth/login")
                {
                    return Failure<T>(
                        status,
                        "The API returned an empty login response.");
                }

                return new ApiReply<T>(
                    true,
                    default,
                    null,
                    status);
            }

            // Convert JSON response into the expected model
            T? data;

            try
            {
                data = JsonSerializer.Deserialize<T>(
                    raw,
                    Json);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Invalid JSON from API endpoint {Path}",
                    path);

                return Failure<T>(
                    status,
                    $"API returned HTTP {(int)status} for {path}, " +
                    "but the response was not JSON. " +
                    $"Content type: {response.Content.Headers.ContentType?.MediaType ?? "(missing)"}. " +
                    $"Response: {Preview(raw)}");
            }

            // Handle login and save the user's API session
            if (path == "api/auth/login")
            {
                if (data == null)
                {
                    return Failure<T>(
                        status,
                        "Login succeeded but no user data was returned.");
                }

                string? sessionCookie = null;

                if (response.Headers.TryGetValues(
                    "Set-Cookie",
                    out var cookies))
                {
                    sessionCookie = cookies
                        .Select(c => c.Split(';', 2)[0])
                        .FirstOrDefault(c =>
                            c.StartsWith(
                                "RaceDay.Session=",
                                StringComparison.Ordinal));
                }

                if (string.IsNullOrWhiteSpace(sessionCookie))
                {
                    return Failure<T>(
                        status,
                        "The API accepted the login but did not " +
                        "return a session cookie. Check UseSession().");
                }

                Session.SetString(
                    "ApiCookie",
                    sessionCookie);
            }

            return new ApiReply<T>(
                true,
                data,
                null,
                status);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Cannot connect to RaceDay API at {Url}",
                _http.BaseAddress);

            return Failure<T>(
                HttpStatusCode.ServiceUnavailable,
                "Cannot connect to RaceDay API at " +
                $"{_http.BaseAddress}. " +
                "Check the API address, port, and HTTPS certificate.");
        }
        catch (TaskCanceledException)
        {
            return Failure<T>(
                HttpStatusCode.GatewayTimeout,
                "The API request timed out.");
        }
    }

    // Create a failed API response
    private static ApiReply<T> Failure<T>(
        HttpStatusCode status,
        string message)
    {
        return new ApiReply<T>(
            false,
            default,
            message,
            status);
    }

    // Extract useful messages from API errors
    private static string FormatError(
        HttpStatusCode status,
        HttpMethod method,
        string path,
        string raw)
    {
        var prefix =
            $"API returned HTTP {(int)status} ({status}) " +
            $"for {method} {path}.";

        if (string.IsNullOrWhiteSpace(raw))
            return prefix;

        try
        {
            using var document = JsonDocument.Parse(raw);

            if (document.RootElement.ValueKind ==
                JsonValueKind.Object)
            {
                foreach (var field in new[]
                {
                    "message",
                    "detail",
                    "title"
                })
                {
                    if (document.RootElement.TryGetProperty(
                        field,
                        out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        return $"{prefix} {value.GetString()}";
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Plain-text API errors are also supported
        }

        return $"{prefix} Response: {Preview(raw)}";
    }

    // Limit error response length
    private static string Preview(string body)
    {
        var clean = body
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        return clean.Length <= 160
            ? clean
            : clean[..160] + "...";
    }
}

// Standard response returned by the MVC API client
public sealed record ApiReply<T>(
    bool Success,
    T? Data,
    string? Error,
    HttpStatusCode StatusCode);
