using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Tests;

public class ResultTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ResultTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Organiser_CanRecordResult_AndParticipantCanViewIt()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);
        var enrolResponse = await TestHelper.EnrolAsync(participant, eventId, categoryId);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentDto>();

        var response = await organiser.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = enrolment!.EnrolmentId,
            finishTime = "00:48:32",
            position = 1,
            status = "Finished"
        });
        var mine = await participant.GetFromJsonAsync<List<ResultDto>>("/api/results/mine");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(mine!);
        Assert.Equal(1, mine!.Single().Position);
    }

    [Fact]
    public async Task Participant_CannotRecordResult()
    {
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");

        var response = await participant.PostAsJsonAsync("/api/results", new
        {
            enrolmentId = 1,
            finishTime = "00:30:00",
            position = 1,
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RecordResult_Twice_Returns409()
    {
        var organiser = await TestHelper.CreateLoggedInClientAsync(_factory, "Organiser");
        var participant = await TestHelper.CreateLoggedInClientAsync(_factory, "Participant");
        var categoryId = await TestHelper.CreateCategoryAsync(organiser);
        var eventId = await TestHelper.CreateEventAsync(organiser);
        await TestHelper.AddCategoryToEventAsync(organiser, eventId, categoryId);
        var enrolResponse = await TestHelper.EnrolAsync(participant, eventId, categoryId);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentDto>();
        var body = new { enrolmentId = enrolment!.EnrolmentId, finishTime = "00:50:00", position = 1, status = "Finished" };
        await organiser.PostAsJsonAsync("/api/results", body);

        var response = await organiser.PostAsJsonAsync("/api/results", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
