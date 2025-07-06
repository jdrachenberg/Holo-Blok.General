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
        #region Get Parameters
        internal static T GetParameterValue<T>(Element element, string parameterName)
        {
            Parameter param = element.LookupParameter(parameterName);
            return ConvertParameterValue<T>(param);
        }

        internal static T GetParameterValue<T>(Element element, BuiltInParameter builtInParameter)
        {
            Parameter param = element.get_Parameter(builtInParameter);
            return ConvertParameterValue<T>(param);
        }

        private static T ConvertParameterValue<T>(Parameter param)
        {
            if (param == null) return default;

            object value = param.StorageType switch
            {
                StorageType.String => param.AsString(),
                StorageType.Integer => param.AsInteger(),
                StorageType.Double => param.AsDouble(),
                StorageType.ElementId => param.AsElementId(),
                _ => throw new InvalidOperationException($"Unsupported StorageType: {param.StorageType}")
            };

            // Check if the returned value matches the requested type
            if (value == null)
                return default;
            
            if (value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidCastException($"Parameter storage type {param.StorageType} does not match expected type {typeof(T).Name}");
        }

        #endregion

        #region Set Parameters - REFACTOR TO ALIGN WITH GET PARAMETER STRUCTURE
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
        #endregion

    }

}
