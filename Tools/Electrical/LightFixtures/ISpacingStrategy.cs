#region Namespaces
#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures
{
    public interface ISpacingStrategy
    {
        List<XYZ> CalculateFixtureLocations(LinkedRoomData room, double wallOffset);
    }
}
