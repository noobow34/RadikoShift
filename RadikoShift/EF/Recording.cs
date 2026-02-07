using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadikoShift.EF
{
    [Table("recordings")]
    public class Recording
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("reservation_id")]
        public int ReservationId { get; set; }

        [Column("program_id")]
        [Required]
        public string ProgramId { get; set; } = null!;

        [Column("station_id")]
        [Required]
        public string StationId { get; set; } = null!;

        [Column("station_name")]
        public string? StationName { get; set; }

        [Column("program_name")]
        public string? ProgramName { get; set; }

        [Column("cast_name")]
        public string? CastName { get; set; }

        [Column("start_time")]
        public TimeOnly StartTime { get; set; }

        [Column("end_time")]
        public TimeOnly EndTime { get; set; }

        [Column("file_name")]
        [Required]
        public string FileName { get; set; } = null!;

        [Column("mime_type")]
        public string? MimeType { get; set; }

        [Column("file_size")]
        public long FileSize { get; set; }

        [Column("audio_data")]
        public byte[] AudioData { get; set; } = null!;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
