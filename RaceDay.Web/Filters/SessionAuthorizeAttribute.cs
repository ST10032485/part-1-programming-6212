using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public SessionAuthorizeAttribute(string? role = null)
    {
        _role = role;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var token = context.HttpContext.Session.GetString("Token");
        var role = context.HttpContext.Session.GetString("Role");

        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (_role != null && role != _role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}