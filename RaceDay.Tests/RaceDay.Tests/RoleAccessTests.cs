using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Tests;

// Checks that Organiser and Participant functionality is kept separate
public class RoleAccessTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RoleAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CannotCreateEvent()
    {
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/events", TestHelper.NewEvent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory()
    {
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/categories",
            new { categoryName = "Not Allowed", distanceKm = 5 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var eventId = await TestHelper.CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInAnEvent()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);

        var response = await TestHelper.EnrolAsync(organiser, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewParticipantEnrolmentList()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.GetAsync("/api/enrolments/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NoToken_CannotCreateEvent()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/events", TestHelper.NewEvent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NoToken_CannotEnrol()
    {
        var anonymous = _factory.CreateClient();

        var response = await TestHelper.EnrolAsync(anonymous, 1, 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
