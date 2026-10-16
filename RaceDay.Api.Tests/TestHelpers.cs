using System.Net.Http.Json;
using RaceDay.Contracts;

namespace RaceDay.Api.Tests;

public static class TestHelpers
{
    public const string Password = "Password123!";

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@test.co.za";

    public static RegisterRequest NewUser(string role, string? email = null) => new()
    {
        FullName = $"Test {role}",
        Email = email ?? UniqueEmail(),
        Password = Password,
        Role = role,
        PhoneNumber = "0820000000",
        DateOfBirth = new DateOnly(1990, 1, 1)
    };

    /// <summary>Registers a brand-new user and logs them in. The client keeps the session cookie.</summary>
    public static async Task<HttpClient> LoginAsNewUser(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var user = NewUser(role);

        (await client.PostAsJsonAsync("/api/auth/register", user)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = Password })).EnsureSuccessStatusCode();

        return client;
    }

    public static async Task<EventDto> CreateEvent(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new CreateEventRequest
        {
            EventName = "Test Run",
            Description = "A test event",
            EventDate = new DateOnly(2030, 1, 1),
            Location = "Cape Town",
            DistanceKm = 10m,
            EventType = "Running"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventDto>())!;
    }

    public static async Task<CategoryDto> CreateCategory(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new CreateCategoryRequest
            {
                CategoryName = "10km Open",
                DistanceKm = 10m,
                MinAge = 12,
                MaxAge = null,
                EntryFee = 100m
            });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    public static async Task<EnrolmentDto> Enrol(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = categoryId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EnrolmentDto>())!;
    }
}