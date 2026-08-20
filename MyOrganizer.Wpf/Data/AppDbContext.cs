using System;
using Microsoft.EntityFrameworkCore;
using MyOrganizer.Wpf.Data.Entities;
using MyOrganizer.Wpf.Entities;
using MyOrganizer.Wpf.Entities.Languages;
using MyOrganizer.Wpf.Entities.Procedures;

namespace MyOrganizer.Wpf.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Tooth> Teeth => Set<Tooth>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Technic> Technics => Set<Technic>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierOffering> SupplierOfferings => Set<SupplierOffering>();
        public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();
        public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<ExpenseLine> ExpenseLines => Set<ExpenseLine>();
        public DbSet<ToothWork> ToothWorks => Set<ToothWork>();
        public DbSet<L10nKey> L10nKeys => Set<L10nKey>();
        public DbSet<L10nValue> L10nValues => Set<L10nValue>();
        public DbSet<Language> Languages => Set<Language>();
        public DbSet<Procedure> Procedures => Set<Procedure>();
        public DbSet<ProcedurePrice> ProcedurePrices => Set<ProcedurePrice>();

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

                // dates
                e.Property(x => x.DateJoin).HasDefaultValue(new DateTime(1900, 1, 1));
                e.Property(x => x.DateDobleJoin).HasDefaultValue(new DateTime(1900, 1, 1));

                // money: use precision (provider-agnostic)
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.Property(x => x.Debet).HasPrecision(18, 2);

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
                e.HasIndex(x => new { x.ClientId, x.ToothNumber })
                 .HasDatabaseName("IX_Teeth_Client_ToothNumber");
            });

            // Product
            b.Entity<Product>(e =>
            {
                e.ToTable("Products");
                e.HasKey(x => x.Id);
                e.Property(x => x.ProductName).IsRequired().HasMaxLength(200);
                e.Property(x => x.Value).IsRequired().HasMaxLength(50);
            });

            // Technic (legacy ledger — kept until a later cleanup migration)
            b.Entity<Technic>(e =>
            {
                e.ToTable("Technics");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);

                // Was nvarchar(max). Use a big but finite length to avoid "(max)"
                e.Property(x => x.Type).IsRequired().HasMaxLength(2000);
            });

            b.Entity<UnitOfMeasure>(e =>
            {
                e.ToTable("UnitsOfMeasure");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(80);
                e.Property(x => x.ConversionFactor).HasPrecision(18, 6);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => x.Name).IsUnique();
                e.HasOne(x => x.BaseUnit)
                    .WithMany()
                    .HasForeignKey(x => x.BaseUnitId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<CatalogItem>(e =>
            {
                e.ToTable("CatalogItems");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.Notes).HasMaxLength(2000);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => x.Name);
                e.HasOne(x => x.UnitOfMeasure)
                    .WithMany()
                    .HasForeignKey(x => x.UnitOfMeasureId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<Supplier>(e =>
            {
                e.ToTable("Suppliers");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.Email).HasMaxLength(200);
                e.Property(x => x.Phone).HasMaxLength(50);
                e.Property(x => x.Notes).HasMaxLength(2000);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => x.Name);
            });

            b.Entity<SupplierOffering>(e =>
            {
                e.ToTable("SupplierOfferings");
                e.HasKey(x => x.Id);
                e.Property(x => x.Notes).HasMaxLength(2000);
                e.Property(x => x.Sku).HasMaxLength(80);
                e.Property(x => x.SupplierPrice).HasPrecision(18, 2);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => new { x.SupplierId, x.CatalogItemId }).IsUnique();
                e.HasOne(x => x.Supplier)
                    .WithMany(s => s.Offerings)
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.CatalogItem)
                    .WithMany(c => c.SupplierOfferings)
                    .HasForeignKey(x => x.CatalogItemId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<Expense>(e =>
            {
                e.ToTable("Expenses");
                e.HasKey(x => x.Id);
                e.Property(x => x.Reference).HasMaxLength(80);
                e.Property(x => x.Notes).HasMaxLength(2000);
                e.Property(x => x.TotalAmount).HasPrecision(18, 2);
                e.HasIndex(x => x.SupplierId);
                e.HasIndex(x => x.Date);
                e.HasOne(x => x.Supplier)
                    .WithMany(s => s.Expenses)
                    .HasForeignKey(x => x.SupplierId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            b.Entity<ExpenseLine>(e =>
            {
                e.ToTable("ExpenseLines");
                e.HasKey(x => x.Id);
                e.Property(x => x.Description).IsRequired().HasMaxLength(200);
                e.Property(x => x.Quantity).HasPrecision(18, 3);
                e.Property(x => x.UnitPrice).HasPrecision(18, 2);
                e.Property(x => x.LineTotal).HasPrecision(18, 2);
                e.Property(x => x.ToothFdi).HasMaxLength(20);
                e.HasIndex(x => x.ExpenseId);
                e.HasIndex(x => x.CatalogItemId);
                e.HasIndex(x => x.ClientId);
                e.HasIndex(x => x.CatalogProcedureId);
                e.HasOne(x => x.Expense)
                    .WithMany(ex => ex.Lines)
                    .HasForeignKey(x => x.ExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(x => x.CatalogItem)
                    .WithMany()
                    .HasForeignKey(x => x.CatalogItemId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.UnitOfMeasure)
                    .WithMany()
                    .HasForeignKey(x => x.UnitOfMeasureId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.Client)
                    .WithMany()
                    .HasForeignKey(x => x.ClientId)
                    .OnDelete(DeleteBehavior.SetNull);
                e.HasOne(x => x.CatalogProcedure)
                    .WithMany()
                    .HasForeignKey(x => x.CatalogProcedureId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ToothWork
            b.Entity<ToothWork>(e =>
            {
                e.ToTable("ToothWorks");
                e.HasKey(x => x.Id);

                e.Property(x => x.ToothFdi).IsRequired().HasMaxLength(2000);
                e.Property(x => x.ProcedureName).IsRequired().HasMaxLength(2000);
                e.Property(x => x.Tier).IsRequired().HasMaxLength(2000);
                e.Property(x => x.Surface).HasMaxLength(20).HasDefaultValue("");
                e.Property(x => x.ProcedureId);
                e.Property(x => x.RootCanalIds).HasMaxLength(400).HasDefaultValue("");

                e.HasIndex(x => x.ClientId);
                e.HasOne<Client>()
                 .WithMany()
                 .HasForeignKey(x => x.ClientId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Localization
            b.Entity<L10nKey>(e =>
            {
                e.ToTable("L10nKeys");
                e.HasKey(x => x.Id);
                e.Property(x => x.Key).HasMaxLength(200).IsRequired();

                // Avoid nvarchar(max) -> use large finite length
                e.Property(x => x.Group).HasMaxLength(2000);
                e.Property(x => x.Description).HasMaxLength(2000);

                e.HasIndex(x => x.Key).IsUnique();
            });

            b.Entity<L10nValue>(e =>
            {
                e.ToTable("L10nValues");
                e.HasKey(x => new { x.KeyId, x.Lang });
                e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
                e.HasOne(x => x.Key)
                 .WithMany(k => k.Values)
                 .HasForeignKey(x => x.KeyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Procedures & Prices
            b.Entity<Procedure>(e =>
            {
                e.ToTable("Procedures");
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.IsActive).HasDefaultValue(true);

                e.HasMany(p => p.Prices)
                 .WithOne(pp => pp.Procedure)
                 .HasForeignKey(pp => pp.ProcedureId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<ProcedurePrice>(e =>
            {
                e.ToTable("ProcedurePrices");
                e.HasKey(x => x.Id);
                e.Property(x => x.Currency).HasMaxLength(10);
                e.HasIndex(x => x.ProcedureId);

                // precision instead of HasColumnType
                e.Property(x => x.Tier1).HasPrecision(18, 2);
                e.Property(x => x.Tier2).HasPrecision(18, 2);
                e.Property(x => x.Tier3).HasPrecision(18, 2);
            });

            // Seed (unchanged)
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
        }

        public async Task<List<ProcedurePrice>> LoadLatestPricesAsync(CancellationToken ct = default)
        {
            var prices = await ProcedurePrices.AsNoTracking().ToListAsync(ct);
            return prices
                .GroupBy(p => p.ProcedureId)
                .Select(g => g.MaxBy(x => x.Id)!)
                .ToList();
        }
    }
}
