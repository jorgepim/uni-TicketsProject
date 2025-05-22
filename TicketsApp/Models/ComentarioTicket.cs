using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class ComentarioTicket
    {
        [Key]
        public int ComentarioId { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public string Comentario { get; set; }

        public DateTime FechaComentario { get; set; } = DateTime.Now;
    }
}
