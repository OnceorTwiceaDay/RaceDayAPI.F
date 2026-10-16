using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Api.Models;

namespace RaceDay.Api.Filters;

/// <summary>
/// Protects an endpoint using the session set up in AuthController.Login.
/// - No session at all              -> 401 Unauthorized
/// - Session exists, wrong role     -> 403 Forbidden
/// - Session exists, role allowed   -> action runs
///
/// [RequireRole]                          -> any logged-in user ("Any" in the endpoint plan)
/// [RequireRole(Role.Organiser)]          -> Organiser only
/// [RequireRole(Role.Participant)]        -> Participant only
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly Role[] _allowedRoles;

    public RequireRoleAttribute(params Role[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var roleString = session.GetString("Role");

        if (userId is null || roleString is null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in to do this." });
            return;
        }

        if (_allowedRoles.Length > 0)
        {
            var isAllowed = Enum.TryParse<Role>(roleString, out var role) && Array.IndexOf(_allowedRoles, role) >= 0;
            if (!isAllowed)
            {
                context.Result = new ObjectResult(new { message = "You do not have permission to perform this action." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
