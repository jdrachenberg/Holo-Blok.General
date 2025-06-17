using Autodesk.Revit.DB;
using System.Collections.Generic;
using HoloBlok;

namespace HoloBlok.Utils.Families
{
    internal class DoorPriorityComparer : IComparer<FamilyInstance>
    {
        public int Compare(FamilyInstance door1, FamilyInstance door2)
        {
            //If
            bool door1SoloCirc = IsSoloCirculationDoor(door1);
            bool door2SoloCirc = IsSoloCirculationDoor(door2);

            if (door1SoloCirc != door2SoloCirc)
                return door1SoloCirc ? -1 : 1;

            bool door1Circ = door1SoloCirc || IsCirculationDoor(door1);
            bool door2Circ = door2SoloCirc || IsCirculationDoor(door2);

            if (door1Circ != door2Circ)
                return door1Circ ? -1 : 1;

            return door1.Id.IntegerValue.CompareTo(door2.Id.IntegerValue);
        }

        private static bool IsSoloCirculationDoor(FamilyInstance door)
        {
            return (HBRoomUtils.IsGivenDepartment(door.ToRoom, HBConstantValues.CirculationDept) && door.FromRoom == null) ||
                   (HBRoomUtils.IsGivenDepartment(door.FromRoom, HBConstantValues.CirculationDept) && door.ToRoom == null);
        }

        private static bool IsCirculationDoor(FamilyInstance door)
        {
            return HBRoomUtils.IsGivenDepartment(door.ToRoom, HBConstantValues.CirculationDept) ||
                   HBRoomUtils.IsGivenDepartment(door.FromRoom, HBConstantValues.CirculationDept);
        }

        private static int CompareDoorIds(ElementId door1Id, ElementId door2Id)
        {
            // Get the Revit version
            string revitVersion = RevitContext.UiApp.ActiveUIDocument.Document.Application.VersionNumber;

            // Use reflection to access the appropriate property
            var elementIdType = typeof(ElementId);
            if (int.Parse(revitVersion) >= 2024)
            {
                // Use Value property for Revit 2024+
                var valueProperty = elementIdType.GetProperty("Value");
                long value1 = (long)valueProperty.GetValue(door1Id);
                long value2 = (long)valueProperty.GetValue(door2Id);
                return value1.CompareTo(value2);
            }
            else
            {
                // Use IntegerValue property for Revit 2023
                var integerValueProperty = elementIdType.GetProperty("IntegerValue");
                int value1 = (int)integerValueProperty.GetValue(door1Id);
                int value2 = (int)integerValueProperty.GetValue(door2Id);
                return value1.CompareTo(value2);
            }
        }
    }


}
