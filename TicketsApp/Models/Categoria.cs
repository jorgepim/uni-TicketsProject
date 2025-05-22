using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace TicketsApp.Models
{
    public class Categoria
    {

        [Key]
        public int CategoriaId { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(255)]
        public string Descripcion { get; set; }

        public ICollection<Ticket> Tickets { get; set; }
    }
}
