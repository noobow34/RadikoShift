namespace RadikoShift
{
    public class CreateReservationRequest
    {
        public string ProgramId { get; set; } = null!;
        public RepeatType RepeatType { get; set; }
    }
}
