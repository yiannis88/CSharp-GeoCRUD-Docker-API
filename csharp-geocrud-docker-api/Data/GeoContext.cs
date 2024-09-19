using Models;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class GeoContext : DbContext
    {
        public GeoContext(DbContextOptions<GeoContext> options) : base (options)
        {
        }

        public DbSet<Geo> Geos { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Geo>().ToTable("geo");
        }
    }
}