using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Real HTTP upload and download through the API with a local Azurite emulator.
/// CI enables the emulator via RACEDAY_AZURITE_TESTS=1. Outside CI the tests
/// that need Azurite are opt-in, so ordinary local dotnet test still works.
/// </summary>
public sealed class MediaTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public MediaTests(RaceDayFactory factory) => _factory = factory;

    // An image with a valid PNG signature is sufficient for API/Blob round-trip tests.
    private static readonly byte[] PngBytes =
        [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0];

    [Fact]
    public async Task OrganiserUploadsBanner_ThenImageIsReadableThroughApi()
    {
        if (!AzuriteTestsEnabled()) return;
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        using var upload = ImageContent(PngBytes, "event.png", "image/png");

        var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", upload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MediaUploadResponse>();
        Assert.NotNull(result);
        Assert.StartsWith("/api/media/banners/", result.MediaUrl);

        // The full path is saved through EF Core and returned by public event API.
        var eventAfterUpload = await organiser.GetFromJsonAsync<EventDto>($"/api/events/{ev.EventId}");
        Assert.Equal(result.MediaUrl, eventAfterUpload!.BannerImageUrl);

        var publicClient = _factory.CreateClient();
        var downloaded = await publicClient.GetAsync(result.MediaUrl);
        Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
        Assert.Equal("image/png", downloaded.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await downloaded.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task ParticipantUploadsProfilePhoto_ThenImageIsReadableThroughApi()
    {
        if (!AzuriteTestsEnabled()) return;
        var participant = await TestHelpers.LoginAsNewUser(_factory, "Participant");
        using var upload = ImageContent(PngBytes, "avatar.png", "image/png");

        var response = await participant.PostAsync("/api/users/me/photo", upload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var uploaded = await response.Content.ReadFromJsonAsync<MediaUploadResponse>();
        Assert.NotNull(uploaded);
        Assert.StartsWith("/api/media/profiles/", uploaded.MediaUrl);

        var profile = await participant.GetFromJsonAsync<ProfileDto>("/api/users/me");
        Assert.Equal(uploaded.MediaUrl, profile!.ProfileImageUrl);

        var downloaded = await _factory.CreateClient().GetAsync(uploaded.MediaUrl);
        Assert.Equal(HttpStatusCode.OK, downloaded.StatusCode);
        Assert.Equal(PngBytes, await downloaded.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadRejectsFakePngWithoutValidSignature()
    {
        var organiser = await TestHelpers.LoginAsNewUser(_factory, "Organiser");
        var ev = await TestHelpers.CreateEvent(organiser);
        using var upload = ImageContent([0, 1, 2, 3, 4, 5, 6, 7, 8], "fake.png", "image/png");

        var response = await organiser.PostAsync($"/api/events/{ev.EventId}/banner", upload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotUploadEventBanner()
    {
        using var upload = ImageContent(PngBytes, "event.png", "image/png");
        var response = await _factory.CreateClient().PostAsync("/api/events/1/banner", upload);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static bool AzuriteTestsEnabled() =>
        Environment.GetEnvironmentVariable("RACEDAY_AZURITE_TESTS") == "1";

    private static MultipartFormDataContent ImageContent(byte[] bytes, string filename, string contentType)
    {
        var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(file, "file", filename);
        return multipart;
    }
}
