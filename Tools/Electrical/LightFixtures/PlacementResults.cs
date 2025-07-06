#region Namespaces
#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures
{
    // Helper classes for results and progress
    public class PlacementResults
    {
        public List<RoomPlacementResult> RoomResults { get; } = new List<RoomPlacementResult>();
        public List<string> Errors { get; } = new List<string>();
        public bool HasErrors => Errors.Any();
    }
}
