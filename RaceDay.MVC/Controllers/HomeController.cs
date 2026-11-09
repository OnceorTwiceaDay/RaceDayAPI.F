using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;
    public HomeController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? type, string? search)
    {
        var reply = await _api.Get<List<EventDto>>("api/events");
        if (!reply.Success) TempData["Error"] = reply.Error;
        var events = reply.Data ?? new List<EventDto>();
        if (!string.IsNullOrWhiteSpace(type)) events = events.Where(e =>
            e.EventType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(search)) events = events.Where(e =>
            e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        ViewBag.SelectedType = type;
        ViewBag.Search = search;
        return View(events.OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.Get<EventDto>($"api/events/{id}");
        if (!ev.Success || ev.Data == null) return NotFound();
        var categories = await _api.Get<List<CategoryDto>>($"api/events/{id}/categories");
        var routes = await _api.Get<List<RouteDto>>($"api/events/{id}/routes");
        var results = await _api.Get<List<ResultDto>>($"api/events/{id}/results");
        return View(new EventDetailsViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new(),
            Routes = routes.Data ?? new(),
            Results = results.Data ?? new()
        });
    }

    public IActionResult Error() => View();
}
