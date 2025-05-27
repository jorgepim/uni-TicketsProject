namespace TicketsApp.Models.ViewModels
{
    public class TecnicoPerfilViewModel
    {
        public int? UsuarioId { get; set; }
        public string? Nombre { get; set; } = "";
        public string? Apellido { get; set; } = "";
        public string? Email { get; set; } = "";
        public string? Telefono { get; set; } = "";
        public string? TipoUsuario { get; set; } = "";
        public DateTime? FechaRegistro { get; set; }

        public List<string>? Areas { get; set; } = new();
        public List<TicketResumenViewModel>? Tickets { get; set; } = new();
    }

    public class TicketResumenViewModel
    {
        public string? Titulo { get; set; }
        public string? Prioridad { get; set; }
        public string? Estado { get; set; }
        public DateTime? FechaCreacion { get; set; }
    }

}
