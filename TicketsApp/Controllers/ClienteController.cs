using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketsApp.Models;
using TicketsApp.Services;

namespace TicketsApp.Controllers
{

    [Authorize(Roles = "Cliente")]
    
    public class ClienteController : Controller
    {

        private readonly ApplicationDbContext _context;

        public ClienteController(ApplicationDbContext context)
        {

            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult Logout()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerTickets()
        {
            var usuarioIdClaim = User.FindFirst("UsuarioId")?.Value;

            if (string.IsNullOrEmpty(usuarioIdClaim))
                return RedirectToAction("Login", "Auth");

            int usuarioId = int.Parse(usuarioIdClaim);

            var tickets = await _context.Tickets
                .Where(t => t.UsuarioCreadorId == usuarioId)
                .ToListAsync();

            return View(tickets);
        }


        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Estado)
                .Include(t => t.Adjunto)
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
                return NotFound();

            return View(ticket);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAdjuntosYComentarios(int ticketId, List<IFormFile>? nuevosAdjuntos, string? nuevoComentario)
        {
            int usuarioId = int.Parse(User.FindFirst("UsuarioId").Value);

            var ticket = await _context.Tickets
                .Include(t => t.Adjunto)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId && t.UsuarioCreadorId == usuarioId);

            if (ticket == null)
                return NotFound("Ticket no encontrado o no pertenece al usuario.");

            const int estadoEnEsperaId = 4; // Estado "En Espera"
            const int estadoAbiertoId = 1;  // Estado "Abierto"

            if (ticket.EstadoId != estadoEnEsperaId)
            {
                return BadRequest("Solo se pueden agregar archivos y comentarios a tickets en estado 'En Espera'.");
            }

            bool hayCambios = false;

            if (!string.IsNullOrWhiteSpace(nuevoComentario))
            {
                var comentario = new ComentariosTicket
                {
                    TicketId = ticketId,
                    UsuarioId = usuarioId,
                    Comentario = nuevoComentario,
                    FechaComentario = DateTime.Now
                };

                _context.ComentariosTicket.Add(comentario);
                hayCambios = true;
            }

            if (nuevosAdjuntos != null && nuevosAdjuntos.Any())
            {
                var fileService = HttpContext.RequestServices.GetRequiredService<IFileService>();
                var rutasArchivos = await fileService.GuardarArchivosAsync(nuevosAdjuntos, ticketId);

                foreach (var ruta in rutasArchivos)
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
                hayCambios = true;
            }

            if (!hayCambios)
            {
                return BadRequest("Debe agregar al menos un comentario o archivo.");
            }

            
            ticket.EstadoId = estadoAbiertoId;

            
            var notificacion = new Notificacion
            {
                UsuarioId = usuarioId,
                TicketId = ticketId,
                Mensaje = $"Se agregaron nuevos comentarios y/o archivos. El ticket volvió a estado 'Abierto'.",
                FechaEnvio = DateTime.Now,
                Leido = false
            };

            _context.Notificaciones.Add(notificacion);

            await _context.SaveChangesAsync();

            return RedirectToAction("Detalles", new { id = ticketId });
        }



    }
}
