using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class EnrolmentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EnrolmentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);

        var response = await TestHelper.EnrolAsync(participant, eventId, categoryId);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(eventId, enrolment!.EventId);
        Assert.Equal(categoryId, enrolment.CategoryId);
        Assert.Equal(1, enrolment.BibNumber);

        // The participant sees it
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");
        Assert.Single(mine!);

        // The organiser sees it
        var forOrganiser = await organiser.GetFromJsonAsync<List<EnrolmentDto>>($"/api/events/{eventId}/enrolments");
        Assert.Single(forOrganiser!);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);
        await TestHelper.EnrolAsync(participant, eventId, categoryId);

        var response = await TestHelper.EnrolAsync(participant, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryNotInEvent_Returns404()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);   // category not added to the event

        var response = await TestHelper.EnrolAsync(participant, eventId, categoryId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_AfterDeadline_Returns400()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser, eventDate: "2020-06-01", deadline: "2020-05-20");
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);

        var response = await TestHelper.EnrolAsync(participant, eventId, categoryId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var first = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var second = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId, maxParticipants: 1);

        var firstResponse = await TestHelper.EnrolAsync(first, eventId, categoryId);
        var secondResponse = await TestHelper.EnrolAsync(second, eventId, categoryId);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Participant_CanCancelEnrolment()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);
        var enrolResponse = await TestHelper.EnrolAsync(participant, eventId, categoryId);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentDto>();

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentId}");
        var mine = await participant.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Cancelled", mine!.Single().Status);
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var owner = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var other = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);
        var enrolResponse = await TestHelper.EnrolAsync(owner, eventId, categoryId);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentDto>();

        var response = await other.DeleteAsync($"/api/enrolments/{enrolment!.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
