using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Contracts;
using RaceDay.MVC.Services;
using Xunit;

namespace RaceDay.MVC.Tests;

public sealed class MvcLoginFlowTests
{
    [Fact]
    public async Task SigningInThroughMvc_OpensOrganiserDashboardAndForwardsApiCookie()
    {
        await using var factory = new TestFactory();
        using var browser = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        // Fetch a real antiforgery token and its matching browser cookie.
        var formPage = await browser.GetAsync("/Account/Login");
        Assert.Equal(HttpStatusCode.OK, formPage.StatusCode);
        var markup = await formPage.Content.ReadAsStringAsync();
        var match = Regex.Match(markup,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.True(match.Success, "MVC login form did not render an antiforgery token.");
        var token = match.Groups[1].Value;

        var reply = await browser.PostAsync("/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = "organiser@test.example",
                ["Password"] = "TestPassword123!"
            }));

        Assert.Equal(HttpStatusCode.Redirect, reply.StatusCode);
        Assert.Contains("/Organiser", reply.Headers.Location?.ToString());

        var dashboard = await browser.GetAsync("/Organiser");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("Your events", await dashboard.Content.ReadAsStringAsync());
    }

    private sealed class TestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<RaceDayApiClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeRaceDayApi());
            });
        }
    }

    private sealed class FakeRaceDayApi : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath;
            if (path == "/api/auth/login" && request.Method == HttpMethod.Post)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new AuthResponse
                    {
                        UserId = 7, FullName = "Test Organiser",
                        Email = "organiser@test.example", Role = "Organiser"
                    })
                };
                response.Headers.TryAddWithoutValidation("Set-Cookie",
                    "RaceDay.Session=test-session; path=/; httponly");
                return Task.FromResult(response);
            }

            if (path == "/api/events/mine" && request.Method == HttpMethod.Get)
            {
                var authenticated = request.Headers.TryGetValues("Cookie", out var cookies)
                    && cookies.Any(c => c.Contains("RaceDay.Session=test-session"));
                return Task.FromResult(authenticated
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = JsonContent.Create(new List<EventDto>())
                    }
                    : new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    {
                        Content = JsonContent.Create(new { message = "No session" })
                    });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = JsonContent.Create(new { message = "Unknown API endpoint" })
            });
        }
    }
}
