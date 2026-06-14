namespace BelotWebApp.BelotClasses
{
    public class ReplayTableRow
    {
        public ReplayTableRow(string id, string creation, string west, string north, string east, string south)
        {
            Id = id;
            Creation = creation;
            West = west;
            North = north;
            East = east;
            South = south;
        }

        public string Id { get; set; }
        public string Creation {  get; set; }
        public string West { get; set; }
        public string North { get; set; }
        public string East { get; set; }
        public string South { get; set; }
    }
}
