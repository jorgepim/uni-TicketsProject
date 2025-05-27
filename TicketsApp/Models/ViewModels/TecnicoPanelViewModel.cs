namespace TicketsApp.Models.ViewModels
{
    public class TecnicoPanelViewModel
    {
        public int? UsuarioId { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public List<string>? Areas { get; set; } = new();
        public int? TicketsAsignados { get; set; }
        public int? TicketsResueltos { get; set; }
        public int Rendimiento => TicketsAsignados == 0 ? 0 : (int)Math.Round((decimal)((double)TicketsResueltos / TicketsAsignados * 100));
    }
}
