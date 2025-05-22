using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class EmpresaExterna
    {
        [Key]
        public int EmpresaId { get; set; }

        [Required, MaxLength(100)]
        public string NombreEmpresa { get; set; }

        [MaxLength(100)]
        public string ContactoPrincipal { get; set; }

        [MaxLength(20)]
        public string TelefonoEmpresa { get; set; }

        [MaxLength(200)]
        public string DireccionEmpresa { get; set; }

        public ICollection<ClienteExterno> Clientes { get; set; }
    }
}
