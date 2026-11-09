using Microsoft.AspNetCore.Mvc;
using System.Net;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _factory;

    public AccountController(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _factory.CreateClient("RaceDayApi");
        var response = await client.PostAsJsonAsync("api/auth/register", model);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Account created. Please log in.";
            return RedirectToAction("Login");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
            ModelState.AddModelError("", "That email is already registered.");
        else
            ModelState.AddModelError("", "Registration failed. Please check your details.");

        return View(model);
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var client = _factory.CreateClient("RaceDayApi");
        var response = await client.PostAsJsonAsync("api/auth/login", model);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", "Wrong email or password.");
            return View(model);
        }

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        HttpContext.Session.SetString("Token", result!.Token);
        HttpContext.Session.SetString("Role", result.Role ?? result.RoleName ?? "");
        HttpContext.Session.SetString("Email", result.Email);
        HttpContext.Session.SetInt32("UserId", result.UserId);

        return RedirectToAction("Index", "Home");
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();
}