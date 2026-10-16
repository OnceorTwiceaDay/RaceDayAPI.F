using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    // ---------- Registration ----------

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidUser_Returns201(string role)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelpers.NewUser(role));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(role, body!.Role);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var user = TestHelpers.NewUser("Participant");
        await client.PostAsJsonAsync("/api/auth/register", user);

        var second = await client.PostAsJsonAsync("/api/auth/register", user);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", TestHelpers.NewUser("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var user = TestHelpers.NewUser("Participant");
        user.Password = "";

        var response = await client.PostAsJsonAsync("/api/auth/register", user);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = _factory.CreateClient();
        var user = TestHelpers.NewUser("Participant");
        await client.PostAsJsonAsync("/api/auth/register", user);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var saved = db.Users.Single(u => u.Email == user.Email);

        Assert.NotEqual(user.Password, saved.PasswordHash);
        Assert.True(saved.PasswordHash.Length > 40);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_CorrectCredentials_Returns200()
    {
        var client = _factory.CreateClient();
        var user = TestHelpers.NewUser("Organiser");
        await client.PostAsJsonAsync("/api/auth/register", user);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("Organiser", body!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var user = TestHelpers.NewUser("Participant");
        await client.PostAsJsonAsync("/api/auth/register", user);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = TestHelpers.UniqueEmail(), Password = TestHelpers.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Logout ----------

    [Fact]
    public async Task Logout_WhenLoggedIn_Returns200_AndSessionEnds()
    {
        var client = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenNotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Profile ----------

    [Fact]
    public async Task Profile_NotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_LoggedIn_ReturnsOwnDetails()
    {
        var client = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.Equal("Participant", profile!.Role);
    }

    [Fact]
    public async Task Profile_Update_ChangesOwnName()
    {
        var client = await TestHelpers.LoginAsNewUser(_factory, "Organiser");

        var response = await client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest { FullName = "Renamed Person", PhoneNumber = "0831112222" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<ProfileDto>();
        Assert.Equal("Renamed Person", profile!.FullName);
    }
}