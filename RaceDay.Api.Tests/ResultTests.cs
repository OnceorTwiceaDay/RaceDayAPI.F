using System.Net;
using System.Net.Http.Json;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private async Task<(HttpClient Organiser, HttpClient Participant, EnrolmentDto Enrolment)> SetUpEnrolment()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var race = await TestHelpers.CreateEvent(organiser);
        var category = await TestHelpers.CreateCategory(organiser, race.EventId);
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        var enrolment = await TestHelpers.Enrol(participant, race.EventId, category.CategoryId);
        return (organiser, participant, enrolment);
    }

    private static CreateResultRequest ResultFor(int enrolmentId) => new()
    {
        EnrolmentId = enrolmentId,
        FinishTime = new TimeSpan(0, 52, 30),
        FinishPosition = 3,
        CategoryPosition = 1,
        Status = "Finished"
    };

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_Returns201()
    {
        var (organiser, _, enrolment) = await SetUpEnrolment();

        var response = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        Assert.Equal(3, result!.FinishPosition);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        var (_, participant, enrolment) = await SetUpEnrolment();

        var response = await participant.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, enrolment) = await SetUpEnrolment();
        await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));

        var second = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsDifferentOrganiser_Returns403()
    {
        var (_, _, enrolment) = await SetUpEnrolment();
        var otherOrganiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnResult()
    {
        var (organiser, participant, enrolment) = await SetUpEnrolment();
        await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));

        var mine = await participant.GetFromJsonAsync<List<ResultDto>>("/api/users/me/results");

        Assert.Single(mine!);
        Assert.Equal(3, mine![0].FinishPosition);
    }

    [Fact]
    public async Task Participant_CannotSeeAnotherParticipantsResult()
    {
        var (organiser, _, enrolment) = await SetUpEnrolment();
        await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolment.EnrolmentId}/result", ResultFor(enrolment.EnrolmentId));
        var stranger = await TestHelpers.LoginAsNewUser(_factory, "Participant");

        var theirs = await stranger.GetFromJsonAsync<List<ResultDto>>("/api/users/me/results");

        Assert.Empty(theirs!);
    }
}