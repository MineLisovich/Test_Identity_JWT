
using Microservice.Authorization.Data.Entities;
using Microservice.Authorization.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace Microservice.Authorization.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpGet]
        public  IActionResult UserProfile() 
        {
            return View();
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpGet]
        public async Task<IActionResult> Test()
        {
            var token = HttpContext.Request.Cookies["Microcervice.Authorization.Cookie"];
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri($"https://localhost:7125");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await httpClient.GetAsync("/WeatherForecast");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    List<WeatherModel>? data = JsonConvert.DeserializeObject<List<WeatherModel>>(result);
                    return View(data);
                }     
                return Json("ERROR");            
            }
        }
    }
}