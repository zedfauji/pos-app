using MagiDesk.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OrderApi.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireOpenShiftAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var shiftService = context.HttpContext.RequestServices.GetService<IShiftService>();
        
        if (shiftService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        var openShift = await shiftService.GetCurrentOpenShiftAsync();
        if (openShift == null)
        {
            // Auto-open shift for Legacy Parity (Legacy system didn't require explicit open shift)
            var userId = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                         ?? context.HttpContext.User.FindFirst("sub")?.Value
                         ?? "system";
            var userName = context.HttpContext.User.Identity?.Name ?? "System Auto-Shift";
            
            try 
            {
               openShift = await shiftService.OpenSystemShiftAsync(userId, userName);
            }
            catch (Exception ex) 
            {
                 context.Result = new ObjectResult(new { error = "SHIFT_AUTO_OPEN_FAILED", message = $"Failed to auto-open legacy shift: {ex.Message}" }) 
                 { 
                     StatusCode = 500 
                 };
                 return;
            }
        }

        // Optional: Inject current shift ID into items if needed for auditing
        context.HttpContext.Items["CurrentShiftId"] = openShift.ShiftId;

        await next();
    }
}
