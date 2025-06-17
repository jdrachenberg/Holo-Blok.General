using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using HoloBlok;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Families
{
    internal class HBDoorMarkingUtils
    {
        internal static void SetMarks(Document doc, FilteredElementCollector roomCollector, FilteredElementCollector doorCollector)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (roomCollector == null) throw new ArgumentNullException(nameof(roomCollector));
            if (doorCollector == null) throw new ArgumentNullException(nameof(doorCollector));

            var roomToDoorMap = HBDoorRoomUtils.GetRoomDoorMap(roomCollector, doorCollector);
            var roomNumberToDoorIdsMap = CreateRoomNumberToDoorIdsMap(doc, doorCollector, roomToDoorMap);
            var sortedRoomNumberMap = SortDoorListsInMap(doc, roomNumberToDoorIdsMap);

            string[] suffixes = ArrayUtils.GetAlphabet();

            foreach (var kvp in sortedRoomNumberMap)
            {
                string baseDoorNumber = kvp.Key;
                List<ElementId> doorIds = kvp.Value;

                if (doorIds.Count == 1)
                {
                    SetSingleDoorMark(doc, doorIds[0], baseDoorNumber);
                }
                else
                {
                    SetMultipleDoorMarks(doc, doorIds, baseDoorNumber, suffixes);
                }
            }
        }

        private static void SetSingleDoorMark(Document doc, ElementId doorId, string baseDoorNumber)
        {
            HBParameterUtils.SetBuiltIn(doc, doorId, BuiltInParameter.ALL_MODEL_MARK, baseDoorNumber);
        }

        private static void SetMultipleDoorMarks(Document doc, List<ElementId> doorIds, string baseDoorNumber, string[] suffixes)
        {
            for (int i = 0; i < doorIds.Count; i++)
            {
                string doorMark = baseDoorNumber + suffixes[i];
                HBParameterUtils.SetBuiltIn(doc, doorIds[i], BuiltInParameter.ALL_MODEL_MARK, doorMark);
            }
        }

        public static Dictionary<string, List<ElementId>> CreateRoomNumberToDoorIdsMap(Document doc, FilteredElementCollector doorCollector, Dictionary<ElementId, List<FamilyInstance>> roomToDoorMap)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (doorCollector == null) throw new ArgumentNullException(nameof(doorCollector));
            if (roomToDoorMap == null) throw new ArgumentNullException(nameof(roomToDoorMap));

            var roomNumberToDoorIdMap = new Dictionary<string, List<ElementId>>();
            foreach (FamilyInstance curDoor in doorCollector.Cast<FamilyInstance>())
            {
                string roomNumber = GetRoomNumber(curDoor, roomToDoorMap);
                if (!string.IsNullOrEmpty(roomNumber))
                {
                    AddToRoomNumberDoorMap(roomNumberToDoorIdMap, roomNumber, curDoor.Id);
                }
            }

            return roomNumberToDoorIdMap;
        }

        private static string GetRoomNumber(FamilyInstance door, Dictionary<ElementId, List<FamilyInstance>> roomToDoorMap)
        {
            if (door == null) throw new ArgumentNullException(nameof(door));
            if (roomToDoorMap == null) throw new ArgumentNullException(nameof(roomToDoorMap));

            Room selectedRoom = SelectRoomForNumbering(door, roomToDoorMap);
            return GetRoomNumberValue(selectedRoom);
        }


        private static Room SelectRoomForNumbering(FamilyInstance door, Dictionary<ElementId, List<FamilyInstance>> roomToDoorMap)
        {

            Room toRoom = door.ToRoom;
            Room fromRoom = door.FromRoom;

            //Step 1: Handle cases where one or both rooms are null
            if (toRoom == null && fromRoom == null) return null;
            if (toRoom == null) return fromRoom;
            if (fromRoom == null) return toRoom;

            //Step 2: If room only has one door, return that room
            if (roomToDoorMap.TryGetValue(toRoom.Id, out var toDoors) && toDoors.Count == 1) return toRoom;
            if (roomToDoorMap.TryGetValue(fromRoom.Id, out var fromDoors) && fromDoors.Count == 1) return fromRoom;

            string toDept = GetRoomDepartmentValue(toRoom);
            string fromDept = GetRoomDepartmentValue(fromRoom);

            //Step 3: Handle cases where doors to go or from circulation rooms
            if (toDept == HBConstantValues.CirculationDept && fromDept == HBConstantValues.CirculationDept) return toRoom;
            if (toDept == HBConstantValues.CirculationDept && fromDept != HBConstantValues.CirculationDept) return fromRoom;
            if (toDept != HBConstantValues.CirculationDept && fromDept == HBConstantValues.CirculationDept) return toRoom;

            return toRoom;
        }

        private static string GetRoomNumberValue(Room room)
        {
            if (room == null) return null;
            return room.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsValueString();
        }

        private static string GetRoomDepartmentValue(Room room)
        {
            return room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT).AsValueString();
        }

        private static void AddToRoomNumberDoorMap(Dictionary<string, List<ElementId>> doorNumberMap, string roomNumber, ElementId doorId)
        {
            //If the dictionary already has the room number, add the door to the list. Otherwise, create a new list.
            if (!doorNumberMap.ContainsKey(roomNumber))
            {
                doorNumberMap[roomNumber] = new List<ElementId>();
            }
            doorNumberMap[roomNumber].Add(doorId);
        }

        private static Dictionary<string, List<ElementId>> SortDoorListsInMap(Document doc, Dictionary<string, List<ElementId>> roomNumberToDoorIdsMap)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (roomNumberToDoorIdsMap == null) throw new ArgumentNullException(nameof(roomNumberToDoorIdsMap));

            foreach (string roomNumber in roomNumberToDoorIdsMap.Keys.ToList())
            {
                var doorIds = roomNumberToDoorIdsMap[roomNumber];
                if (doorIds.Count == 1) continue;

                List<FamilyInstance> doors = GetDoorsFromIds(doc, doorIds);
                List<FamilyInstance> sortedDoors = SortDoorsByCirculation(doors);
                roomNumberToDoorIdsMap[roomNumber] = sortedDoors.Select(door => door.Id).ToList();
            }

            return roomNumberToDoorIdsMap;
        }

        private static List<FamilyInstance> GetDoorsFromIds(Document doc, List<ElementId> doorIds)
        {
            return doorIds.Select(id => doc.GetElement(id) as FamilyInstance)
                .Where(door => door != null)
                .ToList();
        }

        private static List<FamilyInstance> SortDoorsByCirculation(List<FamilyInstance> doors)
        {
            return doors.OrderBy(door => door, new DoorPriorityComparer()).ToList();
        }
    }
}
