using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketsApp.Models;
using TicketsApp.Models.ViewModels;
using System.Security.Claims;
using TicketsApp.Services;

namespace TicketsApp.Controllers
{
    public class TicketGeneralController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketGeneralController> _logger; 

        public TicketGeneralController(ApplicationDbContext context, ILogger<TicketGeneralController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Crear(string returnUrl = null)
        {
            ViewBag.Categorias = _context.Categorias.ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearTicketViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioId = GetCurrentUserId();

                    if (usuarioId == 0)
                    {
                        return Json(new { success = false, message = "Error de autenticación. Por favor, inicie sesión nuevamente." });
                    }

                    var ticket = new Ticket
                    {
                        UsuarioCreadorId = usuarioId,
                        CategoriaId = model.CategoriaId,
                        Titulo = model.Titulo,
                        AplicacionAfectada = model.AplicacionAfectada,
                        Descripcion = model.Descripcion,
                        Prioridad = model.Prioridad,
                        EstadoId = 1, // "Abierto"
                        FechaCreacion = DateTime.Now
                    };

                    _context.Tickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    // ⬇️ Crear notificación para el cliente (usuario que creó el ticket)
                    var notificacion = new Notificacion
                    {
                        UsuarioId = usuarioId,
                        TicketId = ticket.TicketId,
                        Mensaje = $"Nuevo Ticket: {ticket.Titulo}",
                        FechaEnvio = DateTime.Now,
                        Leido = false
                    };
                    _context.Notificaciones.Add(notificacion);
                    await _context.SaveChangesAsync();

                    // Agregar comentario si existe
                    if (!string.IsNullOrWhiteSpace(model.ComentarioInicial))
                    {
                        var comentario = new ComentariosTicket
                        {
                            TicketId = ticket.TicketId,
                            UsuarioId = usuarioId,
                            Comentario = model.ComentarioInicial,
                            FechaComentario = DateTime.Now
                        };
                        _context.ComentariosTicket.Add(comentario);
                        await _context.SaveChangesAsync();
                    }

                    // Procesar archivos adjuntos
                    if (model.Adjuntos != null && model.Adjuntos.Any())
                    {
                        var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
                        var rutasArchivos = await fileService.GuardarArchivosAsync(model.Adjuntos, ticket.TicketId);

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

                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value;
                    var successMessage = $"Ticket #{ticket.TicketId} creado exitosamente.";

                    string redirectUrl;

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        redirectUrl = returnUrl;
                    }
                    else
                    {
                        redirectUrl = rolUsuario switch
                        {
                            "Administrador" => Url.Action("Index", "Administrador"),
                            "Técnico" => Url.Action("Index", "Tecnico"),
                            "Cliente" => Url.Action("Index", "Cliente"),
                            _ => Url.Action("Login", "Auth")
                        };
                    }

                    return Json(new { success = true, message = successMessage, redirectUrl = redirectUrl });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al crear ticket con adjuntos");
                    return Json(new { success = false, message = "Ocurrió un error inesperado al crear el ticket." });
                }
            }

            ViewBag.Categorias = _context.Categorias.ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }


        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UsuarioId");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        [HttpGet]
        public async Task<IActionResult> DescargarAdjunto(int adjuntoId)
        {
            try
            {
                var adjunto = await _context.Adjunto.FindAsync(adjuntoId);

                if (adjunto == null)
                {
                    return NotFound("Archivo no encontrado");
                }

                var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
                var rutaCompleta = fileService.ObtenerRutaCompleta(adjunto.RutaArchivo);

                if (!System.IO.File.Exists(rutaCompleta))
                {
                    return NotFound("El archivo físico no existe");
                }

                var memoria = new MemoryStream();
                using (var stream = new FileStream(rutaCompleta, FileMode.Open))
                {
                    await stream.CopyToAsync(memoria);
                }
                memoria.Position = 0;

                var tipoContenido = GetContentType(adjunto.NombreArchivo);

                return File(memoria, tipoContenido, adjunto.NombreArchivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al descargar adjunto {adjuntoId}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }

        public IActionResult CrearTicketTecnico(string returnUrl = null)
        {
            ViewBag.Categorias = _context.Categorias.ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearTicketTecnico(CrearTicketViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var usuarioId = GetCurrentUserId();

                    if (usuarioId == 0)
                    {
                        return Json(new { success = false, message = "Error de autenticación. Por favor, inicie sesión nuevamente." });
                    }

                    var ticket = new Ticket
                    {
                        UsuarioCreadorId = usuarioId,
                        CategoriaId = model.CategoriaId,
                        Titulo = model.Titulo,
                        AplicacionAfectada = model.AplicacionAfectada,
                        Descripcion = model.Descripcion,
                        Prioridad = model.Prioridad,
                        EstadoId = 1, // "Abierto"
                        FechaCreacion = DateTime.Now
                    };

                    _context.Tickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    // Agregar comentario si existe
                    if (!string.IsNullOrWhiteSpace(model.ComentarioInicial))
                    {
                        var comentario = new ComentariosTicket
                        {
                            TicketId = ticket.TicketId,
                            UsuarioId = usuarioId,
                            Comentario = model.ComentarioInicial,
                            FechaComentario = DateTime.Now
                        };
                        _context.ComentariosTicket.Add(comentario);
                        await _context.SaveChangesAsync();
                    }

                    // Procesar archivos adjuntos
                    if (model.Adjuntos != null && model.Adjuntos.Any())
                    {
                        var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
                        var rutasArchivos = await fileService.GuardarArchivosAsync(model.Adjuntos, ticket.TicketId);

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

                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    var successMessage = $"Ticket #{ticket.TicketId} creado exitosamente.";

                    string redirectUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                        ? returnUrl
                        : Url.Action("Index", "Home");

                    return Json(new { success = true, message = successMessage, redirectUrl = redirectUrl });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al crear ticket con adjuntos");
                    return Json(new { success = false, message = "Ocurrió un error inesperado al crear el ticket." });
                }
            }

            ViewBag.Categorias = _context.Categorias.ToList();
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

    }
}