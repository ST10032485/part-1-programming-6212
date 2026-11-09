using Microsoft.AspNetCore.Mvc;

[SessionAuthorize]
public class ProfileController : Controller
{
    private readonly ApiService _api;

    public ProfileController(ApiService api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var client = _api.CreateClient();
        var response = await client.GetAsync("api/users/me");

        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Could not load your profile.";
            return RedirectToAction("Index", "Home");
        }

        var profile = await response.Content.ReadFromJsonAsync<ProfileModel>();
        return View(profile);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _api.CreateClient();
        var update = new
        {
            model.FirstName,
            model.LastName,
            model.Phone,
            model.DateOfBirth
        };

        var response = await client.PutAsJsonAsync("api/users/me", update);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError("", "Could not update your profile. Please check your details.");
        return View(model);
    }
}