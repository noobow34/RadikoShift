using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadikoShift.EF
{

    [Table("reservations")]
    public class Reservation
    {
        [Key]
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }   // SERIAL対応

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

        // 毎回共通の開始・終了時刻
        [Column("start_time")]
        public TimeOnly StartTime { get; set; }

        [Column("end_time")]
        public TimeOnly EndTime { get; set; }

        // 単発予約用（日付）
        [Column("target_date")]
        public DateOnly? TargetDate { get; set; }

        // 0:Once / 1:Daily / 2:Weekly
        [Column("repeat_type")]
        public RepeatType RepeatType { get; set; }

        [Column("repeat_days")]
        public DayOfWeek? RepeatDays { get; set; }

        [Column("status")]
        public ReservationStatus Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
