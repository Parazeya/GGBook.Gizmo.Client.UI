using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Gizmo.Plugin.GGBook.Filters
{
    /// <summary>
    /// Checks X-GGMod-Key header against GGMod:ApiKey in configuration.
    /// If ApiKey is not configured (empty), all requests are allowed (setup mode).
    /// </summary>
    public sealed class ApiKeyAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var config = context.HttpContext.RequestServices.GetService<IConfiguration>();
            var expectedKey = config?["GGMod:ApiKey"];

            // No key configured → open access (initial setup mode)
            if (string.IsNullOrWhiteSpace(expectedKey))
                return;

            var actualKey = context.HttpContext.Request.Headers["X-GGMod-Key"].ToString();
            if (actualKey != expectedKey)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    status = "error",
                    message = "Invalid or missing X-GGMod-Key header"
                });
            }
        }
    }
}
