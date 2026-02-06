namespace RadikoShift.ViewModel
{
    public class ProgramFilterViewModel
    {
        public string SelectedReagion { get; set; } = "";
        public string SelectedArea { get; set; } = "";
        public string SelectedStation { get; set; } = "";
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public List<IdNamePair> Reagions { get; set; } = new();
        public List<IdNamePair> Areas { get; set; } = new();
        public List<IdNamePair> Stations { get; set; } = new();
    }

    public class IdNamePair
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int DisplayOrder { get; set; }
    }
}
