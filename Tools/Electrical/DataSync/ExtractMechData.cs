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
using HoloBlok.Utils.Collectors;
using HoloBlok.Utils.DataExtractors;
using System.Xml;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq.Expressions;
using HoloBlok.Common.Utils.Excel;

#endregion

namespace HoloBlok.Tools.Electrical.DataSync
{
    [Transaction(TransactionMode.Manual)]
    public class ExtractMechData : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Initialize the Revit context with the UIApplication
            RevitContext.Initialize(commandData.Application);
            
            try
            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;

                RevitLinkInstance mechLink = HBCollectors.GetLinkedModelByDiscipline(doc, LinkType.Mech);
                Document mechDoc = mechLink.GetLinkDocument();

                // Extract mechanical equipment data
                var equipmentData = MechEquipExtractor.ExtractAllMechEquipData(mechDoc);

                if (equipmentData.Count == 0)
                {
                    TaskDialog.Show("No Data", "No mechanical equipment found in the linked model.");
                    return Result.Cancelled;
                }

                var exporter = new ExcelExporter();
                string fileName = exporter.ExportMechanicalEquipment(equipmentData, doc.Title);

                if (!string.IsNullOrEmpty(fileName))
                {
                    // Show summary with option to open file
                    TaskDialogResult result = TaskDialog.Show("Export Complete",
                        $"Successfully exported data from {equipmentData.Count} mechanical equipment family instances to: \n\n{fileName}\n\n" +
                        $"Open the Excel file now?",
                        TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                    if (result == TaskDialogResult.Yes)
                    {
                        try
                        {
                            Process.Start(fileName);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Error", $"Could not open Excel file: {ex.Message}");
                        }
                    }
                }

                return Result.Succeeded;
            }

            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
            finally
            {
                RevitContext.Clear();
            }
        }
        public static String GetMethod()
        {


            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }


    }


}
