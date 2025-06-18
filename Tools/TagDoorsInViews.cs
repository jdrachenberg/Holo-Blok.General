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
using HoloBlok.Forms;
using HoloBlok.Utils;
using HoloBlok.Utils.Families;
using HoloBlok.Utils.Geometry;
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
 *  Loading bar
 *  Only tag doors that are cut by the view range cut plane
 *  Dialog box if no door tag is loaded
 *  Choose door tag type
 *  
 * */

namespace HoloBlok
{
    [Transaction(TransactionMode.Manual)]
    public class TagDoorsInViews : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            //Get current view
            ViewSelectionWindow viewSelectionWindow = new ViewSelectionWindow(doc);
            bool? dialogResult = viewSelectionWindow.ShowDialog();

            if (dialogResult != true || viewSelectionWindow.SelectedViews == null || !viewSelectionWindow.SelectedViews.Any())
                return Result.Cancelled;

            List<View> selectedViews = viewSelectionWindow.SelectedViews;

            //Start transaction
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Tag doors in multiple views");

                int totalTagged = 0;
                List<string> errorMessages = new List<string>();

                foreach (View view in selectedViews)
                {
                    try
                    {
                        var result = DoorTagger.TagAllDoorsInView(doc, view);
                        if (result.Success)
                        {
                            totalTagged += result.TaggedCount;
                        }
                        else
                        {
                            errorMessages.Add($"{view.Name}: {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errorMessages.Add($"{view.Name}: {ex.Message}");
                    }
                }

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