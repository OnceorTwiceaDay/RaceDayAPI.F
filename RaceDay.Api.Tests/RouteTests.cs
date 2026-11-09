using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class RouteTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public RouteTests(RaceDayFactory factory) => _factory = factory;

    private static CreateRouteRequest Sample(string name = "10 km loop") => new()
    {
        RouteName = name,
        DistanceKm = 10m,
        StartPoint = "Sports field",
        EndPoint = "Finish arch",
        ElevationGainM = 55
    };

    [Fact]
    public async Task OwnerCanCreateEditAndDeleteRoute()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);

        var post = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/routes", Sample());
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = await post.Content.ReadFromJsonAsync<RouteDto>();
        Assert.NotNull(created);

        var update = await organiser.PutAsJsonAsync($"/api/routes/{created.RouteId}",
            Sample("Updated race loop"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var changed = await update.Content.ReadFromJsonAsync<RouteDto>();
        Assert.Equal("Updated race loop", changed!.RouteName);

        var list = await organiser.GetFromJsonAsync<List<RouteDto>>($"/api/events/{ev.EventId}/routes");
        Assert.Contains(list!, route => route.RouteId == created.RouteId);

        var delete = await organiser.DeleteAsync($"/api/routes/{created.RouteId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await organiser.GetFromJsonAsync<List<RouteDto>>($"/api/events/{ev.EventId}/routes");
        Assert.DoesNotContain(after!, route => route.RouteId == created.RouteId);
    }

    [Fact]
    public async Task OtherOrganiserCannotChangeOrDeleteRoute()
    {
        var owner = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var other = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(owner);
        var result = await owner.PostAsJsonAsync($"/api/events/{ev.EventId}/routes", Sample());
        var route = (await result.Content.ReadFromJsonAsync<RouteDto>())!;

        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PutAsJsonAsync($"/api/routes/{route.RouteId}", Sample("Hijack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.DeleteAsync($"/api/routes/{route.RouteId}")).StatusCode);
    }

    [Fact]
    public async Task ParticipantCannotEditRoute()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        var ev = await TestHelpers.CreateEvent(organiser);
        var create = await organiser.PostAsJsonAsync($"/api/events/{ev.EventId}/routes", Sample());
        var route = (await create.Content.ReadFromJsonAsync<RouteDto>())!;
        var edit = await participant.PutAsJsonAsync($"/api/routes/{route.RouteId}", Sample("Wrong role"));
        Assert.Equal(HttpStatusCode.Forbidden, edit.StatusCode);
    }
}
