using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok
{
    internal static class HBRoomUtils
    {
        internal static XYZ GetLocationPoint(Room room)
        {
            return (room.Location as LocationPoint).Point;
        }

        internal static bool IsGivenDepartment(Room room, string department)
        {
            if (room == null) return false;

            Parameter roomDepartmentParam = room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT);

            if (roomDepartmentParam.AsString() != null)
            {
                string roomDepartment = roomDepartmentParam.AsString();
                return roomDepartment.Equals(department, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

    }

}
