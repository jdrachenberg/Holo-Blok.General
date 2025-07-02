#region Namespaces
#endregion

namespace HoloBlok.Tools.LightFixtures
{
    public interface ISpacingStrategy
    {
        List<XYZ> CalculateFixtureLocations(LinkedRoomData room, double wallOffset);
    }
}
