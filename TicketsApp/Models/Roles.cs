using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Roles
    {
        public int RolId { get; set; }

        [Required, StringLength(50)]
        public string? NombreRol { get; set; }
    }
}
