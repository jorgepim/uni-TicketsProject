using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models.ViewModels
{
    public class EditarTicketAdminViewModel
    {
        public int TicketId { get; set; }

        [Required]
        [StringLength(100)]
        public string Titulo { get; set; } = string.Empty;

        public string? AplicacionAfectada { get; set; }

        public string? Descripcion { get; set; }

        [Required]
        public string Prioridad { get; set; } = string.Empty;

        [Required]
        public int EstadoId { get; set; }
        public string? NombreEstado { get; set; }

        public DateTime FechaCreacion { get; set; }

        public string NombreCreador { get; set; } = string.Empty;
        public string? NombreEmpresa { get; set; }

        public int? UsuarioAsignadoId { get; set; }
        public string? NombreUsuarioAsignado { get; set; }

        // Para agregar nuevo comentario
        [StringLength(1000)]
        public string? NuevoComentario { get; set; }

        // Para nuevos archivos adjuntos
        public List<IFormFile>? NuevosAdjuntos { get; set; }

        // Listas de datos relacionados
        public List<ComentarioAdminViewModel> Comentarios { get; set; } = new List<ComentarioAdminViewModel>();
        public List<AdjuntoAdminViewModel> Adjuntos { get; set; } = new List<AdjuntoAdminViewModel>();
    }
}
