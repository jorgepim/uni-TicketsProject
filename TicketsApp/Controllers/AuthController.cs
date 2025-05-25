using Microsoft.AspNetCore.Mvc;
using TicketsApp.Models.ViewModels;
using TicketsApp.Models;

namespace TicketsApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Verificar si ya existe el correo
            if (_context.Usuarios.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Este correo ya está registrado.");
                return View(model);
            }

            // Crear el nuevo usuario
            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Email = model.Email,
                Telefono = model.Telefono,
                TipoUsuario = "Externo",
                RolId = 3, // Asegúrate que el RolId 3 sea el de Cliente
                ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FechaRegistro = DateTime.Now,
                Estado = true
            };

            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }
    }
}
