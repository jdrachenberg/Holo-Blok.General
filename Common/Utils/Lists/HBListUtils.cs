using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Lists
{
    internal class HBListUtils
    {
        internal static List<XYZ> SortListByPointsRelativeX(Plane plane, List<XYZ> points)
        {
            if (plane == null) throw new ArgumentNullException(nameof(plane));
            if (points == null) throw new ArgumentNullException(nameof(points));

            Transform planeTransform = HBTransformUtils.GetTransform(plane);
            var sortedIndices = HBXYZUtils.SortIndicesByLocalXAtTransform(points, planeTransform);

            return SortByIndices(points, sortedIndices);
        }


        internal static List<T> SortListByPointsRelativeX<T>(Plane plane, List<XYZ> points, List<T> list)
        {
            if (plane == null) throw new ArgumentNullException(nameof(plane));
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (points.Count != list.Count) throw new ArgumentException("Points and list must have the same count.");

            Transform planeTransform = HBTransformUtils.GetTransform(plane);
            var sortedIndices = HBXYZUtils.SortIndicesByLocalXAtTransform(points, planeTransform);

            return SortByIndices(list, sortedIndices);
        }

        internal static List<T> SortByIndices<T>(List<T> list, List<int> indices)
        {
            return indices.Select(index => list[index]).ToList();
        }
    }
}
