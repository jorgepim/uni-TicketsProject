using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class HistorialEstadoTicket
    {
        [Key]
        public int HistorialId { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int EstadoAnterior { get; set; }
        public int EstadoNuevo { get; set; }

        public int UsuarioCambioId { get; set; }
        public Usuario UsuarioCambio { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string Comentarios { get; set; }
    }
}
