using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using HoloBlok.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HoloBlok.Utils.Collectors.HBCollectorUtils;

namespace HoloBlok.Utils.Collectors
{
    internal static class HBCollectorUtils
    {
        public static List<Solid> GetSolidsFromCollector(FilteredElementCollector modelElements)
        {
            List<Solid> solids = new List<Solid>();

            foreach (Element element in modelElements)
            {
                Solid geomSolid = HBSolidUtils.CreateFromElement(element);

                if (geomSolid != null)
                {
                    solids.Add(geomSolid);
                }
            }

            return solids;
        }

    }
}
