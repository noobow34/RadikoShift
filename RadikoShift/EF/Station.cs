using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadikoShift.EF;

[Table("stations")]
public partial class Station
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = null!;

    [Column("region_id")]
    public string? RegionId { get; set; }

    [Column("region_name")]
    public string? RegionName { get; set; }

    [Column("area")]
    public string? Area { get; set; }

    [Column("code")]
    public string? Code { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("display_order")]
    public int? DisplayOrder { get; set; }

    [Column("area_name")]
    public string? AreaName { get; set; }
}
