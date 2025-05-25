using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketsApp.Models;
using TicketsApp.Models.ViewModels;

namespace TicketsApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("RedirigirPorRol");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            //string hash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            //// Mostrarlo en pantalla para copiarlo
            //ViewBag.Hash = hash;
            //return View(model);

            var user = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.ContrasenaHash))
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
                return View(model);
            }

            // Crear claims de autenticación
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Nombre ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Rol?.NombreRol ?? "Invitado"),
                new Claim("UsuarioId", user.UsuarioId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("RedirigirPorRol");
        }

        [HttpGet]
        public IActionResult RedirigirPorRol()
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;

            return rol switch
            {
                "Administrador" => RedirectToAction("Index", "Administrador"),
                "Técnico" => RedirectToAction("Index", "Tecnico"),
                "Cliente" => RedirectToAction("Index", "Cliente"),
                _ => RedirectToAction("Login")
            };
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccesoDenegado() => RedirectToAction("RedirigirPorRol");
    }
}
