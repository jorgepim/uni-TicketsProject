using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class EmpresaExterna
    {
        [Key]
        public int EmpresaId { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        [Display(Name = "Nombre")]
        public string NombreEmpresa { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        [Display(Name = "Contacto Principal")]
        public string ContactoPrincipal { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(20)]
        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El formato debe ser ####-####")]
        [Display(Name = "Teléfono")]
        public string TelefonoEmpresa { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(200)]
        [Display(Name = "Dirección")]
        public string DireccionEmpresa { get; set; }


        [ValidateNever]
        public ICollection<ClienteExterno>? Clientes { get; set; }
    }
}
