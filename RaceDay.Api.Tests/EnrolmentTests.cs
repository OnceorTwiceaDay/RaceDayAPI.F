using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, EventDto Race, CategoryDto Category)> SetUpEvent()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var race = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, race.EventId);
        return (organiser, race, category);
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndLinksEventAndCategory()
    {
        var (_, race, category) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{race.EventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();
        Assert.Equal(race.EventId, enrolment!.EventId);
        Assert.Equal(category.CategoryId, enrolment.CategoryId);
    }

    [Fact]
    public async Task Enrol_IsRecorded_AndAppearsInMyEnrolments()
    {
        var (_, race, category) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        await TestHelpers.Enrol(participant, race.EventId, category.CategoryId);

        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Single(mine!);
        Assert.Equal(race.EventId, mine![0].EventId);
        Assert.Equal(category.CategoryId, mine[0].CategoryId);
    }

    [Fact]
    public async Task Enrol_TwiceInSameEvent_Returns409()
    {
        var (_, race, category) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        await TestHelpers.Enrol(participant, race.EventId, category.CategoryId);

        var second = await participant.PostAsJsonAsync($"/api/events/{race.EventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var (organiser, race, category) = await SetUpEvent();

        var response = await organiser.PostAsJsonAsync($"/api/events/{race.EventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = category.CategoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var (_, race, _) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{race.EventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = 999999 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnEvent()
    {
        var (organiser, race, category) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        await TestHelpers.Enrol(participant, race.EventId, category.CategoryId);

        var list = await organiser.GetFromJsonAsync<List<EnrolmentDto>>($"/api/events/{race.EventId}/enrolments");

        Assert.Single(list!);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var (_, race, _) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var response = await participant.GetAsync($"/api/events/{race.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEnrolmentsForSomeoneElsesEvent_Returns403()
    {
        var (_, race, _) = await SetUpEvent();
        var otherOrganiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{race.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_Returns204()
    {
        var (_, race, category) = await SetUpEvent();
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(participant, race.EventId, category.CategoryId);

        var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");
        var remaining = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/users/me/enrolments");

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var (_, race, category) = await SetUpEvent();
        var owner = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        var stranger = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(owner, race.EventId, category.CategoryId);

        var response = await stranger.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}