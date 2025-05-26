using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TicketsApp.Models.ViewModels
{
    public class AgregarUsuarioViewModel
    {
        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(20)]
        [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "El formato debe ser ####-####")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "Seleccione un tipo de usuario")]
        [Display(Name = "Tipo de Usuario")]
        public string TipoUsuario { get; set; }

        [Required(ErrorMessage = "Seleccione un rol")]
        [Display(Name = "Rol")]
        public int RolId { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Contraseña")]
        [DataType(DataType.Password)]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "Confirme su contraseña")]
        [Compare("Contrasena", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar Contraseña")]
        [DataType(DataType.Password)]
        public string ConfirmarContrasena { get; set; }

        [Display(Name = "Empresa")]
        public int? EmpresaId { get; set; }

        [Display(Name = "Categorías")]
       
        public List<int> CategoriasSeleccionadas { get; set; } = new List<int>();

        // Propiedades para los dropdowns
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Empresas { get; set; } = new List<SelectListItem>();
        public List<Categoria> TodasCategorias { get; set; } = new List<Categoria>();
        public List<SelectListItem> TiposUsuario { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Interno", Text = "Interno" },
            new SelectListItem { Value = "Externo", Text = "Externo" }
        };
    }
}