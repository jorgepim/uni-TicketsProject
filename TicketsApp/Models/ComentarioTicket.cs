using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using TicketsApp.Models;

public class ComentariosTicket
{
    [Key]
    public int ComentarioId { get; set; }

    public int? TicketId { get; set; }
    [ForeignKey("TicketId")]
    public Ticket? Ticket { get; set; }

    public int? UsuarioId { get; set; }
    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }

    public string? Comentario { get; set; }
    public DateTime? FechaComentario { get; set; } = DateTime.Now;
}
