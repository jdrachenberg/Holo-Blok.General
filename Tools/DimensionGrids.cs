#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Grid = Autodesk.Revit.DB.Grid;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using Autodesk.Revit.DB.Architecture;
using System.Net;
using System.Windows.Media.Media3D;
using System.Security.Cryptography;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.Conversions;
using HoloBlok.Utils.Datums;
using HoloBlok.Lists;
using HoloBlok.Utils.RevitElements;
using System.Linq.Expressions;

#endregion

namespace HoloBlok
{
    [Transaction(TransactionMode.Manual)]
    public class DimensionGrids : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            View activeView = doc.ActiveView;

            using (Transaction t = new Transaction(doc))
            {
                t.Start("Dimension Grids in Active View");
                DimensionGridsInView(doc, activeView);

                t.Commit();
            }

            return Result.Succeeded;
                    
        }

public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }

        private static bool DimensionGridsInView(Document doc, View view)
        {
            if (view == null) return false;


            var grids = HBCollectors.GetGridsInView(doc, view);
            var gridGroups = HBGridUtils.Group(doc, view, grids);
            if (!gridGroups.Any()) return true;

            foreach (var group in gridGroups)
            {
                if (!HBDimensionCreator.DimensionGridGroup(doc, view, group, 8))
                    return false;
            }

            return true;
        }

        public static bool DimensionGridsInViews(Document doc, IEnumerable<View> views)
        {
            if (doc == null) throw new System.ArgumentNullException(nameof(doc));
            if (views == null || !views.Any()) return false;

            using (Transaction t = new Transaction(doc, "Dimension Grids"))
            {
                try
                {
                    t.Start("Dimension Grids in Multiple Views");
                    foreach (View view in views)
                    {
                        if (!DimensionGridsInView(doc, view))
                        {
                            t.RollBack();
                            return false;
                        }
                    }
                    t.Commit();
                    return true;
                }
                catch (Exception)
                {
                    t.RollBack();
                    return false;
                }
            }
        }

        

        
    }
}
