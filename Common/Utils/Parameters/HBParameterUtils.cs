using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok
{
    internal static class HBParameterUtils
    {
        internal static void SetByName(Document doc, ElementId elemId, string parameterName, object value)
        {
            //Get the parameter
            Element element = doc.GetElement(elemId);
            Parameter param = element.LookupParameter(parameterName);

            SetParameterValue(param, value);
        }

        internal static bool SetBuiltIn(Document doc, ElementId elemId, BuiltInParameter paramName, object value)
        {
            // Retrieve the parameter from the instance
            FamilyInstance instance = doc.GetElement(elemId) as FamilyInstance;
            Parameter param = instance.get_Parameter(paramName);
            SetParameterValue(param, value, out bool isSuccess);

            return isSuccess;
        }

        internal static object GetByName(Element element, string parameterName)
        {
            //Get the parameter
            Parameter param = element.LookupParameter(parameterName);

            //Check if the parameter exists
            if (param != null)
            {
                //Get the parameter value
                if (param.StorageType == StorageType.String)
                {
                    return param.AsString();
                }
                else if (param.StorageType == StorageType.Integer)
                {
                    return param.AsInteger();
                }
                else if (param.StorageType == StorageType.Double)
                {
                    return param.AsDouble();
                }
            }

            return null;
        }

        internal static void SetParameterValue(Parameter param, object value)
        {
            SetParameterValue(param, value, out bool success);
        }

        internal static void SetParameterValue(Parameter param, object value, out bool success)
        {
            success = false; // Initialize success to false

            if (param != null)
            {
                try
                {
                    switch (param.StorageType)
                    {
                        case StorageType.String:
                            param.Set(value.ToString());
                            success = true;
                            break;
                        case StorageType.Integer:
                            param.Set(Convert.ToInt32(value));
                            success = true;
                            break;
                        case StorageType.Double:
                            param.Set(Convert.ToDouble(value));
                            success = true;
                            break;
                    }
                }
                catch
                {
                    success = false; // Conversion or setting failed
                }
            }
        }

        
    }

}
