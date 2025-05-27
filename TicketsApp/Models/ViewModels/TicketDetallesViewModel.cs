using System.ComponentModel.DataAnnotations;

namespace TicketsApp.Models.ViewModels
{
    public class TicketDetallesViewModel
    {
        public int TicketId { get; set; }

        public string Titulo { get; set; }

        public string AplicacionAfectada { get; set; }

        public string Descripcion { get; set; }

        public Categoria Categoria { get; set; }

        [Display(Name = "Estado")]
        public int EstadoId { get; set; }  // Para binding del select

        public EstadoTicket Estado { get; set; }

        public string Prioridad { get; set; }

        public DateTime? FechaCreacion { get; set; }

        public string NombreUsuarioCreador { get; set; }
        public string? NombreEmpresa { get; set; }

        public List<EstadoTicket> EstadosDisponibles { get; set; }  // Para el dropdown

        public List<ComentariosTicket> ComentariosTicket { get; set; }

        public List<Adjunto> Adjunto { get; set; }
    }
}
