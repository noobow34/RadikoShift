using Microsoft.EntityFrameworkCore;

namespace RadikoShift.EF;

public partial class ShiftContext : DbContext
{
    public ShiftContext() => AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    public ShiftContext(DbContextOptions<ShiftContext> options) : base(options) => AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

    public virtual DbSet<Program> Programs { get; set; }

    public virtual DbSet<Station> Stations { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string connectionString = Environment.GetEnvironmentVariable("RADIKOSHIFT_CONNECTION_STRING") ?? "";
        optionsBuilder.UseNpgsql(connectionString);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Program>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("programs_pkey");
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("stations_pkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
