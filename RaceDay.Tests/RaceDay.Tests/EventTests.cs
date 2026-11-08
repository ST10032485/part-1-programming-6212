using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class EventTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EventTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanCreateEvent()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", TestHelper.NewEvent());
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Run", ev!.EventType);
        Assert.Equal("Upcoming", ev.Status);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelper.CreateEventAsync(organiser);

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            title = "Updated Title",
            venue = "New Venue",
            city = "Cape Town",
            eventDate = "2030-07-01",
            registrationDeadline = "2030-06-20",
            eventType = "Cycle",
            distanceKm = 40,
            status = "Upcoming"
        });
        var ev = await response.Content.ReadFromJsonAsync<EventDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Updated Title", ev!.Title);
        Assert.Equal("Cycle", ev.EventType);
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelper.CreateEventAsync(organiser);

        var deleteResponse = await organiser.DeleteAsync($"/api/events/{eventId}");
        var getResponse = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUpdateAnotherOrganisersEvent()
    {
        var owner = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var otherOrganiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var eventId = await TestHelper.CreateEventAsync(owner);

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            title = "Hacked",
            venue = "Somewhere",
            city = "Somewhere",
            eventDate = "2030-07-01",
            registrationDeadline = "2030-06-20",
            eventType = "Run",
            distanceKm = 5
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InvalidEventType_Returns400()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            title = "Bad Event",
            venue = "Park",
            city = "Town",
            eventDate = "2030-06-01",
            registrationDeadline = "2030-05-20",
            eventType = "Swim",
            distanceKm = 5
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_DeadlineAfterEventDate_Returns400()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events",
            TestHelper.NewEvent(eventDate: "2030-06-01", deadline: "2030-06-10"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Anyone_CanViewEvents()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        await TestHelper.CreateEventAsync(organiser);
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/events");
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(events!);
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
