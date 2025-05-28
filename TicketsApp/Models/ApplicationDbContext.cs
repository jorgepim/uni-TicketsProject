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
        public DbSet<Adjunto> Adjunto { get; set; }
        public DbSet<Asignacion> Asignaciones { get; set; }

        // ✅ Nombre corregido
        public DbSet<ComentariosTicket> ComentariosTicket { get; set; }

        public DbSet<Notificacion> Notificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Clave compuesta para la tabla intermedia
            modelBuilder.Entity<UsuarioCategoria>()
                .HasKey(uc => new { uc.UsuarioId, uc.CategoriaId });

            modelBuilder.Entity<UsuarioCategoria>()
                .HasOne(uc => uc.Usuario)
                .WithMany()
                .HasForeignKey(uc => uc.UsuarioId);

            modelBuilder.Entity<UsuarioCategoria>()
                .HasOne(uc => uc.Categoria)
                .WithMany()
                .HasForeignKey(uc => uc.CategoriaId);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Estado)
                .HasDefaultValue(true);

            modelBuilder.Entity<Notificacion>()
                .Property(n => n.Leido)
                .HasDefaultValue(false);

            // ✅ Relación Ticket ⇄ ComentarioTicket
            modelBuilder.Entity<ComentariosTicket>()
                .HasOne(ct => ct.Ticket)
                .WithMany(t => t.ComentariosTicket)
                .HasForeignKey(ct => ct.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
