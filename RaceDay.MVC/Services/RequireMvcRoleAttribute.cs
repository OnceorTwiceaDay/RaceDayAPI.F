using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.MVC.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireMvcRoleAttribute : ActionFilterAttribute
{
    private readonly string? _role;
    public RequireMvcRoleAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        if (string.IsNullOrEmpty(session.GetString("ApiCookie")))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (_role != null && !string.Equals(session.GetString("Role"), _role, StringComparison.Ordinal))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
}
