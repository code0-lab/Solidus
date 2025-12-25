using Microsoft.EntityFrameworkCore;
using DomusMercatorisDotnetMVC.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace DomusMercatorisDotnetMVC.Utils
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CommentModel> Comments { get; set; }
        public DbSet<Ban> Bans { get; set; }
        public DbSet<Complaint> Complaints { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false
            };

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
            });

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
        }
    }
}
