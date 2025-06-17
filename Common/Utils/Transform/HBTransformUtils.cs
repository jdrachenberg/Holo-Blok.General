using Autodesk.Revit.DB;

namespace HoloBlok.Utils
{
    public class HBTransformUtils
    {
        internal static Transform GetTransform(Plane plane)
        {
            Transform transform = Transform.Identity;
            transform.Origin = plane.Origin;
            transform.BasisX = plane.XVec;
            transform.BasisY = plane.YVec;
            transform.BasisZ = plane.Normal;
            return transform;
        }
    }
}
