using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Usuarios
    {
     
        public int UsuarioId { get; set; }

        [Required, StringLength(100)]
        public string? Nombre { get; set; }

        [Required, StringLength(100)]
        public string? Apellido { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Telefono { get; set; }

        [Required, RegularExpression("Interno|Externo")]
        public string? TipoUsuario { get; set; }

        [Required, RegularExpression("Correo")]
        public string? MetodoAutenticacion { get; set; }

        public string? ContrasenaHash { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Required]
        public bool Estado { get; set; }
    }


}
