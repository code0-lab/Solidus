using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Core.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System;

namespace DomusMercatoris.Data
{
    public class DomusDbContext : DbContext
    {
        public DomusDbContext(DbContextOptions<DomusDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Ban> Bans { get; set; } = null!;
        public DbSet<Complaint> Complaints { get; set; } = null!;
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<VariantProduct> VariantProducts { get; set; } = null!;
        public DbSet<CargoTracking> CargoTrackings { get; set; } = null!;
        public DbSet<Sale> Sales { get; set; } = null!;
        public DbSet<SaleProduct> SaleProducts { get; set; } = null!;
        public DbSet<ProductCluster> ProductClusters { get; set; } = null!;
        public DbSet<ProductClusterMember> ProductClusterMembers { get; set; } = null!;
        public DbSet<ProductFeature> ProductFeatures { get; set; } = null!;
        public DbSet<AutoCategory> AutoCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };

            // Value Converter for List<string> (User Roles)
            var rolesConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v ?? new List<string>(), jsonOptions),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : (JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>())
            );

            var rolesComparer = new ValueComparer<List<string>>(
                (l1, l2) => (l1 ?? new List<string>()).SequenceEqual(l2 ?? new List<string>()),
                l => (l ?? new List<string>()).Aggregate(0, (acc, val) => HashCode.Combine(acc, val == null ? 0 : val.GetHashCode())),
                l => (l == null ? new List<string>() : l.ToList())
            );

            modelBuilder.Entity<User>(entity =>
            {
                var prop = entity.Property(u => u.Roles)
                    .HasConversion(rolesConverter);
                prop.Metadata.SetValueComparer(rolesComparer);

                entity.HasIndex(u => u.CompanyId);
                entity.HasOne(u => u.Company)
                    .WithMany(c => c.Users)
                    .HasForeignKey(u => u.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Configure Ban relationship explicitly
                entity.HasOne(u => u.Ban)
                    .WithOne(b => b.User)
                    .HasForeignKey<Ban>(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Value Converter for List<string> (Product Images)
            var imagesConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
                v => JsonSerializer.Serialize(v ?? new List<string>(), jsonOptions),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : (JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>())
            );
            var imagesComparer = new ValueComparer<List<string>>(
                (l1, l2) => (l1 ?? new List<string>()).SequenceEqual(l2 ?? new List<string>()),
                l => (l ?? new List<string>()).Aggregate(0, (acc, val) => HashCode.Combine(acc, val == null ? 0 : val.GetHashCode())),
                l => (l == null ? new List<string>() : l.ToList())
            );

            modelBuilder.Entity<Product>(entity =>
            {
                var imagesProp = entity.Property(p => p.Images)
                    .HasConversion(imagesConverter);
                imagesProp.Metadata.SetValueComparer(imagesComparer);

                entity.Property(p => p.Price)
                    .HasColumnType("decimal(18,2)");

                entity.HasIndex(p => new { p.CompanyId, p.CreatedAt });
                
                entity.HasOne(p => p.Company)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(p => p.Categories)
                    .WithMany(c => c.Products)
                    .UsingEntity(j => j.ToTable("ProductCategories"));

                entity.HasOne(p => p.Brand)
                    .WithMany(b => b.Products)
                    .HasForeignKey(p => p.BrandId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(p => p.Variants)
                    .WithOne(v => v.Product)
                    .HasForeignKey(v => v.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.CompanyId);
                entity.HasOne(c => c.Company)
                    .WithMany(cmp => cmp.Categories)
                    .HasForeignKey(c => c.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(c => c.Parent)
                    .WithMany(p => p.Children)
                    .HasForeignKey(c => c.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(c => c.ParentId);
            });

            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasIndex(b => b.CompanyId);
                entity.HasOne(b => b.Company)
                    .WithMany(c => c.Brands)
                    .HasForeignKey(b => b.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CargoTracking>(entity =>
            {
                entity.HasIndex(t => t.TrackingNumber).IsUnique();
                entity.HasOne(t => t.User)
                    .WithMany() // Assuming User doesn't have a specific collection for cargos yet
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(t => t.FleetingUser)
                    .WithMany()
                    .HasForeignKey(t => t.FleetingUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.HasIndex(s => new { s.CompanyId, s.CreatedAt });
                entity.Property(s => s.TotalPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(s => s.Company)
                    .WithMany()
                    .HasForeignKey(s => s.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.User)
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(s => s.CargoTracking)
                    .WithMany()
                    .HasForeignKey(s => s.CargoTrackingId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(s => s.FleetingUser)
                    .WithMany()
                    .HasForeignKey(s => s.FleetingUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasMany(s => s.SaleProducts)
                    .WithOne(sp => sp.Sale)
                    .HasForeignKey(sp => sp.SaleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SaleProduct>(entity =>
            {
                entity.Property(sp => sp.UnitPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(sp => sp.Product)
                    .WithMany()
                    .HasForeignKey(sp => sp.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(sp => sp.VariantProduct)
                    .WithMany()
                    .HasForeignKey(sp => sp.VariantProductId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProductCluster>(entity => {
                entity.HasMany(c => c.Members)
                      .WithOne(m => m.ProductCluster)
                      .HasForeignKey(m => m.ProductClusterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductClusterMember>(entity => {
                entity.HasOne(m => m.Product)
                      .WithMany()
                      .HasForeignKey(m => m.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductFeature>(entity => {
                entity.HasOne(f => f.Product)
                      .WithMany()
                      .HasForeignKey(f => f.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
