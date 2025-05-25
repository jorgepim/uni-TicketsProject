using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Rol
    {
        [Key]
        public int RolId { get; set; }

        [Required, MaxLength(50)]
        public string? NombreRol { get; set; }

        public ICollection<Usuario>? Usuarios { get; set; }
    }
}
