using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models
{
    public class ClienteExterno
    {
        [Key, ForeignKey("Usuario")]
        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }

        public int EmpresaId { get; set; }
        [ForeignKey("EmpresaId")]
        public EmpresaExterna Empresa { get; set; }
    }
}
