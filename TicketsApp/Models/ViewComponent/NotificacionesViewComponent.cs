// Carpeta: ViewComponents
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketsApp.Models;

namespace TicketsApp.ViewComponents
{
    public class NotificacionesViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificacionesViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int usuarioId)
        {
            var notificaciones = await _context.Notificaciones
                .Include(n => n.Ticket)
                .Where(n => n.UsuarioId == usuarioId && n.Leido == false)
                .OrderByDescending(n => n.FechaEnvio)
                .Take(5)
                .ToListAsync();

            return View(notificaciones);
        }
    }
}
