using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

[RequireMvcRole]
public class ProfileController : Controller
{
    private readonly RaceDayApiClient _api;
    public ProfileController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var reply = await _api.Get<ProfileDto>("api/users/me");
        if (!reply.Success || reply.Data == null)
        {
            TempData["Error"] = reply.Error;
            return RedirectToAction("Index", "Home");
        }
        return View(new ProfilePageViewModel { Profile = reply.Data });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(UpdateProfileRequest model, IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(model.FullName))
        {
            TempData["Error"] = "Full name is required.";
            return RedirectToAction(nameof(Index));
        }
        var reply = await _api.Put<ProfileDto>("api/users/me", model);
        if (!reply.Success)
        {
            TempData["Error"] = reply.Error;
            return RedirectToAction(nameof(Index));
        }
        HttpContext.Session.SetString("FullName", model.FullName);
        if (image != null && image.Length > 0)
        {
            var upload = await _api.Upload<MediaUploadResponse>("api/users/me/photo", image);
            if (!upload.Success) TempData["Error"] = upload.Error;
        }
        TempData["Success"] = "Profile saved.";
        return RedirectToAction(nameof(Index));
    }
}
