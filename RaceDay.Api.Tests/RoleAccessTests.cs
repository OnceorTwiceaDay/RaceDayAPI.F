using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/users/me")]
    [InlineData("/api/users/me/enrolments")]
    [InlineData("/api/users/me/results")]
    [InlineData("/api/events/mine")]
    [InlineData("/api/events/1/enrolments")]
    public async Task ProtectedEndpoint_WhenNotLoggedIn_Returns401(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/events/mine")]
    [InlineData("/api/events/1/enrolments")]
    public async Task OrganiserOnlyEndpoint_AsParticipant_Returns403(string url)
    {
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/users/me/enrolments")]
    [InlineData("/api/users/me/results")]
    public async Task ParticipantOnlyEndpoint_AsOrganiser_Returns403(string url)
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");

        var response = await organiser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Profile_BothRolesCanView_Returns200(string role)
    {
        var client = await TestHelpers.LoginAsNewUser(_factory, role);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ParticipantEndpoint_AsParticipant_Returns200()
    {
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.GetAsync("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var race = await TestHelpers.CreateEvent(organiser);
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{race.EventId}/categories",
            new CreateCategoryRequest
            {
                CategoryName = "Sneaky Category",
                DistanceKm = 5m,
                MinAge = 10,
                MaxAge = null,
                EntryFee = 50m
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}