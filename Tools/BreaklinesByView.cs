#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Windows.Controls;
using HoloBlok.Utils;
using HoloBlok.Utils.Conversions;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.RevitElements;
using HoloBlok.Utils.Families;
using System.Security.Cryptography.X509Certificates;
using HoloBlok.Utils.Collectors;
using HoloBlok.Utils.RevitElements.Sheets;

#endregion

namespace HoloBlok
{
    [Transaction(TransactionMode.Manual)]
    public class BreaklinesByView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            //Get breakline family and symbol
            Family breaklineFam = ArchSmarterUtils.Families.GetFamilyByName(doc, "00 Breakline");
            FamilySymbol breaklineSymbol = ArchSmarterUtils.Families.GetFamilySymbolByName(doc, breaklineFam, "Breakline"); //Change to GUI input

            View view = doc.ActiveView;

            using (Transaction t = new Transaction(doc, "Place Breaklines"))
            {
                t.Start();
                if (view is ViewSheet viewSheet)
                {
                    IEnumerable<View> viewsOnSheet = HBSheetUtils.GetViewsOnSheet(doc, viewSheet);

                    foreach (View curView in viewsOnSheet)
                    {
                        PlaceBreaklineForSingleView(doc, breaklineSymbol, curView);
                    }
                }
                else
                {
                    PlaceBreaklineForSingleView(doc, breaklineSymbol, view);
                }
                t.Commit();
            }

            return Result.Succeeded;
        }

        private static void PlaceBreaklineForSingleView(Document doc, FamilySymbol breaklineSymbol, View view)
        {
            double symbolSize = HBConversionUtils.ProjectPaperspaceMMToFeet(8, view, doc);

            Plane viewPlane = HBViewUtils.GetViewPlane(view);
            List<Solid> viewSolids = HBCollectorUtils.GetSolidsFromCollector(HBCollectors.GetModelElementsInView(doc, view));
            IEnumerable<Solid> modelElementSolids = HBPlaneUtils.GetSolidsIntersectingPlane(viewSolids, viewPlane);

            //Get lists of intersecting curves for each crop 
            IList<IList<Curve>> intersectingCurvesList = HBSolidUtils.GetCropCurvesIntersectingSolids(view, modelElementSolids);

            //Place family symbols at midpoints of intersecting curves
            foreach (var cropLoopCurves in intersectingCurvesList)
            {
                foreach (var curve in cropLoopCurves)
                {
                    HBFamilyInstanceUtils.PlaceBreaklineAtCurveMidpoint(doc, view, breaklineSymbol, curve, symbolSize);
                }
            }
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
