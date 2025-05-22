using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class EstadoTicket
    {
        [Key]
        public int EstadoId { get; set; }

        [Required, MaxLength(50)]
        public string NombreEstado { get; set; }
    }
}
