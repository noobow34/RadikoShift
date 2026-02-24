using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadikoShift.EF;

[Table("programs")]
public partial class Program
{
    [Key]
    [Column("p_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PId { get; set; }

    [Column("id")]
    public string Id { get; set; } = null!;

    [Column("station_id")]
    public string? StationId { get; set; }

    [Column("start_time")]
    public DateTime? StartTime { get; set; }

    [Column("end_time")]
    public DateTime? EndTime { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("cast_name")]
    public string? CastName { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }
}
