using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Notificacion
    {
        [Key]
        public int NotificacionId { get; set; }

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        [MaxLength(255)]
        public string Mensaje { get; set; }

        public DateTime FechaEnvio { get; set; } = DateTime.Now;

        public bool Leido { get; set; } = false;
    }
}
