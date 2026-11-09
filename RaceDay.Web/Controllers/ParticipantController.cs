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
}