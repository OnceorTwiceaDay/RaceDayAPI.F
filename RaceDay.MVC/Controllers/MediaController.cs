using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

// Only the RaceDay API communicates with Azure Blob Storage.
// MVC returns the API's validated public image bytes to the visitor.
[Route("media")]
public class MediaController : Controller
{
    private readonly RaceDayApiClient _api;
    public MediaController(RaceDayApiClient api) => _api = api;

    [HttpGet("{kind}/{name}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Get(string kind, string name)
    {
        if (kind is not ("banners" or "profiles") ||
            !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-f0-9]{32}\.(jpg|png|webp)$"))
            return NotFound();
        var (bytes, mime) = await _api.GetMedia($"api/media/{kind}/{name}");
        return bytes == null ? NotFound() : File(bytes, mime ?? "application/octet-stream");
    }
}
