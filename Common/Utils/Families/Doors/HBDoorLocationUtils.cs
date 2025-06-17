using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Families
{
    internal class HBDoorLocationUtils
    {
        internal static XYZ GetDoorLocation(FamilyInstance door)
        {
            if (door == null) throw new ArgumentNullException(nameof(door));

            Wall host = door.Host as Wall;
            if (host.WallType.Kind == WallKind.Curtain)
            {
                return GetCurtainWallDoorLocation(door);
            }
            //Get location point for non-curtain walls
            else
            {
                return GetNormalDoorLocation(door);
            }
        }

        private static XYZ GetCurtainWallDoorLocation(FamilyInstance door)
        {
            return (door.GetTotalTransform().OfPoint(new XYZ()));
        }

        private static XYZ GetNormalDoorLocation(FamilyInstance door)
        {
            return (door.Location as LocationPoint).Point;
        }

        internal static bool IsDoorFlipped(FamilyInstance door)
        {
            if (door == null) throw new ArgumentNullException(nameof(door));

            XYZ doorToRoomVector = GetAssociatedRoomLocationPoint(door) - GetDoorLocation(door);

            double dotProduct = doorToRoomVector.DotProduct(door.FacingOrientation);
            if (dotProduct < 0)
            {
                return true;
            }
            return false;
        }

        internal static XYZ GetAssociatedRoomLocationPoint(FamilyInstance curDoor)
        {
            Room curFromRoom = curDoor.FromRoom;
            Room curToRoom = curDoor.ToRoom;

            return (curToRoom?.Location as LocationPoint)?.Point ?? (curFromRoom?.Location as LocationPoint)?.Point;
        }
    }
}
