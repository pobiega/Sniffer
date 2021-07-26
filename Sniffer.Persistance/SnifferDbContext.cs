using Microsoft.EntityFrameworkCore;
using Sniffer.Persistance.Entities;

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
        public DbSet<ChannelConfiguration> ChannelConfigurations { get; set; }
    }
}
