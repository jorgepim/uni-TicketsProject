using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Asignacion
    {
        [Key]
        public int AsignacionId { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int UsuarioAsignadoId { get; set; }
        public Usuario UsuarioAsignado { get; set; }

        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public string ComentarioAsignacion { get; set; }
    }
}
