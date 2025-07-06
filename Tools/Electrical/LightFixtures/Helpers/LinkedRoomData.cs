#region Namespaces
using Autodesk.Revit.DB.Architecture;
using Transform = Autodesk.Revit.DB.Transform;

#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class LinkedRoomData
    {
        public Room Room { get; }
        public RevitLinkInstance LinkInstance { get; }
        public Transform LinkTransform { get; }

        public string Name => Room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
        public string Number => Room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";
        public string Department => Room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.AsString() ?? "";

        public LinkedRoomData(Room room, RevitLinkInstance linkInstance)
        {
            Room = room;
            LinkInstance = linkInstance;
            LinkTransform = linkInstance.GetTotalTransform();
        }

        public List<XYZ> GetBoundaryPointsInHostCoordinates()
        {
            var boundaries = Room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            var points = new List<XYZ>();

            foreach (var loop in boundaries)
            {
                foreach (var segment in loop)
                {
                    var curve = segment.GetCurve();
                    points.Add(LinkTransform.OfPoint(curve.GetEndPoint(0)));
                }
            }

            return points;
        }
    }
}
