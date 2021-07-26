using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Sniffer.Persistance
{
    public class SnifferDbContext : DbContext
    {
        public SnifferDbContext(DbContextOptions<SnifferDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CacheEntry>()
                .Property(x => x.Content)
                .HasColumnType("jsonb");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSnakeCaseNamingConvention();
        }

        public DbSet<CacheEntry> CacheEntries { get; set; }
    }

    public class CacheEntry
    {
        [Key]
        public string Key { get; set; }

        public string Content { get; set; }
    }
}
