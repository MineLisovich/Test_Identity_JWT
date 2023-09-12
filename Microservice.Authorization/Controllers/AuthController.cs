
using Microservice.Authorization.Data;
using Microservice.Authorization.Data.Entities;
using Microservice.Authorization.Models;
using Microservice.Authorization.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Microservice.Authorization.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;



        public AuthController(UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {    
            return View(new LoginViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> Login (LoginViewModel viewModel)
        {   
            if(viewModel.UserName ==null || viewModel.Password == null)
            {
                return View(viewModel);
            }
            var user = await _userManager.FindByNameAsync(viewModel.UserName);
            if (user !=null && await _userManager.CheckPasswordAsync(user, viewModel.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaimes = new List<Claim>
                {
                    new Claim (ClaimTypes.Name, user.UserName),
                    new Claim (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaimes.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = GetToken(authClaimes);
                var endtoken = new JwtSecurityTokenHandler().WriteToken(token);

                
                var result = await _signInManager.PasswordSignInAsync(viewModel.UserName, viewModel.Password, false, false);
                if (result.Succeeded)
                {
                    HttpContext.Response.Cookies.Append("Microcervice.Authorization.Cookie", endtoken);
                    return RedirectToAction("UserProfile", "Home");
                }
                   
            }
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Registration()
        {
            return View(new RegistrationViewModel());
        }
        [HttpPost]
        public async Task<IActionResult> Registration (RegistrationViewModel viewModel)
        {
            if (viewModel.UserName == null || viewModel.Password == null || viewModel.Email == null )
            {
                return View(viewModel);
            }
            var userExists = await _userManager.FindByNameAsync(viewModel.UserName);
            if (userExists != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ResponseModel { Status = "Error", Message = "User already exists!" });
            }

            IdentityUser user = new()
            {
                UserName = viewModel.UserName,
                Email = viewModel.Email,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            
            var result = await _userManager.CreateAsync(user, viewModel.Password);
            if (!result.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseModel { Status = "Error", Message = "User creation failed! Please check user details and try again." });
            }
             

            if (!await _roleManager.RoleExistsAsync(UserRoles.User))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));
            }

            if (await _roleManager.RoleExistsAsync(UserRoles.User))
            {
                await _userManager.AddToRoleAsync(user, UserRoles.User);
            }
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public async Task<IActionResult> Logout (string logoutId)
        {
            await _signInManager.SignOutAsync();          
            return RedirectToAction("Index", "Home");
        }

        private JwtSecurityToken GetToken(List<Claim> authClaimes)
        {
            var date_time = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                expires: date_time.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                claims: authClaimes,
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            
            return token;
        }
    }
}
