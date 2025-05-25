using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        public int? UsuarioCreadorId { get; set; }
        [ForeignKey("UsuarioCreadorId")]
        public Usuario? UsuarioCreador { get; set; }

        public int? CategoriaId { get; set; }
        [ForeignKey("CategoriaId")]
        public Categoria? Categoria { get; set; }

        [MaxLength(100)]
        public string? Titulo { get; set; }

        [MaxLength(100)]
        public string? AplicacionAfectada { get; set; }

        public string? Descripcion { get; set; }

        [Required, MaxLength(20)]
        public string? Prioridad { get; set; }

        public int? EstadoId { get; set; }
        [ForeignKey("EstadoId")]
        public EstadoTicket? Estado { get; set; }

        public DateTime? FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; }
    }
}
