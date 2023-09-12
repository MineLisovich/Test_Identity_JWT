using Microservice.Authorization.Data.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Microservice.Authorization.Areas.User.Controllers
{
    [Area(UserRoles.User)]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
