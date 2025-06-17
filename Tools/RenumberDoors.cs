#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HoloBlok.Utils;
using HoloBlok.Utils.Families;

#endregion

namespace HoloBlok
{
    [Transaction(TransactionMode.Manual)]
    public class RenumberDoors : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Initialize the Revit context with the UIApplication
            RevitContext.Initialize(commandData.Application);

            try
            {
                // this is a variable for the Revit application
                UIApplication uiapp = commandData.Application;

                // this is a variable for the current Revit model
                Document doc = uiapp.ActiveUIDocument.Document;

                //Collect rooms and doors
                FilteredElementCollector roomCollector = HBCollectors.GetInstancesOfCategory(doc, BuiltInCategory.OST_Rooms);
                FilteredElementCollector doorCollector = HBCollectors.GetInstancesOfCategory(doc, BuiltInCategory.OST_Doors);

                //Start transaction
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Renumber doors");

                    HBDoorRoomUtils.UpdateToFromRooms(doc);
                    HBDoorMarkingUtils.SetMarks(doc, roomCollector, doorCollector);

                    t.Commit();
                }
            }
            finally
            {
                RevitContext.Clear();
            }

            return Result.Succeeded;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }

        
    }
}
