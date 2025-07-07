#region Namespaces
#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class RoomPlacementResult
    {
        public string RoomNumber { get; set; }
        public int PlacedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; } = new List<string>();
        public Dictionary<string, int> FixtureTypesUsed { get; } = new Dictionary<string, int>();
    }
}
