using Microsoft.EntityFrameworkCore;
using ProyectoInnovador.Models;

namespace ProyectoInnovador.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ArchivoOriginal> ArchivosOriginales => Set<ArchivoOriginal>();
    public DbSet<Fragmento> Fragmentos => Set<Fragmento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(100);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Email).HasMaxLength(150).HasDefaultValue("");
            entity.Property(u => u.Plan).IsRequired().HasMaxLength(20).HasDefaultValue("Free");
            entity.Property(u => u.EsAdmin).HasDefaultValue(false);
        });

        modelBuilder.Entity<ArchivoOriginal>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.PublicId).IsRequired();
            entity.HasIndex(a => a.PublicId).IsUnique();
            entity.Property(a => a.UsuarioId).IsRequired(false);
            entity.HasOne(a => a.Usuario)
                  .WithMany()
                  .HasForeignKey(a => a.UsuarioId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.Property(a => a.Nombre).IsRequired().HasMaxLength(255);
            entity.Property(a => a.HashSha256).IsRequired().HasMaxLength(64);
            entity.Property(a => a.SeedHash).IsRequired().HasMaxLength(64);
            entity.Property(a => a.Tamano).IsRequired();
        });

        modelBuilder.Entity<Fragmento>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.HashFragmento).IsRequired().HasMaxLength(128);
            entity.Property(f => f.CloudProvider).IsRequired().HasMaxLength(20);
            entity.Property(f => f.UrlRemota).IsRequired().HasMaxLength(500);

            entity.HasOne(f => f.ArchivoOriginal)
                .WithMany(a => a.Fragmentos)
                .HasForeignKey(f => f.ArchivoOriginalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasCheckConstraint("CK_Fragmento_CloudProvider", "CloudProvider IN ('AWS','Azure')");
        });
    }
}
