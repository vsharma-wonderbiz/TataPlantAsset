using Domain.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using MappingService.Domain.Entities;

namespace Infrastructure.DBs
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<SignalTypes> SignalTypes { get; set; } 

        public DbSet<AssetConfiguration> AssetConfigurations { get; set; }

        public DbSet<AssetSignalDeviceMapping> MappingTable { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>()
                .HasMany(a => a.Childrens)
                .WithOne()
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.Name)
                .IsUnique();

            modelBuilder.Entity<AssetConfiguration>()
        .HasKey(ac=>ac.AssetConfigId);//primary key

            modelBuilder.Entity<AssetConfiguration>()
                .HasOne(a => a.Asset)
                .WithMany(a => a.AssetConfigurations)
                .HasForeignKey(a => a.AssetId);

            modelBuilder.Entity<AssetConfiguration>()
             .HasOne(ac => ac.SignalType)
             .WithMany(st => st.AssetConfigurations) // navigation property in SignalType
             .HasForeignKey(ac => ac.SignaTypeID);

            modelBuilder.Entity<AssetConfiguration>()
            .HasIndex(ac => new { ac.AssetId, ac.SignaTypeID })
            .IsUnique();
        }
    }
}
