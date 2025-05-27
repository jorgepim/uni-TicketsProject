namespace TicketsApp.Models.ViewModels
{
    public class VerTicketsViewModel
    {
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public List<TicketViewModel>? Tickets { get; set; }
    }
}
