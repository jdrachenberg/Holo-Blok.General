


using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using HoloBlok.Utils.Geometry;
using System.Security.Cryptography;

namespace HoloBlok.Utils.RevitElements
{
    internal static class HBViewUtils
    {
        public static Plane GetViewPlane(View activeView)
        {
            if (activeView == null) throw new ArgumentNullException(nameof(activeView));
            return Plane.CreateByNormalAndOrigin(activeView.ViewDirection, activeView.Origin);
        }


        public static IList<IList<Curve>> GetCropIntersectionsWithSolids(View activeView, IEnumerable<Solid> modelElementSolids)
        {
            if (activeView == null) throw new ArgumentNullException(nameof(activeView));
            if (modelElementSolids == null) throw new ArgumentNullException(nameof(modelElementSolids));

            IList<CurveLoop> cropLoops = GetSplitAdjustedCropLoops(activeView);
            var intersectingCurvesList = new List<IList<Curve>>();

            foreach (CurveLoop cropLoop in cropLoops)
            {
                IList<Curve> curvesIntersectingLoop = GetSolidIntersectionsWithCropLoop(cropLoop, modelElementSolids);
                intersectingCurvesList.Add(curvesIntersectingLoop);
            }

            return intersectingCurvesList;
        }


        private static IList<CurveLoop> GetSplitAdjustedCropLoops(View activeView)
        {
            var cropManager = activeView.GetCropRegionShapeManager();
            IList<CurveLoop> originalCropLoops = cropManager.GetCropShape();

            if (originalCropLoops.Count <= 1)
                return originalCropLoops;

            var adjustedCropLoops = new List<CurveLoop>();
            for (int i = 0; i < originalCropLoops.Count; i++)
            {
                XYZ splitRegionOffset = cropManager.GetSplitRegionOffset(i);
                adjustedCropLoops.Add(AdjustCropLoopBySplitOffset(originalCropLoops[i], splitRegionOffset));
            }

            return adjustedCropLoops;
        }


        private static CurveLoop AdjustCropLoopBySplitOffset(CurveLoop cropLoop, XYZ splitRegionOffset)
        {
            var adjustedLoop = new CurveLoop();
            foreach (Curve curve in cropLoop)
            {
                adjustedLoop.Append(curve.CreateTransformed(Transform.CreateTranslation(splitRegionOffset)));
            }
            return adjustedLoop;
        }


        private static IList<Curve> GetSolidIntersectionsWithCropLoop(CurveLoop cropLoop, IEnumerable<Solid> modelElementSolids)
        {
            var curvesIntersectingLoop = new List<Curve>();

            foreach (Curve curve in cropLoop)
            {
                IList<Curve> curvesIntersectingLine = GetSolidIntersectionsWithCropCurve(curve, modelElementSolids);
                if (curvesIntersectingLine.Count == 1)
                {
                    curvesIntersectingLoop.Add(curvesIntersectingLine[0]);
                }
                else if (curvesIntersectingLine.Count > 1)
                {
                    Curve combinedCurve = HBCurveListUtils.MergeCollinearCurves(curvesIntersectingLine);
                    curvesIntersectingLoop.Add(combinedCurve);
                }
            }

            return curvesIntersectingLoop;
        }

        //This checks for geometry intersections with an individual crop line
        private static IList<Curve> GetSolidIntersectionsWithCropCurve(Curve cropCurve, IEnumerable<Solid> modelElementSolids)
        {
            var curvesIntersectingLine = new List<Curve>();

            foreach (Solid modelElementSolid in modelElementSolids)
            {
                if (modelElementSolid == null) continue;

                try
                {
                    var intersection = modelElementSolid.IntersectWithCurve(cropCurve, new SolidCurveIntersectionOptions());
                    if (intersection?.SegmentCount == 1)
                    {
                        curvesIntersectingLine.Add(intersection.GetCurveSegment(0));
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error intersecting curve with solid: {e.Message}");
                }
            }

            return curvesIntersectingLine;
        }

        internal static IList<CurveLoop> GetAdjustedCropLoops(View view)
        {
            ViewCropRegionShapeManager cropManager = view.GetCropRegionShapeManager();
            IList<CurveLoop> originalCropLoops = cropManager.GetCropShape();

            if (originalCropLoops.Count == 1)
            {
                return originalCropLoops;
            }

            IList<CurveLoop> adjustedCropLoops = new List<CurveLoop>();
            for (int i = 0; i < originalCropLoops.Count; i++)
            {
                XYZ splitRegionOffset = cropManager.GetSplitRegionOffset(i);
                CurveLoop adjustedLoop = new CurveLoop();
                foreach (Curve curve in originalCropLoops[i])
                {
                    adjustedLoop.Append(curve.CreateTransformed(Transform.CreateTranslation(splitRegionOffset)));
                }
                adjustedCropLoops.Add(adjustedLoop);
            }
            return adjustedCropLoops;
        }


    }
}