using Microsoft.EntityFrameworkCore;

namespace TicketsApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Rol> Roles { get; set; }
        public DbSet<EmpresaExterna> EmpresasExternas { get; set; }
        public DbSet<ClienteExterno> ClientesExternos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<UsuarioCategoria> UsuariosCategorias { get; set; }
        public DbSet<EstadoTicket> EstadosTicket { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<HistorialEstadoTicket> HistorialEstadosTicket { get; set; }
        public DbSet<Adjunto> Adjuntos { get; set; }
        public DbSet<Asignacion> Asignaciones { get; set; }
        public DbSet<TareaColaborativa> TareasColaborativas { get; set; }
        public DbSet<ComentarioTicket> ComentariosTicket { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Clave compuesta para la tabla intermedia
            modelBuilder.Entity<UsuarioCategoria>()
                .HasKey(uc => new { uc.UsuarioId, uc.CategoriaId });

            // Relaciones con Usuario y Categoria
            modelBuilder.Entity<UsuarioCategoria>()
                .HasOne(uc => uc.Usuario)
                .WithMany()
                .HasForeignKey(uc => uc.UsuarioId);

            modelBuilder.Entity<UsuarioCategoria>()
                .HasOne(uc => uc.Categoria)
                .WithMany()
                .HasForeignKey(uc => uc.CategoriaId);



            // Configuraciones adicionales si necesitás
            modelBuilder.Entity<Usuario>().Property(u => u.Estado).HasDefaultValue(true);
            modelBuilder.Entity<Notificacion>().Property(n => n.Leido).HasDefaultValue(false);
        }
    }
}
