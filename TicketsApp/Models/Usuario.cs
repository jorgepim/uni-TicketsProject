using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketsApp.Models
{
    public class Usuario
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }


        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        public string Apellido { get; set; }


        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(150)]
        public string Email { get; set; }


        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El formato debe ser ####-####")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required, MaxLength(20)]
        public string TipoUsuario { get; set; }

        public int RolId { get; set; }
        [ForeignKey("RolId")]
        public Rol? Rol { get; set; }

        [Required]
        public string ContrasenaHash { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public bool? Estado { get; set; } = true;
    }


}
