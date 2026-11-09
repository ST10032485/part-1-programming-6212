using Microsoft.AspNetCore.Mvc;

[SessionAuthorize("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiService _api;

    public OrganiserController(ApiService api)
    {
        _api = api;
    }

    public async Task<IActionResult> Events()
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync("api/events/mine");

        var events = new List<EventItem>();
        if (response.IsSuccessStatusCode)
            events = await response.Content.ReadFromJsonAsync<List<EventItem>>() ?? new();
        else
            TempData["Error"] = "Could not load your events. Please try again.";

        return View(events);
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new EventFormModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (model.RegistrationDeadline > model.EventDate)
        {
            ModelState.AddModelError("", "Registration deadline must be on or before the event date.");
            return View(model);
        }

        var client = _api.CreateClient();
        var response = await client.PostAsJsonAsync("api/events", model);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Event created.";
            return RedirectToAction("Events");
        }

        ModelState.AddModelError("", "Could not create the event. Please check your details.");
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var client = _api.CreateClient();
        var response = await client.DeleteAsync($"api/events/{id}");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Event deleted.";
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            TempData["Error"] = "This event has enrolments and cannot be deleted.";
        else
            TempData["Error"] = "Could not delete the event.";

        return RedirectToAction("Events");
    }
}