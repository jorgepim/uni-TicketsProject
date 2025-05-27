using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using TicketsApp.Models;
using TicketsApp.Models.ViewModels;
using TicketsApp.Services;

namespace TicketsApp.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministradorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketGeneralController> _logger;
        private const int PageSize = 20;
        private readonly IFileService _fileService;

        public AdministradorController(ApplicationDbContext context, ILogger<TicketGeneralController> logger, IFileService fileService)
        {
            _context = context;
            _logger = logger;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index(int page = 1, string fechaFiltro = "", DateTime? fechaInicio = null,
    DateTime? fechaFin = null, int? categoriaId = null, int? estadoId = null, int? usuarioAsignadoId = null)
        {
            // Obtener datos directamente de los claims
            ViewBag.NombreCompleto = User.FindFirst("NombreCompleto")?.Value
                ?? $"{User.FindFirst(ClaimTypes.Name)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
            ViewBag.Email = User.FindFirst(ClaimTypes.Email)?.Value;
            ViewBag.Rol = User.FindFirst(ClaimTypes.Role)?.Value;

            // Obtener listas para los dropdowns de filtros
            ViewBag.Categorias = await _context.Categorias
                .Select(c => new { c.CategoriaId, c.Nombre })
                .ToListAsync();

            ViewBag.Estados = await _context.EstadosTicket
                .Select(e => new { e.EstadoId, e.NombreEstado })
                .ToListAsync();

            ViewBag.UsuariosAsignados = await _context.Usuarios
                .Where(u => u.TipoUsuario == "Interno") // Solo usuarios internos para asignación
                .Select(u => new { u.UsuarioId, NombreCompleto = $"{u.Nombre} {u.Apellido}" })
                .ToListAsync();

            // Pasar valores de filtros actuales a la vista
            ViewBag.FechaFiltro = fechaFiltro;
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");
            ViewBag.CategoriaId = categoriaId;
            ViewBag.EstadoId = estadoId;
            ViewBag.UsuarioAsignadoId = usuarioAsignadoId;

            // Query base de tickets
            var ticketsQuery = from t in _context.Tickets
                               join u in _context.Usuarios on t.UsuarioCreadorId equals u.UsuarioId
                               join c in _context.Categorias on t.CategoriaId equals c.CategoriaId
                               join e in _context.EstadosTicket on t.EstadoId equals e.EstadoId
                               join ce in _context.ClientesExternos on u.UsuarioId equals ce.UsuarioId into ceGroup
                               from ce in ceGroup.DefaultIfEmpty()
                               join emp in _context.EmpresasExternas on ce.EmpresaId equals emp.EmpresaId into empGroup
                               from emp in empGroup.DefaultIfEmpty()
                               join asig in _context.Asignaciones on t.TicketId equals asig.TicketId into asigGroup
                               from asig in asigGroup.DefaultIfEmpty()
                               join uAsig in _context.Usuarios on asig.UsuarioAsignadoId equals uAsig.UsuarioId into uAsigGroup
                               from uAsig in uAsigGroup.DefaultIfEmpty()
                               select new TicketAdminViewModel
                               {
                                   TicketId = t.TicketId,
                                   Titulo = t.Titulo,
                                   Descripcion = t.Descripcion,
                                   Prioridad = t.Prioridad,
                                   FechaCreacion = t.FechaCreacion ?? DateTime.MinValue,
                                   NombreCategoria = c.Nombre,
                                   NombreEstado = e.NombreEstado,
                                   NombreEmpresa = emp != null ? emp.NombreEmpresa : (u.TipoUsuario == "Interno" ? "Fix|Now" : "Fix|Now"),
                                   UsuarioAsignado = uAsig != null ? $"{uAsig.Nombre} {uAsig.Apellido}" : null,
                                   CategoriaId = c.CategoriaId,
                                   EstadoId = e.EstadoId,
                                   UsuarioAsignadoId = uAsig != null ? uAsig.UsuarioId : (int?)null
                               };

            // Aplicar filtros de fecha
            DateTime? fechaDesde = null;
            DateTime? fechaHasta = null;

            switch (fechaFiltro)
            {
                case "ultimos5":
                    fechaDesde = DateTime.Now.AddDays(-5).Date;
                    break;
                case "ultimos10":
                    fechaDesde = DateTime.Now.AddDays(-10).Date;
                    break;
                case "rango":
                    if (fechaInicio.HasValue)
                        fechaDesde = fechaInicio.Value.Date;
                    if (fechaFin.HasValue)
                        fechaHasta = fechaFin.Value.Date.AddDays(1).AddTicks(-1); // Incluir todo el día
                    break;
            }

            if (fechaDesde.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.FechaCreacion >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                ticketsQuery = ticketsQuery.Where(t => t.FechaCreacion <= fechaHasta.Value);
            }

            // Aplicar filtro por categoría
            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                ticketsQuery = ticketsQuery.Where(t => t.CategoriaId == categoriaId.Value);
            }

            // Aplicar filtro por estado
            if (estadoId.HasValue && estadoId.Value > 0)
            {
                ticketsQuery = ticketsQuery.Where(t => t.EstadoId == estadoId.Value);
            }

            // Aplicar filtro por usuario asignado
            if (usuarioAsignadoId.HasValue && usuarioAsignadoId.Value > 0)
            {
                ticketsQuery = ticketsQuery.Where(t => t.UsuarioAsignadoId == usuarioAsignadoId.Value);
            }

            // Ordenar por fecha de creación descendente
            ticketsQuery = ticketsQuery.OrderByDescending(t => t.FechaCreacion);

            // Contar total de tickets filtrados
            var totalTickets = await ticketsQuery.CountAsync();

            // Obtener tickets para la página actual
            var tickets = await ticketsQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Pasar datos de paginación a la vista
            ViewBag.CurrentPage = page;
            ViewBag.TotalTickets = totalTickets;

            return View(tickets);
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
                roles = roles.Where(r => (r.NombreRol != "Administrador" && r.NombreRol != "Tecnico")).ToList();
            }
            else if (tipoUsuario == "Interno")
            {
                roles = roles.Where(r => r.NombreRol != "Cliente").ToList();
            }

            var result = roles.Select(r => new { value = r.RolId, text = r.NombreRol }).ToList();
            return Json(result);
        }

        // Método GET para obtener usuarios disponibles según la categoría del ticket
        [HttpGet]
        public async Task<IActionResult> ObtenerUsuariosParaAsignacion(int ticketId)
        {
            try
            {
                // Obtener ticket con su categoría
                var ticket = await _context.Tickets
                    .Where(t => t.TicketId == ticketId)
                    .FirstOrDefaultAsync();

                if (ticket == null)
                {
                    return NotFound("Ticket no encontrado");
                }

                // Verificar si el ticket puede ser editado (no cerrado ni cancelado)
                var estadoTicket = await _context.EstadosTicket
                    .Where(e => e.EstadoId == ticket.EstadoId)
                    .FirstOrDefaultAsync();

                bool esEditable = estadoTicket?.NombreEstado != "Cerrado" &&
                                 estadoTicket?.NombreEstado != "Cancelado";

                if (!esEditable)
                {
                    return Json(new { success = false, message = "El ticket no puede ser reasignado porque está cerrado o cancelado" });
                }

                // Obtener todos los administradores activos
                var administradores = await (from u in _context.Usuarios
                                             join r in _context.Roles on u.RolId equals r.RolId
                                             where u.TipoUsuario == "Interno" &&
                                                   u.Estado == true &&
                                                   r.NombreRol == "Administrador"
                                             select new
                                             {
                                                 u.UsuarioId,
                                                 u.Nombre,
                                                 u.Apellido,
                                                 NombreRol = r.NombreRol,
                                                 Tipo = "Administrador"
                                             }).ToListAsync();

                // Obtener técnicos de la categoría específica del ticket
                var tecnicos = await (from u in _context.Usuarios
                                      join r in _context.Roles on u.RolId equals r.RolId
                                      join uc in _context.UsuariosCategorias on u.UsuarioId equals uc.UsuarioId
                                      where u.TipoUsuario == "Interno" &&
                                            u.Estado == true &&
                                            r.NombreRol == "Técnico" &&
                                            uc.CategoriaId == ticket.CategoriaId
                                      select new
                                      {
                                          u.UsuarioId,
                                          u.Nombre,
                                          u.Apellido,
                                          NombreRol = r.NombreRol,
                                          Tipo = "Técnico"
                                      }).ToListAsync();

                // Combinar administradores y técnicos
                var usuariosDisponibles = administradores.Concat(tecnicos)
                    .OrderBy(u => u.Tipo)
                    .ThenBy(u => u.Nombre)
                    .ToList();

                return Json(new
                {
                    success = true,
                    usuarios = usuariosDisponibles,
                    esEditable = esEditable
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios para asignación del ticket {TicketId}", ticketId);
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }


        // Método POST para asignar técnico o administrador al ticket
        [HttpPost]
        public async Task<IActionResult> AsignarTicket(int ticketId, int? usuarioAsignadoId, string? comentarioAsignacion)
        {
            try
            {
                // Validar que el ticket existe
                var ticket = await _context.Tickets
                    .Where(t => t.TicketId == ticketId)
                    .FirstOrDefaultAsync();

                if (ticket == null)
                {
                    return Json(new { success = false, message = "Ticket no encontrado" });
                }

                // Verificar si el ticket puede ser editado
                var estadoTicket = await _context.EstadosTicket
                    .Where(e => e.EstadoId == ticket.EstadoId)
                    .FirstOrDefaultAsync();

                bool esEditable = estadoTicket?.NombreEstado != "Cerrado" &&
                                 estadoTicket?.NombreEstado != "Cancelado";

                if (!esEditable)
                {
                    return Json(new { success = false, message = "No se puede asignar un ticket que está cerrado o cancelado" });
                }

                // Si se está asignando a alguien, validar que el usuario existe y está activo
                if (usuarioAsignadoId.HasValue)
                {
                    var usuarioAsignado = await _context.Usuarios
                        .Where(u => u.UsuarioId == usuarioAsignadoId.Value && u.Estado == true)
                        .FirstOrDefaultAsync();

                    if (usuarioAsignado == null)
                    {
                        return Json(new { success = false, message = "Usuario asignado no encontrado o inactivo" });
                    }

                    // Validar que si es técnico, pertenezca a la categoría del ticket
                    if (usuarioAsignado.TipoUsuario == "Interno")
                    {
                        var rol = await _context.Roles
                            .Where(r => r.RolId == usuarioAsignado.RolId)
                            .FirstOrDefaultAsync();

                        if (rol?.NombreRol == "Técnico")
                        {
                            var tieneCategoria = await _context.UsuariosCategorias
                                .AnyAsync(uc => uc.UsuarioId == usuarioAsignado.UsuarioId &&
                                               uc.CategoriaId == ticket.CategoriaId);

                            if (!tieneCategoria)
                            {
                                return Json(new { success = false, message = "El técnico no pertenece a la categoría de este ticket" });
                            }
                        }
                    }
                }

                // Verificar si ya existe una asignación para este ticket
                var asignacionExistente = await _context.Asignaciones
                    .Where(a => a.TicketId == ticketId)
                    .OrderByDescending(a => a.FechaAsignacion)
                    .FirstOrDefaultAsync();

                bool esPrimeraAsignacion = asignacionExistente == null;
                string mensajeAccion = "";

                if (asignacionExistente != null)
                {
                    // ACTUALIZAR asignación existente
                    asignacionExistente.UsuarioAsignadoId = usuarioAsignadoId;
                    asignacionExistente.FechaAsignacion = DateTime.Now;

                    // SOLO actualizar el comentario si se proporciona uno nuevo
                    // Si no se proporciona, conservar el comentario anterior
                    if (!string.IsNullOrWhiteSpace(comentarioAsignacion))
                    {
                        asignacionExistente.ComentarioAsignacion = comentarioAsignacion;
                    }
                    // Si comentarioAsignacion es null o vacío, NO tocar el campo ComentarioAsignacion
                    // para conservar el valor anterior

                    _context.Asignaciones.Update(asignacionExistente);
                    mensajeAccion = usuarioAsignadoId.HasValue ? "Ticket reasignado correctamente" : "Asignación removida correctamente";
                }
                else
                {
                    // CREAR nueva asignación (primera vez)
                    var nuevaAsignacion = new Asignacion
                    {
                        TicketId = ticketId,
                        UsuarioAsignadoId = usuarioAsignadoId,
                        FechaAsignacion = DateTime.Now,
                        ComentarioAsignacion = comentarioAsignacion // Aquí sí puede ser null en primera asignación
                    };

                    _context.Asignaciones.Add(nuevaAsignacion);
                    mensajeAccion = "Ticket asignado correctamente";

                    // Solo cambiar estado a 'Asignado' (EstadoId = 2) en la PRIMERA asignación
                    if (usuarioAsignadoId.HasValue)
                    {
                        ticket.EstadoId = 2; // Estado 'Asignado'
                        _context.Tickets.Update(ticket);
                    }
                }

                await _context.SaveChangesAsync();

                // Obtener información del usuario asignado para la respuesta
                string nombreUsuarioAsignado = "Sin asignar";
                if (usuarioAsignadoId.HasValue)
                {
                    var usuario = await _context.Usuarios
                        .Where(u => u.UsuarioId == usuarioAsignadoId.Value)
                        .FirstOrDefaultAsync();

                    if (usuario != null)
                    {
                        nombreUsuarioAsignado = $"{usuario.Nombre} {usuario.Apellido}";
                    }
                }

                return Json(new
                {
                    success = true,
                    message = mensajeAccion,
                    nombreUsuarioAsignado = nombreUsuarioAsignado,
                    esPrimeraAsignacion = esPrimeraAsignacion,
                    estadoCambiado = esPrimeraAsignacion && usuarioAsignadoId.HasValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar ticket {TicketId}", ticketId);
                return Json(new { success = false, message = "Error interno del servidor" });
            }
        }



        // Agregar esta acción al AdministradorController

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            // Consulta principal del ticket
            var ticket = await _context.Tickets
                .Where(t => t.TicketId == id)
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                return NotFound();
            }

            // Obtener usuario creador
            var usuarioCreador = await _context.Usuarios
                .Where(u => u.UsuarioId == ticket.UsuarioCreadorId)
                .FirstOrDefaultAsync();

            // Obtener empresa si es usuario externo
            string nombreEmpresa = "Fix.now";
            if (usuarioCreador?.TipoUsuario == "Externo")
            {
                var clienteExterno = await _context.ClientesExternos
                    .Where(ce => ce.UsuarioId == usuarioCreador.UsuarioId)
                    .FirstOrDefaultAsync();

                if (clienteExterno != null)
                {
                    var empresa = await _context.EmpresasExternas
                        .Where(e => e.EmpresaId == clienteExterno.EmpresaId)
                        .FirstOrDefaultAsync();
                    nombreEmpresa = empresa?.NombreEmpresa ?? "Sin empresa";
                }
            }

            // Obtener estado actual
            var estado = await _context.EstadosTicket
                .Where(e => e.EstadoId == ticket.EstadoId)
                .FirstOrDefaultAsync();

            // Verificar si el ticket es editable (no cerrado ni cancelado)
            bool esEditable = estado?.NombreEstado != "Cerrado" &&
                             estado?.NombreEstado != "Cancelado";

            // Obtener asignación actual (la más reciente)
            var asignacionActual = await _context.Asignaciones
                .Where(a => a.TicketId == ticket.TicketId)
                .OrderByDescending(a => a.FechaAsignacion)
                .FirstOrDefaultAsync();

            // Obtener usuario asignado si existe
            var usuarioAsignado = asignacionActual != null
                ? await _context.Usuarios.Where(u => u.UsuarioId == asignacionActual.UsuarioAsignadoId).FirstOrDefaultAsync()
                : null;

            Console.WriteLine($"Ticket ID: {ticket.TicketId}");
            Console.WriteLine($"Asignación actual: {asignacionActual?.UsuarioAsignadoId}");
            Console.WriteLine($"Usuario asignado: {usuarioAsignado?.UsuarioId} - {usuarioAsignado?.Nombre}");
            Console.WriteLine($"Es editable: {esEditable}");

            // Obtener comentarios con usuarios
            var comentariosQuery = from c in _context.ComentariosTicket
                                   join u in _context.Usuarios on c.UsuarioId equals u.UsuarioId
                                   join r in _context.Roles on u.RolId equals r.RolId into roleGroup
                                   from role in roleGroup.DefaultIfEmpty()
                                   where c.TicketId == ticket.TicketId
                                   orderby c.FechaComentario
                                   select new ComentarioAdminViewModel
                                   {
                                       ComentarioId = c.ComentarioId,
                                       Comentario = c.Comentario,
                                       FechaComentario = c.FechaComentario ?? DateTime.MinValue,
                                       NombreUsuario = u.Nombre + " " + u.Apellido,
                                       RolUsuario = role.NombreRol ?? "Sin rol"
                                   };

            var comentarios = await comentariosQuery.ToListAsync();

            // Obtener adjuntos
            var adjuntos = await _context.Adjunto
                .Where(a => a.TicketId == ticket.TicketId)
                .Select(a => new AdjuntoAdminViewModel
                {
                    AdjuntoId = a.AdjuntoId,
                    NombreArchivo = a.NombreArchivo,
                    FechaSubida = a.FechaSubida ?? DateTime.MinValue
                })
                .ToListAsync();

            // Obtener datos para los dropdowns
            ViewBag.Estados = await _context.EstadosTicket.ToListAsync();
            ViewBag.Prioridades = new List<string> { "Crítico", "Importante", "Baja" };
            ViewBag.EsEditable = esEditable;

            // Solo cargar usuarios para asignación si el ticket es editable
            if (esEditable)
            {
                // Obtener todos los administradores activos
                var administradores = await (from u in _context.Usuarios
                                             join r in _context.Roles on u.RolId equals r.RolId
                                             where u.TipoUsuario == "Interno" &&
                                                   u.Estado == true &&
                                                   r.NombreRol == "Administrador"
                                             select new
                                             {
                                                 u.UsuarioId,
                                                 u.Nombre,
                                                 u.Apellido,
                                                 NombreRol = r.NombreRol
                                             }).ToListAsync();

                // Obtener técnicos de la categoría específica del ticket
                var tecnicos = await (from u in _context.Usuarios
                                      join r in _context.Roles on u.RolId equals r.RolId
                                      join uc in _context.UsuariosCategorias on u.UsuarioId equals uc.UsuarioId
                                      where u.TipoUsuario == "Interno" &&
                                            u.Estado == true &&
                                            r.NombreRol == "Técnico" &&
                                            uc.CategoriaId == ticket.CategoriaId
                                      select new
                                      {
                                          u.UsuarioId,
                                          u.Nombre,
                                          u.Apellido,
                                          NombreRol = r.NombreRol
                                      }).ToListAsync();

                // Combinar administradores y técnicos, ordenados por rol y nombre
                ViewBag.UsuariosInternos = administradores.Concat(tecnicos)
                    .OrderBy(u => u.NombreRol) // Primero Administradores, después Técnicos
                    .ThenBy(u => u.Nombre)
                    .ToList();

                Console.WriteLine($"Administradores encontrados: {administradores.Count}");
                Console.WriteLine($"Técnicos de categoría {ticket.CategoriaId} encontrados: {tecnicos.Count}");
            }
            else
            {
                // Si no es editable, lista vacía
                ViewBag.UsuariosInternos = new List<object>();
                Console.WriteLine("Ticket no editable - no se cargan usuarios para asignación");
            }

            // Crear el modelo de vista
            var model = new EditarTicketAdminViewModel
            {
                TicketId = ticket.TicketId,
                Titulo = ticket.Titulo,
                AplicacionAfectada = ticket.AplicacionAfectada,
                Descripcion = ticket.Descripcion,
                Prioridad = ticket.Prioridad,
                EstadoId = ticket.EstadoId ?? 0, // Si es null, asigna 0
                NombreEstado = estado?.NombreEstado ?? "Sin estado",
                FechaCreacion = ticket.FechaCreacion ?? DateTime.MinValue,
                NombreCreador = usuarioCreador != null ? $"{usuarioCreador.Nombre} {usuarioCreador.Apellido}" : "Desconocido",
                NombreEmpresa = nombreEmpresa,
                UsuarioAsignadoId = asignacionActual?.UsuarioAsignadoId,
                NombreUsuarioAsignado = usuarioAsignado != null
                    ? $"{usuarioAsignado.Nombre} {usuarioAsignado.Apellido}"
                    : "Sin asignar",
                Comentarios = comentarios,
                Adjuntos = adjuntos
            };

            return View(model);
        }
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UsuarioId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // Método para actualizar el Estado del ticket
        [HttpPost]
        public async Task<IActionResult> ActualizarEstado(int ticketId, int estadoId)
        {
            try
            {
                var usuarioId = GetCurrentUserId();
                Console.WriteLine($"Debug - UsuarioId: {usuarioId}");
                var ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket == null)
                {
                    return Json(new { success = false, message = "Ticket no encontrado" });
                }

                // Verificar que el estado existe
                var estado = await _context.EstadosTicket.FindAsync(estadoId);
                if (estado == null)
                {
                    return Json(new { success = false, message = "Estado no válido" });
                }

                // Actualizar el estado
                ticket.EstadoId = estadoId;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Estado actualizado a '{estado.NombreEstado}' correctamente",
                    nuevoEstado = estado.NombreEstado
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar el estado: " + ex.Message });
            }
        }

        // Método para actualizar la Prioridad del ticket
        [HttpPost]
        public async Task<IActionResult> ActualizarPrioridad(int ticketId, string prioridad)
        {
            try
            {
                // Validar que la prioridad sea válida
                var prioridadesValidas = new List<string> { "Crítico", "Importante", "Baja" };
                if (!prioridadesValidas.Contains(prioridad))
                {
                    return Json(new { success = false, message = "Prioridad no válida" });
                }

                var ticket = await _context.Tickets.FindAsync(ticketId);
                if (ticket == null)
                {
                    return Json(new { success = false, message = "Ticket no encontrado" });
                }

                // Actualizar la prioridad
                ticket.Prioridad = prioridad;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Prioridad actualizada a '{prioridad}' correctamente",
                    nuevaPrioridad = prioridad
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al actualizar la prioridad: " + ex.Message });
            }
        }
        // Agregar este método en tu controlador
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarComentario(int ticketId, string comentario)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(comentario))
                {
                    return Json(new { success = false, message = "El comentario no puede estar vacío." });
                }

                var usuarioId = GetCurrentUserId();
                Console.WriteLine($"Debug - UsuarioId: {usuarioId}");

                if (usuarioId == 0)
                {
                    return Json(new { success = false, message = "Error de autenticación. Por favor, inicie sesión nuevamente." });
                }

                // Verificar que el ticket existe
                var ticketExiste = await _context.Tickets.AnyAsync(t => t.TicketId == ticketId);
                if (!ticketExiste)
                {
                    return Json(new { success = false, message = "El ticket no existe." });
                }

                // Crear el nuevo comentario
                var nuevoComentario = new ComentariosTicket
                {
                    TicketId = ticketId,
                    UsuarioId = usuarioId,
                    Comentario = comentario,
                    FechaComentario = DateTime.Now
                };

                _context.ComentariosTicket.Add(nuevoComentario);
                await _context.SaveChangesAsync();

                // Obtener la información del usuario para devolver en la respuesta
                var usuario = await _context.Usuarios
                    .Where(u => u.UsuarioId == usuarioId)
                    .FirstOrDefaultAsync();

                var rol = await _context.Roles
                    .Where(r => r.RolId == usuario.RolId)
                    .FirstOrDefaultAsync();

                var comentarioViewModel = new ComentarioAdminViewModel
                {
                    ComentarioId = nuevoComentario.ComentarioId,
                    Comentario = nuevoComentario.Comentario,
                    FechaComentario = nuevoComentario.FechaComentario ?? DateTime.Now,
                    NombreUsuario = $"{usuario.Nombre} {usuario.Apellido}",
                    RolUsuario = rol?.NombreRol ?? "Sin rol"
                };

                return Json(new
                {
                    success = true,
                    message = "Comentario agregado exitosamente.",
                    comentario = comentarioViewModel
                });
            }
            catch (Exception ex)
            {
                // Log del error si tienes un sistema de logging
                // _logger.LogError(ex, "Error al agregar comentario");

                return Json(new { success = false, message = "Error interno del servidor." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirAdjuntos(int ticketId, List<IFormFile> nuevosAdjuntos)
        {
            if (nuevosAdjuntos == null || !nuevosAdjuntos.Any())
            {
                return BadRequest(new { success = false, message = "No se recibieron archivos." });
            }

            try
            {
                var rutasArchivos = await _fileService.GuardarArchivosAsync(nuevosAdjuntos, ticketId);

                // 1. Preparamos una lista para guardar las nuevas entidades de Adjunto  
                var listaNuevosAdjuntos = new List<Adjunto>();

                foreach (var ruta in rutasArchivos)
                {
                    var adjunto = new Adjunto
                    {
                        TicketId = ticketId,
                        NombreArchivo = Path.GetFileName(ruta),
                        RutaArchivo = ruta,
                        FechaSubida = DateTime.Now
                    };
                    listaNuevosAdjuntos.Add(adjunto);
                }

                // 2. Agregamos todas las entidades al contexto de una vez  
                _context.Adjunto.AddRange(listaNuevosAdjuntos);

                // 3. Guardamos todos los cambios en la base de datos en UNA SOLA TRANSACCIÓN (MÉTODO EFICIENTE)  
                await _context.SaveChangesAsync();

                // 4. Ahora que ya se guardaron y tienen un ID, preparamos la respuesta JSON  
                var nuevosAdjuntosInfo = listaNuevosAdjuntos.Select(adj => new
                {
                    adjuntoId = adj.AdjuntoId,
                    nombreArchivo = adj.NombreArchivo,
                    fechaSubida = adj.FechaSubida?.ToString("dd/MM/yyyy HH:mm"), // Fixed CS1501 issue  
                    urlDescarga = Url.Action("DownloadFile", "Administrador", new { id = adj.AdjuntoId })
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Archivos subidos correctamente.",
                    nuevosArchivos = nuevosAdjuntosInfo
                });
            }
            catch (Exception) // Removed unused variable 'ex' to fix CS0168  
            {
                return StatusCode(500, new { success = false, message = "Ocurrió un error interno en el servidor." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var adjunto = await _context.Adjunto.FindAsync(id);

            if (adjunto == null)
            {
                return NotFound();
            }

            var rutaCompleta = _fileService.ObtenerRutaCompleta(adjunto.RutaArchivo);

            if (!System.IO.File.Exists(rutaCompleta))
            {
                return NotFound("El archivo no se encuentra en el servidor.");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(rutaCompleta, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            // "application/octet-stream" fuerza la descarga en el navegador
            return File(memory, "application/octet-stream", adjunto.NombreArchivo);
        }
        /* [HttpPost]
         public async Task<IActionResult> Editar(EditarTicketAdminViewModel model)
         {
             if (!ModelState.IsValid)
             {
                 // Recargar datos para los dropdowns si hay error
                 ViewBag.Estados = await _context.EstadosTicket.ToListAsync();
                 ViewBag.UsuariosInternos = await (from u in _context.Usuarios
                                                   join r in _context.Roles on u.RolId equals r.RolId into roleGroup
                                                   from role in roleGroup.DefaultIfEmpty()
                                                   where u.TipoUsuario == "Interno" && u.Estado == true
                                                   select new
                                                   {
                                                       u.UsuarioId,
                                                       u.Nombre,
                                                       u.Apellido,
                                                       NombreRol = role.NombreRol ?? "Sin rol"
                                                   }).ToListAsync();
                 ViewBag.Prioridades = new List<string> { "Crítico", "Importante", "Baja" };

                 return View(model);
             }

             using var transaction = await _context.Database.BeginTransactionAsync();
             try
             {
                 var ticket = await _context.Tickets
                     .Where(t => t.TicketId == model.TicketId)
                     .FirstOrDefaultAsync();

                 if (ticket == null)
                 {
                     return NotFound();
                 }

                 // Actualizar datos del ticket
                 ticket.Prioridad = model.Prioridad;
                 ticket.EstadoId = model.EstadoId;

                 // Manejar asignación si cambió
                 if (model.UsuarioAsignadoId.HasValue)
                 {
                     var asignacionActual = await _context.Asignaciones
                         .Where(a => a.TicketId == model.TicketId)
                         .OrderByDescending(a => a.FechaAsignacion)
                         .FirstOrDefaultAsync();

                     if (asignacionActual == null || asignacionActual.UsuarioAsignadoId != model.UsuarioAsignadoId.Value)
                     {
                         var nuevaAsignacion = new Asignacion
                         {
                             TicketId = model.TicketId,
                             UsuarioAsignadoId = model.UsuarioAsignadoId.Value,
                             FechaAsignacion = DateTime.Now,
                             ComentarioAsignacion = "Asignación actualizada desde edición"
                         };
                         _context.Asignaciones.Add(nuevaAsignacion);
                     }
                 }

                 // Agregar nuevo comentario si se proporcionó
                 if (!string.IsNullOrWhiteSpace(model.NuevoComentario))
                 {
                     var usuarioId = int.Parse(User.FindFirst("UsuarioId")?.Value ?? "0");
                     var comentario = new ComentariosTicket
                     {
                         TicketId = model.TicketId,
                         UsuarioId = usuarioId,
                         Comentario = model.NuevoComentario,
                         FechaComentario = DateTime.Now
                     };
                     _context.ComentariosTicket.Add(comentario);
                 }

                 // Procesar nuevos archivos adjuntos
                 if (model.NuevosAdjuntos != null && model.NuevosAdjuntos.Any())
                 {
                     var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
                     var rutasArchivos = await fileService.GuardarArchivosAsync(model.NuevosAdjuntos, ticket.TicketId);

                     foreach (var ruta in rutasArchivos)
                     {
                         var adjunto = new Adjunto
                         {
                             TicketId = ticket.TicketId,
                             NombreArchivo = Path.GetFileName(ruta),
                             RutaArchivo = ruta,
                             FechaSubida = DateTime.Now
                         };
                         _context.Adjunto.Add(adjunto);
                     }
                 }

                 await _context.SaveChangesAsync();
                 await transaction.CommitAsync();

                 TempData["SuccessMessage"] = $"Ticket TK-{model.TicketId} actualizado exitosamente.";
                 return RedirectToAction("Editar", new { id = model.TicketId });
             }
             catch (Exception ex)
             {
                 await transaction.RollbackAsync();
                 _logger.LogError(ex, "Error al actualizar ticket {TicketId}", model.TicketId);
                 ModelState.AddModelError("", "Ocurrió un error al actualizar el ticket.");

                 // Recargar datos para los dropdowns
                 ViewBag.Estados = await _context.EstadosTicket.ToListAsync();
                 ViewBag.UsuariosInternos = await (from u in _context.Usuarios
                                                   join r in _context.Roles on u.RolId equals r.RolId into roleGroup
                                                   from role in roleGroup.DefaultIfEmpty()
                                                   where u.TipoUsuario == "Interno" && u.Estado == true
                                                   select new
                                                   {
                                                       u.UsuarioId,
                                                       u.Nombre,
                                                       u.Apellido,
                                                       NombreRol = role.NombreRol ?? "Sin rol"
                                                   }).ToListAsync();
                 ViewBag.Prioridades = new List<string> { "Crítico", "Importante", "Baja" };

                 return View(model);
             }
         }
 */



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

        // end class
    }
}