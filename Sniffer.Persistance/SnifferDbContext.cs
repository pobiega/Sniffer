using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sniffer.Persistance
{
    public class SnifferDbContext : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CacheEntry>()
                .Property(x => x.Content)
                .HasColumnType("jsonb");

            modelBuilder.Entity<SystemData>()
                .Property(x => x.SystemId)
                .IsRequired();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseNpgsql("Host=localhost; Port=5432; Database=sniffer; Username=postgres; Password=aaaa")
                .UseSnakeCaseNamingConvention();
        }

        public DbSet<SystemData> SystemData { get; set; }
        public DbSet<CacheEntry> CacheEntries { get; set; }
    }

    public class SystemData
    {
        [Key]
        [Column("id")]
        public int SystemId { get; set; }
        public string Name { get; set; }
    }

    public class CacheEntry
    {
        [Key]
        public string Key { get; set; }

        public string Content { get; set; }
    }
}
