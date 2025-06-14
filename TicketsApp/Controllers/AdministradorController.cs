﻿using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using TicketsApp.Models;
using TicketsApp.Models.ViewModels;

namespace TicketsApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministradorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdministradorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Obtener datos directamente de los claims
            ViewBag.NombreCompleto = User.FindFirst("NombreCompleto")?.Value
                ?? $"{User.FindFirst(ClaimTypes.Name)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
            ViewBag.Email = User.FindFirst(ClaimTypes.Email)?.Value;
            ViewBag.Rol = User.FindFirst(ClaimTypes.Role)?.Value;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AgregarUsuario()
        {
            var model = new AgregarUsuarioViewModel();
            await CargarListasDesplegables(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarUsuario(AgregarUsuarioViewModel model)
        {
            // Validación personalizada: Admin no puede ser Externo
            if (model.RolId > 0)
            {
                var rol = await _context.Roles.FindAsync(model.RolId);
                if (rol != null && rol.NombreRol == "Admin" && model.TipoUsuario == "Externo")
                {
                    ModelState.AddModelError("TipoUsuario", "Un usuario Admin no puede ser de tipo Externo");
                }
            }

            // Validación: Si es Externo debe tener empresa
            if (model.TipoUsuario == "Externo" && !model.EmpresaId.HasValue)
            {
                ModelState.AddModelError("EmpresaId", "Debe seleccionar una empresa para usuarios externos");
            }

            // Validación: Debe tener al menos una categoría
            // Validación: Solo si es Interno y Técnico, debe seleccionar al menos una categoría
            if (model.TipoUsuario == "Interno")
            {
                var rol = await _context.Roles.FirstOrDefaultAsync(r => r.RolId == model.RolId);
                if (rol != null && rol.NombreRol == "Tecnico")
                {
                    if (model.CategoriasSeleccionadas == null || !model.CategoriasSeleccionadas.Any())
                    {
                        ModelState.AddModelError("CategoriasSeleccionadas", "Debe seleccionar al menos una categoría");
                    }
                }
            }


            // Verificar que el email no exista
            var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == model.Email);
            if (emailExiste)
            {
                ModelState.AddModelError("Email", "Ya existe un usuario con este email");
            }

            if (!ModelState.IsValid)
            {
                await CargarListasDesplegables(model);
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validar que los campos requeridos no estén null o vacíos
                if (string.IsNullOrWhiteSpace(model.Nombre) ||
                    string.IsNullOrWhiteSpace(model.Apellido) ||
                    string.IsNullOrWhiteSpace(model.Email) ||
                    string.IsNullOrWhiteSpace(model.Telefono) ||
                    string.IsNullOrWhiteSpace(model.TipoUsuario) ||
                    string.IsNullOrWhiteSpace(model.Contrasena))
                {
                    ModelState.AddModelError("", "Todos los campos son obligatorios");
                    await CargarListasDesplegables(model);
                    return View(model);
                }

                // 2. Verificar que el RolId sea válido (mayor que 0)
                if (model.RolId <= 0)
                {
                    ModelState.AddModelError("RolId", "Debe seleccionar un rol válido");
                    await CargarListasDesplegables(model);
                    return View(model);
                }

                Console.WriteLine($"Debug - Nombre: '{model.Nombre}'");
                Console.WriteLine($"Debug - Apellido: '{model.Apellido}'");
                Console.WriteLine($"Debug - Email: '{model.Email}'");
                Console.WriteLine($"Debug - Telefono: '{model.Telefono}'");
                Console.WriteLine($"Debug - TipoUsuario: '{model.TipoUsuario}'");
                Console.WriteLine($"Debug - RolId: {model.RolId}");
                Console.WriteLine($"Debug - Contrasena length: {model.Contrasena?.Length ?? 0}");

                // Verificar que el rol existe
                var rolSeleccionado = await _context.Roles.FindAsync(model.RolId);
                Console.WriteLine($"Debug - Rol encontrado: {rolSeleccionado?.NombreRol ?? "NULL"}");

                if (rolSeleccionado == null)
                {
                    ModelState.AddModelError("RolId", "El rol seleccionado no existe en la base de datos");
                    await CargarListasDesplegables(model);
                    return View(model);
                }


                // Verificar que el RolId existe
                var rolExiste = await _context.Roles.AnyAsync(r => r.RolId == model.RolId);
                if (!rolExiste)
                {
                    ModelState.AddModelError("RolId", "El rol seleccionado no existe");
                    await CargarListasDesplegables(model);
                    return View(model);
                }

                // Si es externo, verificar que la empresa existe
                if (model.TipoUsuario == "Externo" && model.EmpresaId.HasValue)
                {
                    var empresaExiste = await _context.EmpresasExternas.AnyAsync(e => e.EmpresaId == model.EmpresaId.Value);
                    if (!empresaExiste)
                    {
                        ModelState.AddModelError("EmpresaId", "La empresa seleccionada no existe");
                        await CargarListasDesplegables(model);
                        return View(model);
                    }
                }

                // Crear el usuario
                var usuario = new Usuario
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    Email = model.Email,
                    Telefono = model.Telefono,
                    TipoUsuario = model.TipoUsuario,
                    RolId = model.RolId,
                    ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(model.Contrasena),
                    FechaRegistro = DateTime.Now,
                    Estado = true
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Si es usuario externo, crear la relación con la empresa
                if (model.TipoUsuario == "Externo" && model.EmpresaId.HasValue)
                {
                    var clienteExterno = new ClienteExterno
                    {
                        UsuarioId = usuario.UsuarioId,
                        EmpresaId = model.EmpresaId.Value
                    };
                    _context.ClientesExternos.Add(clienteExterno);
                }


                // Agregar las categorías seleccionadas
                foreach (var categoriaId in model.CategoriasSeleccionadas)
                {
                    // Verificar que la categoría existe
                    var categoriaExiste = await _context.Categorias.AnyAsync(c => c.CategoriaId == categoriaId);
                    if (categoriaExiste)
                    {
                        var usuarioCategoria = new UsuarioCategoria
                        {
                            UsuarioId = usuario.UsuarioId,
                            CategoriaId = categoriaId
                        };
                        _context.UsuariosCategorias.Add(usuarioCategoria);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Usuario agregado exitosamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // Debugging mejorado
                var innerException = ex.InnerException?.Message ?? "No inner exception";
                var fullError = $"Error: {ex.Message} | Inner: {innerException}";

                // Log adicional para debugging
                Console.WriteLine($"Error completo: {ex}");

                ModelState.AddModelError("", $"Error al guardar: {fullError}");
                await CargarListasDesplegables(model);
                return View(model);
            }
        }

        private async Task CargarListasDesplegables(AgregarUsuarioViewModel model)
        {
            // Cargar roles
            var roles = await _context.Roles.ToListAsync();
            model.Roles = roles.Select(r => new SelectListItem
            {
                Value = r.RolId.ToString(),
                Text = r.NombreRol
            }).ToList();

            // Cargar empresas
            var empresas = await _context.EmpresasExternas.ToListAsync();
            model.Empresas = empresas.Select(e => new SelectListItem
            {
                Value = e.EmpresaId.ToString(),
                Text = e.NombreEmpresa
            }).ToList();

            // Cargar categorías
            model.TodasCategorias = await _context.Categorias.ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesByTipo(string tipoUsuario)
        {
            var roles = await _context.Roles.ToListAsync();

            // Filtrar Admin si es tipo Externo
            if (tipoUsuario == "Externo")
            {
                roles = roles.Where(r =>( r.NombreRol != "Administrador" && r.NombreRol != "Tecnico")).ToList();
            }else if (tipoUsuario == "Interno")
            {
                roles = roles.Where(r => r.NombreRol != "Cliente").ToList();
            }

                var result = roles.Select(r => new { value = r.RolId, text = r.NombreRol }).ToList();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> PanelAdministrador(string busqueda = "", string area = "")
        {
            var tecnicos = await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Rol.NombreRol == "Tecnico")
                .ToListAsync();

            var categorias = await _context.UsuariosCategorias
                .Include(uc => uc.Categoria)
                .ToListAsync();

            var asignaciones = await _context.Asignaciones
                .Include(a => a.Ticket)
                .ThenInclude(t => t.Estado)
                .ToListAsync();

            var lista = tecnicos.Select(t =>
            {
                var ticketsAsignados = asignaciones.Count(a => a.UsuarioAsignadoId == t.UsuarioId);
                var ticketsResueltos = asignaciones.Count(a =>
                    a.UsuarioAsignadoId == t.UsuarioId && a.Ticket?.Estado?.NombreEstado == "Resuelto");

                var areas = categorias
                    .Where(c => c.UsuarioId == t.UsuarioId)
                    .Select(c => c.Categoria?.Nombre ?? "")
                    .Distinct()
                    .ToList();

                return new TecnicoPanelViewModel
                {
                    UsuarioId = t.UsuarioId,
                    Nombre = t.Nombre!,
                    Apellido = t.Apellido!,
                    Email = t.Email!,
                    Areas = areas,
                    TicketsAsignados = ticketsAsignados,
                    TicketsResueltos = ticketsResueltos
                };
            }).ToList();

            // Filtro por búsqueda (nombre o apellido)
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                lista = lista.Where(t =>
                    t.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    t.Apellido.Contains(busqueda, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filtro por área
            if (!string.IsNullOrWhiteSpace(area) && area != "Todas")
            {
                lista = lista.Where(t => t.Areas.Contains(area)).ToList();
            }

            return View(lista);
        }

        [HttpGet]
        public async Task<IActionResult> PerfilTecnico(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.UsuarioId == id);

            if (usuario == null || usuario.Rol?.NombreRol != "Tecnico")
                return NotFound();

            var areas = await _context.UsuariosCategorias
                .Where(uc => uc.UsuarioId == id)
                .Select(uc => uc.Categoria.Nombre)
                .ToListAsync();

            var tickets = await _context.Asignaciones
                .Where(a => a.UsuarioAsignadoId == id)
                .Include(a => a.Ticket)
                .ThenInclude(t => t.Estado)
                .ToListAsync();

            var model = new TecnicoPerfilViewModel
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono,
                TipoUsuario = usuario.TipoUsuario,
                FechaRegistro = usuario.FechaRegistro ?? DateTime.Now,
                Areas = areas,
                Tickets = tickets.Select(t => new TicketResumenViewModel
                {
                    Titulo = t.Ticket?.Titulo,
                    Estado = t.Ticket?.Estado?.NombreEstado,
                    Prioridad = t.Ticket?.Prioridad,
                    FechaCreacion = t.Ticket?.FechaCreacion
                }).ToList()
            };

            return View(model);
        }

    }
}