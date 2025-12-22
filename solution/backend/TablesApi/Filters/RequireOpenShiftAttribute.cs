using MagiDesk.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TablesApi.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireOpenShiftAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Service Locator used here to avoid modifying Program.cs for filter registration
        var shiftService = context.HttpContext.RequestServices.GetService<IShiftService>();
        
        if (shiftService == null)
        {
            // Should not happen if DI is correct
            context.Result = new StatusCodeResult(500);
            return;
        }

        var openShift = await shiftService.GetCurrentOpenShiftAsync();
        if (openShift == null)
        {
            context.Result = new ObjectResult(new { error = "NO_SHIFT_OPEN", message = "Operation requires an open shift." })
            {
                StatusCode = 423 // Locked
            };
            return;
        }

        // Optional: Inject current shift ID into items if needed for auditing
        context.HttpContext.Items["CurrentShiftId"] = openShift.ShiftId;

        await next();
    }
}
