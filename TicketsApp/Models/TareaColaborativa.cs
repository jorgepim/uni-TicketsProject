using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class TareaColaborativa
    {
        [Key]
        public int TareaId { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public int AsignadorId { get; set; }
        public Usuario Asignador { get; set; }

        public int UsuarioDestinoId { get; set; }
        public Usuario UsuarioDestino { get; set; }

        [MaxLength(500)]
        public string Descripcion { get; set; }

        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string Estado { get; set; }
    }
}
