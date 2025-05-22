using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketsApp.Models
{
    public class Usuario
    {


        [Key]
        public int UsuarioId { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; }

        [Required, MaxLength(100)]
        public string Apellido { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string Telefono { get; set; }

        [Required, MaxLength(20)]
        public string TipoUsuario { get; set; }

        public int RolId { get; set; }
        [ForeignKey("RolId")]
        public Rol Rol { get; set; }

        [Required]
        public string ContrasenaHash { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool Estado { get; set; } = true;
    }


}
