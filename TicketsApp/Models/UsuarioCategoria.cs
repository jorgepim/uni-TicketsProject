using System.ComponentModel.DataAnnotations.Schema;

namespace TicketsApp.Models
{
    [Table("Usuarios_Categorias")]
    public class UsuarioCategoria
    {
        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        public int? CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
    }
}
