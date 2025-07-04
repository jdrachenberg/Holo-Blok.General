#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HoloBlok.Common.Utils.RevitElements.Doors;
using HoloBlok.Common.Utils.RevitElements.Elements;
using HoloBlok.Common.Utils.RevitElements.Tags;
using HoloBlok.Utils;
using HoloBlok.Utils.Families;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.RevitElements.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Shapes;
using Line = Autodesk.Revit.DB.Line;
using Transform = Autodesk.Revit.DB.Transform;

#endregion

/* FEATURES:
 *  Progress bar
 *  If tag location is outside of cropbox, do not tag
 *  Dialog box if no door tag is loaded
 *  Choose door tag type
 *  
 * */

namespace HoloBlok
{
    [Transaction(TransactionMode.Manual)]
    public class TagDoorsInView : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            //Get current view
            View currentView = doc.ActiveView;

            //Start transaction
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Tag and update doors");

                if (currentView is ViewSheet viewSheet)
                {
                    IEnumerable<View> planViewsOnSheet = HBSheetUtils.GetPlanViewsOnSheet(doc, viewSheet);
                    foreach (View planView in planViewsOnSheet)
                        DoorTagger.TagAllDoorsInView(doc, planView);
                }
                else DoorTagger.TagAllDoorsInView(doc, currentView);

                t.Commit();
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