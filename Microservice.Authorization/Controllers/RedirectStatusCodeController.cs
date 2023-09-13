using Microsoft.AspNetCore.Mvc;

namespace Microservice.Authorization.Controllers
{
    public class RedirectStatusCodeController : Controller
    {
        public IActionResult Forbidden()
        {
            return View();
        }
    }
}
