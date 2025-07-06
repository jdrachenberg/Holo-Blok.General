#region Namespaces
#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public interface ISpacingStrategy
    {
        List<XYZ> CalculateFixtureLocations(LinkedRoomData room, double wallOffset);
    }
}
