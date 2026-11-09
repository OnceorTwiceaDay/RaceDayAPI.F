using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class AccountController : Controller
{
    private readonly RaceDayApiClient _api;
    public AccountController(RaceDayApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var reply = await _api.Post<AuthResponse>("api/auth/login", new LoginRequest
        {
            Email = model.Email, Password = model.Password
        });
        if (!reply.Success || reply.Data == null)
        {
            ModelState.AddModelError("", reply.Error ?? "Login failed.");
            return View(model);
        }
        HttpContext.Session.SetString("Role", reply.Data.Role);
        HttpContext.Session.SetString("FullName", reply.Data.FullName);
        HttpContext.Session.SetInt32("UserId", reply.Data.UserId);
        return RedirectToAction("Index", reply.Data.Role == "Organiser" ? "Organiser" : "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var reply = await _api.Post<AuthResponse>("api/auth/register", new RegisterRequest
        {
            FullName = model.FullName, Email = model.Email, Password = model.Password, Role = model.Role
        });
        if (!reply.Success)
        {
            ModelState.AddModelError("", reply.Error ?? "Registration failed.");
            return View(model);
        }
        TempData["Success"] = "Account created. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.Post<object>("api/auth/logout", new { });
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }
}
