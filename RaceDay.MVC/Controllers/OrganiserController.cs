using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

[RequireMvcRole("Organiser")]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;
    public OrganiserController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var mine = await _api.Get<List<EventDto>>("api/events/mine");
        if (!mine.Success) TempData["Error"] = mine.Error;
        var vm = new OrganiserDashboardViewModel { Events = mine.Data ?? new() };
        foreach (var ev in vm.Events)
        {
            var entrants = await _api.Get<List<EnrolmentDto>>($"api/events/{ev.EventId}/enrolments");
            vm.EntrantsByEvent[ev.EventId] = entrants.Data?.Count ?? 0;
        }
        return View(vm);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new EventFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var reply = await _api.Post<EventDto>("api/events", model.ToRequest());
        if (!reply.Success || reply.Data == null)
        {
            ModelState.AddModelError("", reply.Error ?? "Could not create event.");
            return View("Form", model);
        }
        await UploadBanner(reply.Data.EventId, model.Banner);
        return RedirectToAction(nameof(Manage), new { id = reply.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var reply = await _api.Get<EventDto>($"api/events/{id}");
        if (!reply.Success || reply.Data == null) return NotFound();
        if (reply.Data.OrganiserId != HttpContext.Session.GetInt32("UserId")) return Forbid();
        return View("Form", new EventFormViewModel
        {
            EventName = reply.Data.EventName, Description = reply.Data.Description,
            EventDate = reply.Data.EventDate, Location = reply.Data.Location,
            DistanceKm = reply.Data.DistanceKm, EventType = reply.Data.EventType
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var reply = await _api.Put<EventDto>($"api/events/{id}", model.ToRequest());
        if (!reply.Success)
        {
            ModelState.AddModelError("", reply.Error ?? "Could not update event.");
            return View("Form", model);
        }
        await UploadBanner(id, model.Banner);
        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Manage), new { id });
    }

    private async Task UploadBanner(int id, IFormFile? file)
    {
        if (file == null || file.Length == 0) return;
        var result = await _api.Upload<MediaUploadResponse>($"api/events/{id}/banner", file);
        if (!result.Success) TempData["Error"] = result.Error;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var reply = await _api.Delete<object>($"api/events/{id}");
        TempData[reply.Success ? "Success" : "Error"] = reply.Success ? "Event deleted." : reply.Error;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Manage(int id)
    {
        var ev = await _api.Get<EventDto>($"api/events/{id}");
        if (!ev.Success || ev.Data == null) return NotFound();
        if (ev.Data.OrganiserId != HttpContext.Session.GetInt32("UserId")) return Forbid();
        var entrants = await _api.Get<List<EnrolmentDto>>($"api/events/{id}/enrolments");
        var categories = await _api.Get<List<CategoryDto>>($"api/events/{id}/categories");
        var results = await _api.Get<List<ResultDto>>($"api/events/{id}/results");
        return View(new OrganiserEventViewModel
        {
            Event = ev.Data, Enrolments = entrants.Data ?? new(),
            Categories = categories.Data ?? new(), Results = results.Data ?? new()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, CreateCategoryRequest model)
    {
        var reply = await _api.Post<CategoryDto>($"api/events/{eventId}/categories", model);
        TempData[reply.Success ? "Success" : "Error"] =
            reply.Success ? "Category added." : reply.Error;
        return RedirectToAction(nameof(Manage), new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var reply = await _api.Delete<object>($"api/categories/{categoryId}");
        TempData[reply.Success ? "Success" : "Error"] =
            reply.Success ? "Category deleted." : reply.Error;
        return RedirectToAction(nameof(Manage), new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int eventId, int enrolmentId, string status)
    {
        var reply = await _api.Put<EnrolmentDto>($"api/enrolments/{enrolmentId}/status",
            new UpdateEnrolmentStatusRequest { Status = status });
        TempData[reply.Success ? "Success" : "Error"] =
            reply.Success ? "Enrolment status updated." : reply.Error;
        return RedirectToAction(nameof(Manage), new { id = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultEntryViewModel model)
    {
        var current = await _api.Get<List<ResultDto>>($"api/events/{model.EventId}/results");
        var existing = current.Data?.FirstOrDefault(r => r.EnrolmentId == model.EnrolmentId);
        var request = new CreateResultRequest
        {
            EnrolmentId = model.EnrolmentId, FinishTime = model.FinishTime,
            FinishPosition = model.FinishPosition, CategoryPosition = model.CategoryPosition, Status = model.Status
        };
        var reply = existing == null
            ? await _api.Post<ResultDto>($"api/enrolments/{model.EnrolmentId}/result", request)
            : await _api.Put<ResultDto>($"api/results/{existing.ResultId}", request);
        TempData[reply.Success ? "Success" : "Error"] = reply.Success ? "Result saved." : reply.Error;
        return RedirectToAction(nameof(Manage), new { id = model.EventId });
    }
}
