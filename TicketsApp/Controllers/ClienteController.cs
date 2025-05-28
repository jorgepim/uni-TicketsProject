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
        private readonly EmailService _emailService;

        public ClienteController(ApplicationDbContext context, EmailService emailService)
        {

            _context = context;
            _emailService = emailService;
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

               .Include(t => t.ComentariosTicket)
                .ThenInclude(c => c.Usuario)
        .ThenInclude(u => u.Rol)


                .Include(t => t.ComentariosTicket)
                    .ThenInclude(c => c.Usuario)
                    
                .FirstOrDefaultAsync(t => t.TicketId == id);

            if (ticket == null)
                return NotFound();

            ViewBag.UsuarioActualId = int.Parse(User.FindFirst("UsuarioId")?.Value ?? "0");
            ViewBag.UsuarioCreadorId = ticket.UsuarioCreadorId;

            return View(ticket);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAdjuntosYComentarios(int ticketId, List<IFormFile>? nuevosAdjuntos, string? nuevoComentario)
        {
            int usuarioId = int.Parse(User.FindFirst("UsuarioId")!.Value);

            var ticket = await _context.Tickets
                .Include(t => t.Adjunto)
                .Include(t => t.ComentariosTicket)
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);

            if (ticket == null)
                return NotFound("Ticket no encontrado.");

            bool hayCambios = false;

            // Insertar nuevo comentario
            if (!string.IsNullOrWhiteSpace(nuevoComentario))
            {
                var nuevo = new ComentariosTicket
                {
                    TicketId = ticketId,
                    UsuarioId = usuarioId,
                    Comentario = nuevoComentario,
                    FechaComentario = DateTime.Now
                };

                _context.ComentariosTicket.Add(nuevo);
                hayCambios = true;
               
                string cuerpoCorreoComentario = _emailService.GenerarCuerpoCorreoComentario(ticket.TicketId.ToString(), ticket.Titulo, nuevoComentario, "Creador del Ticket");
                string emailCreador = await _context.Usuarios
                    .Where(u => u.UsuarioId == ticket.UsuarioCreadorId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();
                await _emailService.SendEmailAsync(emailCreador, "Nuevo Comentario en el Ticket", cuerpoCorreoComentario);
                var asignacion = await _context.Asignaciones
       .FirstOrDefaultAsync(a => a.TicketId == ticket.TicketId);

                Usuario? usuarioAsignado = null;
                if (asignacion != null)
                {
                    usuarioAsignado = await _context.Usuarios.FindAsync(asignacion.UsuarioAsignadoId);
                    if (usuarioAsignado != null)
                    {
                        await _emailService.SendEmailAsync(usuarioAsignado.Email, "Nuevo Comentario en el Ticket", cuerpoCorreoComentario);
                    }
                }
            }

            // Adjuntar archivos
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

                var cuerpoCorreoAdjunto = _emailService.GenerarCuerpoCorreoAdjunto(ticket.TicketId.ToString(), ticket.Titulo, "Archivo Adjunto", "Creador del Ticket");
                string emailCreador = await _context.Usuarios
                    .Where(u => u.UsuarioId == ticket.UsuarioCreadorId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();
                await _emailService.SendEmailAsync(emailCreador, "Nuevo Archivo Adjunto en el Ticket", cuerpoCorreoAdjunto);

                // Enviar correo al técnico asignado
                var asignacion = await _context.Asignaciones
       .FirstOrDefaultAsync(a => a.TicketId == ticket.TicketId);

                Usuario? usuarioAsignado = null;
                if (asignacion != null)
                {
                    usuarioAsignado = await _context.Usuarios.FindAsync(asignacion.UsuarioAsignadoId);
                    if (usuarioAsignado != null)
                    {
                        await _emailService.SendEmailAsync(usuarioAsignado.Email, "Nuevo Archivo Adjunto en el Ticket", cuerpoCorreoAdjunto);
                    }
                }
            }

            if (!hayCambios)

            {
                return BadRequest("Debe agregar al menos un comentario o archivo.");
            }


            //ticket.EstadoId = estadoAbiertoId;



                return BadRequest("Debe ingresar al menos un comentario o archivo adjunto.");

            // Crear notificación

            var notificacion = new Notificacion
            {
                UsuarioId = usuarioId,
                TicketId = ticketId,

                Mensaje = $"Se agregaron nuevos comentarios y/o archivos.",

                Mensaje = "Se agregó un nuevo comentario o archivo al ticket.",

                FechaEnvio = DateTime.Now,
                Leido = false
            };

            _context.Notificaciones.Add(notificacion);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detalles", new { id = ticketId });
        }


    }
}
