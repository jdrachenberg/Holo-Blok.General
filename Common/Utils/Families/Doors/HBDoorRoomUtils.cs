using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using HoloBlok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Families
{
    internal class HBDoorRoomUtils
    {
        internal static void UpdateToFromRooms(Document doc)
        {
            FilteredElementCollector doorCollector = HBCollectors.GetInstancesOfCategory(doc, BuiltInCategory.OST_Doors);

            foreach (FamilyInstance curDoor in doorCollector.Cast<FamilyInstance>())
            {
                XYZ roomLocation = HBDoorLocationUtils.GetAssociatedRoomLocationPoint(curDoor);
                if (roomLocation == null) continue;
                bool doorIsFlipped = HBDoorLocationUtils.IsDoorFlipped(curDoor);

                FlipDoorRoomsIfNeeded(curDoor, doorIsFlipped, curDoor.FromRoom, curDoor.ToRoom);
            }
        }

        private static void FlipDoorRoomsIfNeeded(FamilyInstance curDoor, bool doorIsFlipped, Room fromRoom, Room toRoom)
        {
            if ((toRoom == null && doorIsFlipped == false) ||
                                (fromRoom == null && doorIsFlipped) ||
                                (doorIsFlipped && (toRoom != null && fromRoom != null)))
            {
                curDoor.FlipFromToRoom();
            }
        }

        private static List<FamilyInstance> GetAssociatedDoorsForRoom(Room room, FilteredElementCollector doorCollector)
        {
            List<FamilyInstance> associatedDoors = new List<FamilyInstance>();

            foreach (FamilyInstance door in doorCollector.Cast<FamilyInstance>())
            {
                if (IsAssociatedWithRoom(door, room))
                {
                    associatedDoors.Add(door);
                }
            }

            return associatedDoors;
        }
        private static bool IsAssociatedWithRoom(FamilyInstance door, Room room)
        {
            Room fromRoom = door.FromRoom;
            Room toRoom = door.ToRoom;

            return (fromRoom != null && fromRoom.Id == room.Id) ||
                (toRoom != null && toRoom.Id == room.Id);
        }

        internal static Dictionary<ElementId, List<FamilyInstance>> GetRoomDoorMap(FilteredElementCollector roomCollector, FilteredElementCollector doorCollector)
        {

            Dictionary<ElementId, List<FamilyInstance>> roomToDoorMap = new Dictionary<ElementId, List<FamilyInstance>>();

            foreach (Room room in roomCollector.Cast<Room>())
            {
                List<FamilyInstance> associatedDoors = GetAssociatedDoorsForRoom(room, doorCollector);
                if (associatedDoors.Any())
                {
                    roomToDoorMap[room.Id] = associatedDoors;
                }
            }

            return roomToDoorMap;
        }



    }
}
