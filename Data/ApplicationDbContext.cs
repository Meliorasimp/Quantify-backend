using Microsoft.EntityFrameworkCore;
using EnterpriseGradeInventoryAPI.Models;

namespace EnterpriseGradeInventoryAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<StorageLocation> StorageLocations { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure StorageLocation to Warehouse relationship - Cascade when warehouse is deleted
            modelBuilder.Entity<StorageLocation>()
                .HasOne(sl => sl.Warehouse)
                .WithMany()
                .HasForeignKey(sl => sl.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Inventory to StorageLocation relationship - Restrict to avoid cascade conflicts
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.StorageLocation)
                .WithMany()
                .HasForeignKey(i => i.StorageLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Inventory to User relationship - Restrict to avoid cascade conflicts  
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.User)
                .WithMany(u => u.Inventories)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Warehouse to User (CreatedBy) relationship - Restrict to avoid cascade conflicts
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.CreatedByUser)
                .WithMany(u => u.Warehouses)
                .HasForeignKey(w => w.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure StorageLocation to User relationship - Restrict to avoid cascade conflicts
            modelBuilder.Entity<StorageLocation>()
                .HasOne(sl => sl.User)
                .WithMany(u => u.StorageLocations)
                .HasForeignKey(sl => sl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}