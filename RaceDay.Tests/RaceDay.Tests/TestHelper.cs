using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;

namespace RaceDay.Tests;

// Small helper methods so each test stays short
public static class TestHelper
{
    public const string Password = "Password1!";

    // Registers a new user with the given role, logs in and returns a client carrying the token
    public static async Task<HttpClient> CreateLoggedInClientAsync(ApiFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@test.com";

        await client.PostAsJsonAsync("/api/auth/register",
            new { firstName = "Test", lastName = role, email, password = Password, role });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "Cat" + Guid.NewGuid().ToString("N")[..8],
            distanceKm = 10,
            description = "Test category"
        });
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        return category!.CategoryId;
    }

    public static object NewEvent(string eventDate = "2030-06-01", string deadline = "2030-05-20") => new
    {
        title = "Test Run",
        description = "A test event",
        venue = "Central Park",
        city = "Johannesburg",
        eventDate,
        registrationDeadline = deadline,
        eventType = "Run",
        distanceKm = 10
    };

    public static async Task<int> CreateEventAsync(HttpClient organiser,
        string eventDate = "2030-06-01", string deadline = "2030-05-20")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", NewEvent(eventDate, deadline));
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();
        return ev!.EventId;
    }

    public static async Task AddCategoryToEventAsync(HttpClient organiser, int eventId,
        int categoryId, int maxParticipants = 100)
    {
        await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { categoryId, entryFee = 100, maxParticipants });
    }

    public static Task<HttpResponseMessage> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        return participant.PostAsJsonAsync($"/api/events/{eventId}/enrol", new { categoryId });
    }
}
