using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketsApp.Models;

public class NotificacionesController : Controller
{
    private readonly ApplicationDbContext _context;

    public NotificacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Ultimas()
    {
        var usuarioId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "UsuarioId")?.Value ?? "0");

        var notificaciones = await _context.Notificaciones
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaEnvio)
            .Take(5)
            .ToListAsync();

        return PartialView("_ListaNotificaciones", notificaciones);
    }
}
