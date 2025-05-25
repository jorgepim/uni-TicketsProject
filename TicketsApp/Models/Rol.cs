using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class Rol
    {
        [Key]
        public int RolId { get; set; }
        
        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(20)]
        [Display(Name = "Nombre")]
        public string? NombreRol { get; set; }

        [ValidateNever]
        public ICollection<Usuario>? Usuarios { get; set; }
    }
}
