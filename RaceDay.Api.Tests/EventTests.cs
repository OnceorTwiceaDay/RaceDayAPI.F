using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory) => _factory = factory;

    private static CreateEventRequest ValidEvent() => new()
    {
        EventName = "Test Run",
        Description = "A test event",
        EventDate = new DateOnly(2030, 1, 1),
        Location = "Cape Town",
        DistanceKm = 21.1m,
        EventType = "Running"
    };

    [Fact]
    public async Task GetEvents_NotLoggedIn_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Test Run", created!.EventName);
        Assert.Equal(21.1m, created.DistanceKm);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_NotLoggedIn_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", ValidEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var request = ValidEvent();
        request.EventType = "Swimming";

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_ZeroDistance_Returns400()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var request = ValidEvent();
        request.DistanceKm = 0m;

        var response = await organiser.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_OwnEvent_Returns200()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var created = await TestHelpers.CreateEvent(organiser);
        var update = new UpdateEventRequest
        {
            EventName = "Renamed Run",
            Description = "Updated",
            EventDate = new DateOnly(2030, 2, 2),
            Location = "Durban",
            DistanceKm = 5m,
            EventType = "Walking"
        };

        var response = await organiser.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.Equal("Renamed Run", updated!.EventName);
    }

    [Fact]
    public async Task UpdateEvent_OtherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var created = await TestHelpers.CreateEvent(owner);
        var update = new UpdateEventRequest
        {
            EventName = "Hijacked",
            Description = "x",
            EventDate = new DateOnly(2030, 2, 2),
            Location = "Durban",
            DistanceKm = 5m,
            EventType = "Walking"
        };

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{created.EventId}", update);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_OwnEvent_Returns204_ThenEventIsGone()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var created = await TestHelpers.CreateEvent(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{created.EventId}");
        var afterDelete = await organiser.GetAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_OtherOrganisersEvent_Returns403()
    {
        var owner = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var otherOrganiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var created = await TestHelpers.CreateEvent(owner);

        var response = await otherOrganiser.DeleteAsync($"/api/events/{created.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}