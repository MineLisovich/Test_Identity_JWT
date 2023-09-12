using Microservice.Authorization.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Microservice.Authorization.Areas.Admin.Controllers
{
    [Area(UserRoles.Admin)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
