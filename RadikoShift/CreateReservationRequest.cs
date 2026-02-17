namespace RadikoShift
{
    public class CreateReservationRequest
    {
        public string ProgramId { get; set; } = null!;

        public string Title { get; set; } = null!;

        public string? CastName { get; set; }

        public TimeOnly StartTime { get; set; }

        public TimeOnly EndTime { get; set; }

        public RepeatType RepeatType { get; set; }

        public bool IsEdited { get; set; }
    }
}