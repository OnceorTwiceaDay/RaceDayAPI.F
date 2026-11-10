using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.MVC.Models;
using RaceDay.MVC.Services;

namespace RaceDay.MVC.Controllers;

[RequireMvcRole("Participant")]
public class ParticipantController : Controller
{
    private readonly RaceDayApiClient _api;
    public ParticipantController(RaceDayApiClient api) => _api = api;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var reply = await _api.Post<EnrolmentDto>($"api/events/{eventId}/enrolments",
            new CreateEnrolmentRequest { CategoryId = categoryId });
        TempData[reply.Success ? "Success" : "Error"] = reply.Success
            ? "You have successfully entered the event." : reply.Error;
        return RedirectToAction("Details", "Home", new { id = eventId });
    }

    public async Task<IActionResult> Enrolments()
    {
        var reply = await _api.Get<List<EnrolmentDto>>("api/users/me/enrolments");
        if (!reply.Success) TempData["Error"] = reply.Error;
        return View(reply.Data ?? new());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var reply = await _api.Delete<object>($"api/enrolments/{id}");
        TempData[reply.Success ? "Success" : "Error"] = reply.Success
            ? "Enrolment cancelled." : reply.Error;
        return RedirectToAction(nameof(Enrolments));
    }

    public async Task<IActionResult> Results()
    {
        var reply = await _api.Get<List<ResultDto>>("api/users/me/results");
        if (!reply.Success) TempData["Error"] = reply.Error;
        var vm = new ParticipantResultsViewModel { Results = reply.Data ?? new() };
        foreach (var eventId in vm.Results.Select(r => r.EventId).Distinct())
        {
            var leaderboard = await _api.Get<List<ResultDto>>($"api/events/{eventId}/results");
            vm.RecordedFinishersByEvent[eventId] = leaderboard.Data?
                .Count(r => r.Status == "Finished") ?? 0;
        }
        return View(vm);
    }
}
