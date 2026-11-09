using Microsoft.AspNetCore.Mvc;

[SessionAuthorize("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiService _api;

    public ParticipantController(ApiService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Events(string? city, string? type, DateOnly? date)
    {
        var client = _api.CreateClient();

        var url = "api/events?status=Upcoming";
        if (!string.IsNullOrWhiteSpace(city)) url += $"&city={Uri.EscapeDataString(city)}";
        if (date.HasValue) url += $"&date={date:yyyy-MM-dd}";

        var response = await client.GetAsync(url);

        var events = new List<EventItem>();
        if (response.IsSuccessStatusCode)
            events = await response.Content.ReadFromJsonAsync<List<EventItem>>() ?? new();
        else
            TempData["Error"] = "Could not load events. Please try again.";

        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => e.EventType == type).ToList();

        ViewBag.City = city;
        ViewBag.Type = type;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");

        return View(events);
    }

    public async Task<IActionResult> EventDetails(int id)
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync($"api/events/{id}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Events");
        }

        var ev = await response.Content.ReadFromJsonAsync<EventItem>();
        return View(ev);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, int categoryId)
    {
        var client = _api.CreateClient();
        var response = await client.PostAsJsonAsync($"api/events/{id}/enrol", new { CategoryId = categoryId });

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "You are enrolled. Good luck!";
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            TempData["Error"] = "You are already enrolled, or this category is full.";
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            TempData["Error"] = "Registration is closed for this event.";
        else
            TempData["Error"] = "Could not enrol. Please try again.";

        return RedirectToAction("EventDetails", new { id });
    }

    public async Task<IActionResult> MyEnrolments()
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync("api/enrolments/mine");

        var list = new List<EnrolmentItem>();
        if (response.IsSuccessStatusCode)
            list = await response.Content.ReadFromJsonAsync<List<EnrolmentItem>>() ?? new();
        else
            TempData["Error"] = "Could not load your enrolments.";

        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var client = _api.CreateClient();
        var response = await client.DeleteAsync($"api/enrolments/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Enrolment cancelled.";
        else
            TempData["Error"] = "Could not cancel the enrolment.";

        return RedirectToAction("MyEnrolments");
    }

    public async Task<IActionResult> MyResults()
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync("api/results/mine");

        var results = new List<ResultItem>();
        if (response.IsSuccessStatusCode)
            results = await response.Content.ReadFromJsonAsync<List<ResultItem>>() ?? new();
        else
            TempData["Error"] = "Could not load your results.";

        var rows = new List<MyResultRow>();

        foreach (var r in results)
        {
            var row = new MyResultRow { Result = r };

            var eventResponse = await client.GetAsync($"api/events/{r.EventId}");
            if (eventResponse.IsSuccessStatusCode)
            {
                var ev = await eventResponse.Content.ReadFromJsonAsync<EventItem>();
                row.EventDate = ev?.EventDate;
            }

            var allResponse = await client.GetAsync($"api/events/{r.EventId}/results");
            if (allResponse.IsSuccessStatusCode)
            {
                var all = await allResponse.Content.ReadFromJsonAsync<List<ResultItem>>() ?? new();
                row.TotalFinishers = all.Count(x => x.Status == "Finished");
            }

            rows.Add(row);
        }

        return View(rows.OrderByDescending(x => x.EventDate).ToList());
    }
   
}