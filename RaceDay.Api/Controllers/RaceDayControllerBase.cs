using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Base class for any controller that needs to know who is logged in.
/// Safe to read CurrentUserId/CurrentRole only on actions protected by
/// [RequireRole] - that filter runs first and guarantees the session
/// values exist before the action body executes.
/// </summary>
public abstract class RaceDayControllerBase : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetInt32("UserId")!.Value;
    protected string CurrentRole => HttpContext.Session.GetString("Role")!;
}
