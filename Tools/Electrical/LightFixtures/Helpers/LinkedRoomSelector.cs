#region Namespaces
using Autodesk.Revit.DB.Architecture;

#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class LinkedRoomSelector
    {
        private readonly RevitLinkInstance _linkedModel;

        public LinkedRoomSelector(RevitLinkInstance linkedModel)
        {
            _linkedModel = linkedModel;
        }

        public List<LinkedRoomData> SelectRooms()
        {
            var rooms = new List<LinkedRoomData>();

                Document linkDoc = _linkedModel.GetLinkDocument();
                if (linkDoc == null) return null;

                var linkRooms = new FilteredElementCollector(linkDoc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .Cast<Room>()
                    .Where(r => r.Area > 0) // Valid rooms only
                    .Select(r => new LinkedRoomData(r, _linkedModel))
                    .ToList();

                rooms.AddRange(linkRooms);

            return rooms;
        }

        private bool IsArchitectural(RevitLinkInstance link)
        {
            string linkName = link.Name.ToLower();
            return linkName.Contains("arch");
        }

        // Future method for filtering by department, name, etc.
        public List<LinkedRoomData> FilterByDepartment(List<LinkedRoomData> rooms, string department)
        {
            return rooms.Where(r => r.Department == department).ToList();
        }
    }
}
