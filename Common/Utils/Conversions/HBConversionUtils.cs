using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Conversions
{
    internal static class HBConversionUtils
    {
        internal static double ProjectPaperspaceMMToFeet(double paperDistance, View view, Document doc)
        {

            double distanceInModelUnits = ProjectUnitsToFeet(paperDistance, doc) * view.Scale;

            return Math.Round(distanceInModelUnits, 3);
        }

        internal static double ProjectUnitsToFeet(double value, Document doc)
        {
            //Get the project units
            Units units = doc.GetUnits();
            FormatOptions formatOptions = units.GetFormatOptions(SpecTypeId.Length);
            double valueInFeet = UnitUtils.ConvertToInternalUnits(value, formatOptions.GetUnitTypeId());

            return valueInFeet;
        }
    }

}
