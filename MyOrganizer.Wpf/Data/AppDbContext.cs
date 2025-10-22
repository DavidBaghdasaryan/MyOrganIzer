using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Entities.Languages;
using MyOrganizer.Wpf.Entities.Procedures;

namespace MyOrganizer.Wpf.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Tooth> Teeth => Set<Tooth>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Technic> Technics => Set<Technic>();
    public DbSet<ToothWork> ToothWorks => Set<ToothWork>();
    public DbSet<L10nKey> L10nKeys => Set<L10nKey>();
    public DbSet<L10nValue> L10nValues => Set<L10nValue>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<ProcedurePrice> ProcedurePrices => Set<ProcedurePrice>();
    public DbSet<Procedure> Procedures => Set<Procedure>();


    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Client
        b.Entity<Client>(e =>
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
        b.Entity<Tooth>(e =>
        {
            e.ToTable("Teeth");
            e.HasKey(x => x.Id);

            e.Property(x => x.ToothNumber).HasMaxLength(10);

            // If you want index for quick lookups:
            e.HasIndex(x => new { x.ClientId, x.ToothNumber }).HasDatabaseName("IX_Teeth_Client_ToothNumber");
        });

        // Product
        b.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.Id);

            e.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
            e.Property(x => x.Value).IsRequired().HasMaxLength(50);
        });

        b.Entity<Technic>(e =>
        {
            e.ToTable("Technics");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
        });
        b.Entity<ToothWork>(e =>
        {
            e.ToTable("ToothWorks");
            e.HasKey(x => x.Id);

        });

        b.Entity<L10nKey>(b =>
        {
            b.HasIndex(x => x.Key).IsUnique();
            b.Property(x => x.Key).HasMaxLength(200).IsRequired();
        });

        b.Entity<L10nValue>(b =>
        {
            b.HasKey(x => new { x.KeyId, x.Lang });
            b.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            b.HasOne(x => x.Key)
             .WithMany(k => k.Values)
             .HasForeignKey(x => x.KeyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Procedure>(e =>
        {
            e.ToTable("Procedures");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.IsActive).HasDefaultValue(true);

            // 1 -> many
            e.HasMany(p => p.Prices)
             .WithOne(pp => pp.Procedure)
             .HasForeignKey(pp => pp.ProcedureId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProcedurePrice>(e =>
        {
            e.ToTable("ProcedurePrices");
            e.HasKey(x => x.Id);

            // precision for money
            e.Property(x => x.Tier1).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tier2).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tier3).HasColumnType("decimal(18,2)");
            e.Property(x => x.Currency).HasMaxLength(10);
            e.HasIndex(x => x.ProcedureId);

        });


        b.Entity<Procedure>().HasData(
            new Procedure { Id = 1, Name = "Removable Partial Denture (Metal Framework)", IsActive = true },
            new Procedure { Id = 2, Name = "Full Denture", IsActive = true },
            new Procedure { Id = 3, Name = "Implant with Zirconia Crown", IsActive = true },
            new Procedure { Id = 4, Name = "Implant with Metal-Ceramic Crown", IsActive = true },
            new Procedure { Id = 5, Name = "Zirconia or E-max Crown", IsActive = true },
            new Procedure { Id = 6, Name = "Metal-Ceramic Crown", IsActive = true },
            new Procedure { Id = 7, Name = "Composite or Inlay Restoration", IsActive = true },
            new Procedure { Id = 8, Name = "Filling (Composite / Amalgam)", IsActive = true },
            new Procedure { Id = 9, Name = "Work Shift / Appointment Slot", IsActive = true },
            new Procedure { Id = 10, Name = "Endodontic Treatment (Root Canal)", IsActive = true }
        );

        b.Entity<ProcedurePrice>(e =>
        {
            e.ToTable("ProcedurePrices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.Tier1).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tier2).HasColumnType("decimal(18,2)");
            e.Property(x => x.Tier3).HasColumnType("decimal(18,2)");
        });
    }
}
