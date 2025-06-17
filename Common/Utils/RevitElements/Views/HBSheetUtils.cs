using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.RevitElements.Sheets
{
    internal class HBSheetUtils
    {
        internal static IEnumerable<View> GetViewsOnSheet(Document doc, ViewSheet viewSheet)
        {
            var viewports = new FilteredElementCollector(doc)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .Where(vp => vp.SheetId == viewSheet.Id)
                .ToList();

            return viewports.Select(vp => doc.GetElement(vp.ViewId) as View)
                           .Where(v => v != null &&
                                      (v.ViewType == ViewType.FloorPlan ||
                                       v.ViewType == ViewType.Section ||
                                       v.ViewType == ViewType.Elevation));
        }
    }
}
