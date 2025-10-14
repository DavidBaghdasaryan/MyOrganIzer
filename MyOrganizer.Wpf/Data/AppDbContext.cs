using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Entities.Languages;

namespace MyOrganizer.Wpf.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Tooth> Teeth => Set<Tooth>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Tecno>  Tecnos => Set<Tecno>();
    public DbSet<ToothWork>   ToothWorks => Set<ToothWork>();
    public DbSet<L10nKey> L10nKeys => Set<L10nKey>();
    public DbSet<L10nValue> L10nValues => Set<L10nValue>();
    public DbSet<Language> Languages => Set<Language>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Client
        modelBuilder.Entity<Client>(e =>
        {
            e.ToTable("Clients");
            e.HasKey(x => x.Id);

            e.Property(x => x.FirstName).HasMaxLength(100);
            e.Property(x => x.LastName).HasMaxLength(100);
            e.Property(x => x.MidlName).HasMaxLength(100);
            e.Property(x => x.PhoneNumber).HasMaxLength(50);

            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.Debet).HasColumnType("decimal(18,2)");

            e.Property(x => x.DateJoin).HasDefaultValue(new DateTime(1900, 1, 1));
            e.Property(x => x.DateDobleJoin).HasDefaultValue(new DateTime(1900, 1, 1));

            e.HasMany(x => x.ClientTooths)
             .WithOne(t => t.Client!)
             .HasForeignKey(t => t.ClientId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Tooth
        modelBuilder.Entity<Tooth>(e =>
        {
            e.ToTable("Teeth");
            e.HasKey(x => x.Id);

            e.Property(x => x.ToothNumber).HasMaxLength(10);

            // If you want index for quick lookups:
            e.HasIndex(x => new { x.ClientId, x.ToothNumber }).HasDatabaseName("IX_Teeth_Client_ToothNumber");
        });

        // Product
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);

            e.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Value).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Tecno>(e =>
        {
            e.ToTable("Tecnos");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });
        modelBuilder.Entity<ToothWork>(e =>
        {
            e.ToTable("ToothWorks");
            e.HasKey(x => x.Id);

        });

        modelBuilder.Entity<L10nKey>(b =>
        {
            b.HasIndex(x => x.Key).IsUnique();
            b.Property(x => x.Key).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<L10nValue>(b =>
        {
            b.HasKey(x => new { x.KeyId, x.Lang });
            b.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            b.HasOne(x => x.Key)
             .WithMany(k => k.Values)
             .HasForeignKey(x => x.KeyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Language>(b =>
        {
            b.HasKey(x => x.Code);
            b.Property(x => x.Code).HasMaxLength(5);
            b.Property(x => x.Name).HasMaxLength(50);
        });
    }
}
