namespace TicketsApp.Models.ViewModels
{
    public class ComentarioAdminViewModel
    {
        public int ComentarioId { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public DateTime FechaComentario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string RolUsuario { get; set; } = string.Empty;
    }
}
