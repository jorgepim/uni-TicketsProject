using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models.ViewModels
{
    public class CrearTicketViewModel
    {
        [Required(ErrorMessage = "La categoría es obligatoria")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(100, ErrorMessage = "El título no puede exceder los 100 caracteres")]
        [Display(Name = "Título")]
        public string Titulo { get; set; }

        [StringLength(100, ErrorMessage = "La aplicación afectada no puede exceder los 100 caracteres")]
        [Display(Name = "Aplicación Afectada")]
        [Required(ErrorMessage = "Aplicacion es obligatorio")]
        public string? AplicacionAfectada { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "La prioridad es obligatoria")]
        [Display(Name = "Prioridad")]
        public string Prioridad { get; set; }

        [Display(Name = "Comentario Inicial (Opcional)")]
        public string? ComentarioInicial { get; set; }

        // Nueva propiedad para los archivos adjuntos
        [Display(Name = "Archivos Adjuntos")]
        public List<IFormFile>? Adjuntos { get; set; }
    }
}