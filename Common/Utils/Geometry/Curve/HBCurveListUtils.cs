using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Geometry
{
    internal static class HBCurveListUtils
    {
        public static XYZ GetFirstLineDirection(IList<Curve> segments)
        {
            if (segments == null || segments.Count == 0)
                throw new ArgumentException("Segment list is null or empty", nameof(segments));

            Line firstLine = segments[0] as Line ??
                throw new ArgumentException("First segment is not a line", nameof(segments));

            XYZ directionVector = firstLine.Direction;
            return directionVector;
        }

        public static List<XYZ> GetAllEndEndpoints(IList<Curve> segments)
        {
            //Flatten the list of points (start and end) from all segments
            List<XYZ> allPoints = segments.SelectMany(x => new XYZ[] { x.GetEndPoint(0), x.GetEndPoint(1) }).ToList();

            return allPoints;
        }

        public static void ThrowExceptionIfEmpty(IList<Curve> segments)
        {
            if (segments == null || segments.Count == 0)
                throw new ArgumentException("Segment list is null or empty", nameof(segments));
        }

        public static Curve MergeCollinearCurves(IList<Curve> segments)
        {
            HBCurveListUtils.ThrowExceptionIfEmpty(segments);

            XYZ directionVector = HBCurveListUtils.GetFirstLineDirection(segments);
            if (!AreLinesCollinear(segments, directionVector))
                throw new ArgumentException("Segments are not collinear", nameof(segments));

            var allPoints = HBCurveListUtils.GetAllEndEndpoints(segments);
            var sortedPoints = HBXYZUtils.SortAlongVector(allPoints, directionVector);

            return HBCurveUtils.CreateFromStartEndPoints(sortedPoints);
        }

        public static bool AreLinesCollinear(IList<Curve> lines, XYZ baseDiretion)
        {
            return lines.All(seg => seg is Line line && line.Direction.IsAlmostEqualTo(baseDiretion));
        }
    }
}
