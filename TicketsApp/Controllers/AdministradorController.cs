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

        public async Task<IActionResult> Index(int page = 1)
        {
            // Obtener datos directamente de los claims
            ViewBag.NombreCompleto = User.FindFirst("NombreCompleto")?.Value
                ?? $"{User.FindFirst(ClaimTypes.Name)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
            ViewBag.Email = User.FindFirst(ClaimTypes.Email)?.Value;
            ViewBag.Rol = User.FindFirst(ClaimTypes.Role)?.Value;

            // Obtener tickets con paginación
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
                               orderby t.FechaCreacion descending
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
                                   UsuarioAsignado = uAsig != null ? $"{uAsig.Nombre} {uAsig.Apellido}" : null
                               };

            // Contar total de tickets
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
                roles = roles.Where(r =>( r.NombreRol != "Administrador" && r.NombreRol != "Tecnico")).ToList();
            }
            else if (tipoUsuario == "Interno")
            {
                roles = roles.Where(r => r.NombreRol != "Cliente").ToList();
            }

            var result = roles.Select(r => new { value = r.RolId, text = r.NombreRol }).ToList();
            return Json(result);
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
                                       //FechaComentario = c.FechaComentario,
                                       FechaComentario = c.FechaComentario ?? DateTime.MinValue,
                                       //  FechaCreacion = t.FechaCreacion ?? DateTime.MinValue,
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



        // end class
    }
}