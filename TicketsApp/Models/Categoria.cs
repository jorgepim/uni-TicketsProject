using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.Net.Sockets;

namespace TicketsApp.Models
{
    public class Categoria
    {
        [Key]
        public int CategoriaId { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(100)]
        public string? Nombre { get; set; }

        [Required(ErrorMessage = "Este campo es obligatorio")]
        [MaxLength(250)]
        [Display(Name = "Descripcción")]
        public string? Descripcion { get; set; }

        [ValidateNever]
        public ICollection<Ticket>? Tickets { get; set; }
    }
}
