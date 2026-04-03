using Microsoft.EntityFrameworkCore;

namespace RadikoShift.Data
{
    public class ShiftStagingContext : ShiftContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Station>().ToTable("stations_staging");
            modelBuilder.Entity<Data.Program>().ToTable("programs_staging");
        }
    }
}
