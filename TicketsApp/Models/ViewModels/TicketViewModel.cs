namespace TicketsApp.Models.ViewModels
{
    public class TicketViewModel
    {
        public int? TicketId { get; set; }

        public string? Titulo { get; set; } = string.Empty;

        public string? Prioridad { get; set; } = string.Empty;

        public string? Estado { get; set; } = string.Empty;

        public DateTime? FechaCreacion { get; set; }
    }
}
