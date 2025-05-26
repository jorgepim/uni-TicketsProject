using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketsApp.Models;
using TicketsApp.Models.ViewModels;
using TicketsApp.Services;

namespace TicketsApp.Controllers
{
    [Authorize(Roles = "Tecnico")]
    public class TecnicoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TecnicoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UsuarioId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        public IActionResult Index()
        {
            ViewBag.NombreCompleto = User.FindFirst("NombreCompleto")?.Value
                ?? $"{User.FindFirst(ClaimTypes.Name)?.Value} {User.FindFirst(ClaimTypes.Surname)?.Value}".Trim();
            ViewBag.Email = User.FindFirst(ClaimTypes.Email)?.Value;
            ViewBag.Rol = User.FindFirst(ClaimTypes.Role)?.Value;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> TicketsAsignados()
        {
            int usuarioId = GetCurrentUserId();
            if (usuarioId == 0)
            {
                // Puedes devolver JSON con error o redirigir
                return Json(new { success = false, message = "Error de autenticación. Por favor, inicie sesión nuevamente." });
            }

            var estadosPermitidos = new[] { "Asignado", "En Progreso", "En Espera", "Resuelto" };

            var tickets = await _context.Asignaciones
                .Where(a => a.UsuarioAsignadoId == usuarioId)
                .Include(a => a.Ticket)
                    .ThenInclude(t => t.Estado)
                .Where(a => a.Ticket != null && a.Ticket.Estado != null && estadosPermitidos.Contains(a.Ticket.Estado.NombreEstado))
                .Select(a => a.Ticket)
                .ToListAsync();

            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> DetallesHistorial(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Estado)
                .Include(t => t.ComentariosTicket).ThenInclude(c => c.Usuario)
                .Include(t => t.Adjunto)
                .Include(t => t.UsuarioCreador)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                return NotFound();
            }

            // Obtener empresa si es externo
            string nombreEmpresa = "Fix.now";
            var usuarioCreador = ticket.UsuarioCreador;

            if (usuarioCreador?.TipoUsuario == "Externo")
            {
                var clienteExterno = await _context.ClientesExternos
                    .FirstOrDefaultAsync(ce => ce.UsuarioId == usuarioCreador.UsuarioId);

                if (clienteExterno != null)
                {
                    var empresa = await _context.EmpresasExternas
                        .FirstOrDefaultAsync(e => e.EmpresaId == clienteExterno.EmpresaId);

                    nombreEmpresa = empresa?.NombreEmpresa ?? "Sin empresa";
                }
            }

            ViewBag.NombreEmpresa = nombreEmpresa;

            return View(ticket);
        }


        [HttpGet]
        public async Task<IActionResult> TodosLosTickets()
        {
            int usuarioId = GetCurrentUserId();
            if (usuarioId == 0)
            {
                return Json(new { success = false, message = "Error de autenticación. Por favor, inicie sesión nuevamente." });
            }

            var tickets = await _context.Asignaciones
                .Where(a => a.UsuarioAsignadoId == usuarioId && a.Ticket.Estado.NombreEstado == "Cerrado")
                .Include(a => a.Ticket)
                    .ThenInclude(t => t.Estado)
                .Include(a => a.Ticket)
                    .ThenInclude(t => t.Categoria)
                .Select(a => a.Ticket)
                .ToListAsync();

            return View(tickets);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarAdjunto(int adjuntoId)
        {
            var adjunto = await _context.Adjunto
                .FirstOrDefaultAsync(a => a.AdjuntoId == adjuntoId);

            if (adjunto == null || string.IsNullOrEmpty(adjunto.RutaArchivo))
            {
                return NotFound("Archivo no encontrado.");
            }

            // Ruta física absoluta al archivo
            var rutaCompleta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", adjunto.RutaArchivo);

            if (!System.IO.File.Exists(rutaCompleta))
            {
                return NotFound("El archivo no existe en el servidor.");
            }

            var tipoContenido = "application/octet-stream";
            var nombreArchivo = adjunto.NombreArchivo ?? Path.GetFileName(rutaCompleta);
            var bytes = await System.IO.File.ReadAllBytesAsync(rutaCompleta);

            return File(bytes, tipoContenido, nombreArchivo);
        }

        [HttpGet]
        public async Task<IActionResult> TicketsCreados()
        {
            int usuarioId = GetCurrentUserId();
            if (usuarioId == 0)
            {
                return Json(new { success = false, message = "Error de autenticación. Por favor, inicie sesión nuevamente." });
            }

            var tickets = await _context.Tickets
                .Where(t => t.UsuarioCreadorId == usuarioId)
                .Include(t => t.Estado)
                .Include(t => t.Categoria)
                .ToListAsync();

            return View(tickets);
        }

        public async Task<IActionResult> Detalles(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Estado)
                .Include(t => t.ComentariosTicket).ThenInclude(c => c.Usuario)
                .Include(t => t.Adjunto)
                .Include(t => t.UsuarioCreador) // Incluir el creador
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                TempData["Error"] = "El ticket no fue encontrado.";
                return RedirectToAction("Index");
            }

            // Estados permitidos
            var estadosPermitidos = new[] { "En progreso", "En espera", "Resuelto" };

            // Filtrar los estados válidos
            var estados = await _context.EstadosTicket
                .Where(e => estadosPermitidos.Contains(e.NombreEstado))
                .ToListAsync();

            // Obtener nombre de empresa del creador
            string nombreEmpresa = "Fix.now";
            var usuarioCreador = ticket.UsuarioCreador;

            if (usuarioCreador?.TipoUsuario == "Externo")
            {
                var clienteExterno = await _context.ClientesExternos
                    .FirstOrDefaultAsync(ce => ce.UsuarioId == usuarioCreador.UsuarioId);

                if (clienteExterno != null)
                {
                    var empresa = await _context.EmpresasExternas
                        .FirstOrDefaultAsync(e => e.EmpresaId == clienteExterno.EmpresaId);

                    nombreEmpresa = empresa?.NombreEmpresa ?? "Sin empresa";
                }
            }

            var viewModel = new TicketDetallesViewModel
            {
                TicketId = ticket.TicketId,
                Titulo = ticket.Titulo,
                AplicacionAfectada = ticket.AplicacionAfectada,
                Descripcion = ticket.Descripcion,
                Categoria = ticket.Categoria,
                EstadoId = ticket.EstadoId ?? 0,
                Estado = ticket.Estado,
                Prioridad = ticket.Prioridad,
                FechaCreacion = ticket.FechaCreacion,
                EstadosDisponibles = estados,
                ComentariosTicket = ticket.ComentariosTicket.ToList(),
                Adjunto = ticket.Adjunto.ToList(),
                NombreUsuarioCreador = usuarioCreador?.Nombre,
                NombreEmpresa = nombreEmpresa
            };

            ViewBag.Estados = estados;

            return View(viewModel);
        }


        [HttpPost]
        public IActionResult CambiarEstado(int ticketId, int EstadoId)
        {
            // Buscar el ticket incluyendo las relaciones necesarias
            var ticket = _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Estado)
                .Include(t => t.ComentariosTicket).ThenInclude(c => c.Usuario)
                .Include(t => t.Adjunto)
                .FirstOrDefault(t => t.TicketId == ticketId);

            if (ticket == null)
            {
                TempData["Error"] = "El ticket no fue encontrado.";
                return RedirectToAction("Detalles", new { id = ticketId });
            }

            // Estados permitidos por nombre
            var estadosPermitidos = new[] { "En progreso", "En espera", "Resuelto" };

            // Buscar el estado seleccionado, solo si está permitido
            var estado = _context.EstadosTicket
                .FirstOrDefault(e => e.EstadoId == EstadoId && estadosPermitidos.Contains(e.NombreEstado));

            if (estado == null)
            {
                TempData["Error"] = "El estado seleccionado no es válido.";

                // Cargar solo los estados válidos
                var estados = _context.EstadosTicket
                    .Where(e => estadosPermitidos.Contains(e.NombreEstado))
                    .ToList();

                // Armar el view model manualmente
                var viewModel = new TicketDetallesViewModel
                {
                    TicketId = ticket.TicketId,
                    Titulo = ticket.Titulo,
                    AplicacionAfectada = ticket.AplicacionAfectada,
                    Descripcion = ticket.Descripcion,
                    Categoria = ticket.Categoria,
                    EstadoId = ticket.EstadoId ?? 0,
                    Estado = ticket.Estado,
                    Prioridad = ticket.Prioridad,
                    FechaCreacion = ticket.FechaCreacion,
                    EstadosDisponibles = estados,
                    ComentariosTicket = ticket.ComentariosTicket.ToList(),
                    Adjunto = ticket.Adjunto.ToList()
                };

                ViewBag.Estados = estados;
                return View("Detalles", viewModel);
            }

            // Cambiar el estado y guardar
            ticket.EstadoId = EstadoId;
            _context.SaveChanges();

            TempData["Success"] = "Estado actualizado correctamente.";
            return RedirectToAction("Detalles", new { id = ticketId });
        }


        [HttpPost]
        public async Task<IActionResult> AgregarComentario(int ticketId, string comentario)
        {
            var usuarioId = GetCurrentUserId();
            if (usuarioId == 0)
            {
                TempData["Error"] = "Usuario no autorizado.";
                return RedirectToAction("Detalles", new { id = ticketId });
            }

            var nuevoComentario = new ComentariosTicket
            {
                TicketId = ticketId,
                UsuarioId = usuarioId,
                Comentario = comentario,
                FechaComentario = DateTime.Now
            };

            _context.ComentariosTicket.Add(nuevoComentario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comentario agregado correctamente.";
            return RedirectToAction("Detalles", new { id = ticketId });
        }


        [HttpPost]
        public async Task<IActionResult> SubirAdjunto(int ticketId, List<IFormFile> archivos)
        {
            if (archivos == null || !archivos.Any())
            {
                TempData["Error"] = "No se seleccionó ningún archivo para subir.";
                return RedirectToAction("Detalles", new { id = ticketId });
            }

            var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
            var rutas = await fileService.GuardarArchivosAsync(archivos, ticketId);

            foreach (var ruta in rutas)
            {
                var adjunto = new Adjunto
                {
                    TicketId = ticketId,
                    NombreArchivo = Path.GetFileName(ruta),
                    RutaArchivo = ruta,
                    FechaSubida = DateTime.Now
                };
                _context.Adjunto.Add(adjunto);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Archivo(s) subido(s) correctamente.";
            return RedirectToAction("Detalles", new { id = ticketId });
        }

        [HttpPost]
        public IActionResult CerrarTicket(int ticketId)
        {
            var ticket = _context.Tickets.FirstOrDefault(t => t.TicketId == ticketId);
            if (ticket == null)
            {
                return RedirectToAction("TicketsAsignados");
            }

            var estadoCerrado = _context.EstadosTicket.FirstOrDefault(e => e.NombreEstado == "Cerrado");
            if (estadoCerrado == null)
            {
                return RedirectToAction("TicketsAsignados");
            }

            ticket.EstadoId = estadoCerrado.EstadoId;
            ticket.FechaCierre = DateTime.Now;

            _context.SaveChanges();

            return RedirectToAction("TicketsAsignados");
        }

        public async Task<IActionResult> DetalleCreados(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Estado)
                .Include(t => t.ComentariosTicket).ThenInclude(c => c.Usuario)
                .Include(t => t.Adjunto)
                .Include(t => t.UsuarioCreador) // Asegúrate de incluir al creador
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
            {
                TempData["Error"] = "El ticket no fue encontrado.";
                return RedirectToAction("Index");
            }

            string nombreEmpresa = "Fix.now";

            var usuarioCreador = ticket.UsuarioCreador;

            if (usuarioCreador?.TipoUsuario == "Externo")
            {
                var clienteExterno = await _context.ClientesExternos
                    .FirstOrDefaultAsync(ce => ce.UsuarioId == usuarioCreador.UsuarioId);

                if (clienteExterno != null)
                {
                    var empresa = await _context.EmpresasExternas
                        .FirstOrDefaultAsync(e => e.EmpresaId == clienteExterno.EmpresaId);

                    nombreEmpresa = empresa?.NombreEmpresa ?? "Sin empresa";
                }
            }

            var viewModel = new TicketDetallesViewModel
            {
                TicketId = ticket.TicketId,
                Titulo = ticket.Titulo,
                AplicacionAfectada = ticket.AplicacionAfectada,
                Descripcion = ticket.Descripcion,
                Categoria = ticket.Categoria,
                EstadoId = ticket.EstadoId ?? 0,
                Estado = ticket.Estado,
                Prioridad = ticket.Prioridad,
                FechaCreacion = ticket.FechaCreacion,
                ComentariosTicket = ticket.ComentariosTicket.ToList(),
                Adjunto = ticket.Adjunto.ToList(),
                NombreUsuarioCreador = usuarioCreador?.Nombre,
                NombreEmpresa = nombreEmpresa
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> AgregarComentario2(int ticketId, string comentario)
        {
            var usuarioId = GetCurrentUserId();
            if (usuarioId == 0)
            {
                TempData["Error"] = "Usuario no autorizado.";
                return RedirectToAction("DetalleCreados", new { id = ticketId });
            }

            var nuevoComentario = new ComentariosTicket
            {
                TicketId = ticketId,
                UsuarioId = usuarioId,
                Comentario = comentario,
                FechaComentario = DateTime.Now
            };

            _context.ComentariosTicket.Add(nuevoComentario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Comentario agregado correctamente.";
            return RedirectToAction("DetalleCreados", new { id = ticketId });
        }


        [HttpPost]
        public async Task<IActionResult> SubirAdjunto2(int ticketId, List<IFormFile> archivos)
        {
            if (archivos == null || !archivos.Any())
            {
                TempData["Error"] = "No se seleccionó ningún archivo para subir.";
                return RedirectToAction("DetalleCreados", new { id = ticketId });
            }

            var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
            var rutas = await fileService.GuardarArchivosAsync(archivos, ticketId);

            foreach (var ruta in rutas)
            {
                var adjunto = new Adjunto
                {
                    TicketId = ticketId,
                    NombreArchivo = Path.GetFileName(ruta),
                    RutaArchivo = ruta,
                    FechaSubida = DateTime.Now
                };
                _context.Adjunto.Add(adjunto);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Archivo(s) subido(s) correctamente.";
            return RedirectToAction("DetalleCreados", new { id = ticketId });
        }
    }
}
