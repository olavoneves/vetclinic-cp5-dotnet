using Microsoft.EntityFrameworkCore;
using VetClinic.Domain.Entities;

namespace VetClinic.Infrastructure.Persistence;

public class VetClinicDbContext : DbContext
{
    public VetClinicDbContext(DbContextOptions<VetClinicDbContext> options) : base(options) { }

    public DbSet<Tutor> Tutores => Set<Tutor>();
    public DbSet<Pet> Pets => Set<Pet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tutor>(e =>
        {
            e.ToTable("Tutores");
            e.HasKey(t => t.Id);
            e.Property(t => t.Nome).HasMaxLength(100).IsRequired();
            e.Property(t => t.Email).HasMaxLength(150).IsRequired();
            e.Property(t => t.Cep).HasMaxLength(9).IsRequired();
            e.Property(t => t.Logradouro).HasMaxLength(150);
            e.Property(t => t.Bairro).HasMaxLength(100);
            e.Property(t => t.Cidade).HasMaxLength(100);
            e.Property(t => t.Uf).HasMaxLength(2);

            // OTIMIZACAO: índice único em Email (busca rápida + garante unicidade no banco).
            e.HasIndex(t => t.Email).IsUnique();

            e.HasMany(t => t.Pets)
             .WithOne(p => p.Tutor)
             .HasForeignKey(p => p.TutorId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pet>(e =>
        {
            e.ToTable("Pets");
            e.HasKey(p => p.Id);
            e.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            e.Property(p => p.Especie).HasMaxLength(50).IsRequired();
            e.Property(p => p.Raca).HasMaxLength(50);

            // OTIMIZACAO: índice na FK TutorId (filtro de pets por tutor e joins).
            e.HasIndex(p => p.TutorId);
        });
    }
}
