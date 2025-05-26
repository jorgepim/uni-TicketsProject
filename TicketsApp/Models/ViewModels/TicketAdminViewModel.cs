namespace TicketsApp.Models.ViewModels
{
    public class TicketAdminViewModel
    {
        public int TicketId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Prioridad { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public string NombreCategoria { get; set; } = string.Empty;
        public string NombreEstado { get; set; } = string.Empty;
        public string? NombreEmpresa { get; set; }
        public string? UsuarioAsignado { get; set; }
    }
}
