using System.Net;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventDeleteWithEnrolmentsTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EventDeleteWithEnrolmentsTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task EventWithEnrolments_Returns409InsteadOfServerError()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        var ev = await TestHelpers.CreateEvent(organiser);
        var cat = await TestHelpers.CreateCategory(organiser, ev.EventId);
        await TestHelpers.Enrol(participant, ev.EventId, cat.CategoryId);

        var response = await organiser.DeleteAsync($"/api/events/{ev.EventId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
