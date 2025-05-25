using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Adjunto
    {
        [Key]
        public int AdjuntoId { get; set; }

        public int? TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        [MaxLength(255)]
        public string? NombreArchivo { get; set; }

        [MaxLength(500)]
        public string? RutaArchivo { get; set; }

        public DateTime? FechaSubida { get; set; } = DateTime.Now;
    }
}
