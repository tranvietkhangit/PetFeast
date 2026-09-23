using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PetFeast.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            Response.StatusCode = statusCode;

            return View(statusCode.ToString());
        }
        [Route("Error")]
        public IActionResult Error()
        {
            var exceptionHandler =
                HttpContext.Features
                    .Get<IExceptionHandlerPathFeature>();
            return View("500");
        }
    }
}