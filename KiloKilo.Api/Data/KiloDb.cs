using Microsoft.EntityFrameworkCore;

namespace KiloKilo.Api.Data;

public class KiloDb : DbContext
{
    public KiloDb(DbContextOptions<KiloDb> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<MarcheEntity> Marches => Set<MarcheEntity>();
    public DbSet<CourseEntity> Courses => Set<CourseEntity>();
    public DbSet<ArticleEntity> Articles => Set<ArticleEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserEntity>(e =>
        {
            e.HasIndex(u => u.Identifiant).IsUnique();
            e.HasIndex(u => u.Email);
            e.Property(u => u.NomComplet).HasMaxLength(120);
            e.Property(u => u.Identifiant).HasMaxLength(40);
        });

        b.Entity<MarcheEntity>(e =>
        {
            e.HasIndex(m => m.UserId);
            e.Property(m => m.Nom).HasMaxLength(80);
        });

        b.Entity<CourseEntity>(e =>
        {
            e.HasIndex(c => new { c.UserId, c.UpdatedAt });
            e.HasIndex(c => new { c.UserId, c.Date });
            e.HasMany(c => c.Articles).WithOne(a => a.Course).HasForeignKey(a => a.CourseId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ArticleEntity>(e => e.HasIndex(a => a.NomNormalise));
        b.Entity<RefreshTokenEntity>(e => e.HasIndex(t => t.TokenHash).IsUnique());
    }
}
