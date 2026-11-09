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

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync($"api/events/{id}");

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Events");
        }

        var e = await response.Content.ReadFromJsonAsync<EventItem>();

        var model = new EventFormModel
        {
            Title = e!.Title,
            Description = e.Description,
            Venue = e.Venue,
            City = e.City,
            EventDate = e.EventDate,
            RegistrationDeadline = e.RegistrationDeadline,
            EventType = e.EventType,
            DistanceKm = e.DistanceKm
        };

        ViewBag.EventId = id;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventFormModel model)
    {
        ViewBag.EventId = id;
        if (!ModelState.IsValid) return View(model);

        if (model.RegistrationDeadline > model.EventDate)
        {
            ModelState.AddModelError("", "Registration deadline must be on or before the event date.");
            return View(model);
        }

        var client = _api.CreateClient();
        var response = await client.PutAsJsonAsync($"api/events/{id}", model);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Event updated.";
            return RedirectToAction("Events");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            ModelState.AddModelError("", "You can only edit your own events.");
        else
            ModelState.AddModelError("", "Could not update the event. Please check your details.");

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

    public async Task<IActionResult> Categories()
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync("api/events/mine");

        var events = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<EventItem>>() ?? new()
            : new List<EventItem>();

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> ManageCategories(int id)
    {
        var client = _api.CreateClient();

        var eventResponse = await client.GetAsync($"api/events/{id}");
        if (!eventResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Categories");
        }

        var categoriesResponse = await client.GetAsync("api/categories");

        var model = new ManageCategoriesViewModel
        {
            Event = (await eventResponse.Content.ReadFromJsonAsync<EventItem>())!,
            AllCategories = categoriesResponse.IsSuccessStatusCode
                ? await categoriesResponse.Content.ReadFromJsonAsync<List<CategoryItem>>() ?? new()
                : new()
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategoryToEvent(int id, AddCategoryModel form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fill in all fields correctly.";
            return RedirectToAction("ManageCategories", new { id });
        }

        var client = _api.CreateClient();
        var response = await client.PostAsJsonAsync($"api/events/{id}/categories", form);

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category added to the event.";
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            TempData["Error"] = "That category is already on this event.";
        else
            TempData["Error"] = "Could not add the category.";

        return RedirectToAction("ManageCategories", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCategoryFromEvent(int id, int categoryId)
    {
        var client = _api.CreateClient();
        var response = await client.DeleteAsync($"api/events/{id}/categories/{categoryId}");

        if (response.IsSuccessStatusCode)
            TempData["Success"] = "Category removed.";
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            TempData["Error"] = "This category has enrolments and cannot be removed.";
        else
            TempData["Error"] = "Could not remove the category.";

        return RedirectToAction("ManageCategories", new { id });
    }

    [HttpGet]
    public IActionResult CreateCategory() => View(new CategoryFormModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CategoryFormModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _api.CreateClient();
        var response = await client.PostAsJsonAsync("api/categories", model);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Category created. You can now add it to an event.";
            return RedirectToAction("Categories");
        }

        ModelState.AddModelError("", "Could not create the category. Please check your details.");
        return View(model);
    }

    public async Task<IActionResult> Enrolments()
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync("api/events/mine");

        var events = response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<EventItem>>() ?? new()
            : new List<EventItem>();

        return View(events);
    }

    [HttpGet]
    public async Task<IActionResult> EventEnrolments(int id)
    {
        var client = _api.CreateClient();

        var eventResponse = await client.GetAsync($"api/events/{id}");
        if (!eventResponse.IsSuccessStatusCode)
        {
            TempData["Error"] = "Event not found.";
            return RedirectToAction("Enrolments");
        }

        var ev = await eventResponse.Content.ReadFromJsonAsync<EventItem>();
        ViewBag.EventTitle = ev!.Title;
        ViewBag.EventId = id;

        var response = await client.GetAsync($"api/events/{id}/enrolments");

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.LoadError = "Could not load enrolments for this event.";
            return View(new List<EnrolmentItem>());
        }

        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentItem>>() ?? new();
        return View(list);
    }
}

