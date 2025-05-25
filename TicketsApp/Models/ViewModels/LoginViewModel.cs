using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Correo requerido")]
        [EmailAddress(ErrorMessage = "Correo inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contraseña requerida")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
