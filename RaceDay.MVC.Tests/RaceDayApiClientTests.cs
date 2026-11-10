using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Contracts;
using RaceDay.MVC.Services;
using Xunit;

namespace RaceDay.MVC.Tests;

public class RaceDayApiClientTests
{
    [Fact]
    public async Task Login_StoresApiSessionCookieInCurrentVisitorSession()
    {
        var (api, session) = Client(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new AuthResponse
                {
                    UserId = 42, FullName = "Runner One", Email = "runner@example.com", Role = "Participant"
                })
            };
            response.Headers.TryAddWithoutValidation("Set-Cookie",
                "RaceDay.Session=abc123; path=/; httponly; samesite=lax");
            return response;
        });

        var result = await api.Post<AuthResponse>("api/auth/login",
            new LoginRequest { Email = "runner@example.com", Password = "not-a-real-password" });

        Assert.True(result.Success);
        Assert.Equal("Participant", result.Data!.Role);
        Assert.Equal("RaceDay.Session=abc123", session.GetString("ApiCookie"));
    }

    [Fact]
    public async Task OtherRequests_ForwardOnlyTheCurrentVisitorsApiCookie()
    {
        string? forwarded = null;
        var (api, session) = Client(req =>
        {
            forwarded = req.Headers.TryGetValues("Cookie", out var values)
                ? string.Join("; ", values) : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<EventDto>())
            };
        });
        session.SetString("ApiCookie", "RaceDay.Session=visitor-one");
        var reply = await api.Get<List<EventDto>>("api/events/mine");

        Assert.True(reply.Success);
        Assert.Equal("RaceDay.Session=visitor-one", forwarded);
    }

    [Fact]
    public async Task PlainText405_DoesNotThrowJsonException()
    {
        var (api, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
        {
            Content = new StringContent("Method Not Allowed")
        });

        var result = await api.Post<AuthResponse>("api/auth/login",
            new LoginRequest { Email = "a@b.com", Password = "x" });

        Assert.False(result.Success);
        Assert.Contains("405", result.Error);
        Assert.Contains("Method Not Allowed", result.Error);
    }

    [Fact]
    public async Task RedirectToHttps_ReportsDestinationInsteadOfParsingJson()
    {
        var (api, _) = Client(_ =>
        {
            var resp = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            resp.Headers.Location = new Uri("https://localhost:59670/api/auth/login");
            return resp;
        });
        var reply = await api.Post<AuthResponse>("api/auth/login", new LoginRequest());

        Assert.False(reply.Success);
        Assert.Contains("redirected", reply.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://localhost:59670", reply.Error);
    }

    [Fact]
    public async Task InvalidCredentials_ShowApiMessage()
    {
        var (api, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(new { message = "Invalid email or password." })
        });

        var result = await api.Post<AuthResponse>("api/auth/login", new LoginRequest());

        Assert.False(result.Success);
        Assert.Contains("Invalid email or password", result.Error);
    }

    [Fact]
    public async Task LoginWithoutApiCookie_ReturnsActionableError()
    {
        var (api, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthResponse { UserId = 1, Role = "Organiser" })
        });

        var result = await api.Post<AuthResponse>("api/auth/login", new LoginRequest());

        Assert.False(result.Success);
        Assert.Contains("RaceDay.Session", result.Error);
    }

    [Fact]
    public async Task UnauthorizedProtectedRequest_ClearsExpiredMvcSession()
    {
        var (api, session) = Client(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = JsonContent.Create(new { message = "No active session" })
        });
        session.SetString("ApiCookie", "RaceDay.Session=expired");
        session.SetString("Role", "Organiser");

        await api.Get<ProfileDto>("api/users/me");

        Assert.Null(session.GetString("ApiCookie"));
        Assert.Null(session.GetString("Role"));
    }

    [Fact]
    public async Task SuccessfulResponseWithText_IncludesRouteAndContentType()
    {
        var (api, _) = Client(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("Method Not Allowed")
        });

        var result = await api.Get<List<EventDto>>("api/events");

        Assert.False(result.Success);
        Assert.Contains("not JSON", result.Error);
        Assert.Contains("api/events", result.Error);
    }

    private static (RaceDayApiClient Api, InMemorySession Session) Client(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var session = new InMemorySession();
        var context = new DefaultHttpContext();
        context.Features.Set<ISessionFeature>(new TestSessionFeature { Session = session });
        var accessor = new HttpContextAccessor { HttpContext = context };
        var client = new HttpClient(new StubHandler(responder))
        {
            BaseAddress = new Uri("https://localhost:59670/")
        };
        return (new RaceDayApiClient(client, accessor, NullLogger<RaceDayApiClient>.Instance), session);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private sealed class InMemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();
        public string Id => "test-session";
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _values.Keys;
        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
    }
}
