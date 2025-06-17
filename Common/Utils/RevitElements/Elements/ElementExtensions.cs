using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Common.Utils.RevitElements.Elements
{
    internal static class ElementExtensions
    {
        public static double GetParameterAsDouble(this Element element, string parameterName)
        {
            return ArchSmarterUtils.Parameters.GetParameterByName(element, parameterName)?.AsDouble() ?? 0.0;
        }

        public static int GetParameterAsInteger(this Element element, string parameterName)
        {
            return ArchSmarterUtils.Parameters.GetParameterByName(element, parameterName)?.AsInteger() ?? 0;
        }

        public static bool GetParameterAsBool(this Element element, string parameterName)
        {
            int yesNo = element.GetParameterAsInteger(parameterName);
            return yesNo == 1 ? true : false;
        }
    }
}
