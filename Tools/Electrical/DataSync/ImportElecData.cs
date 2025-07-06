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
using HoloBlok.Utils.RevitElements.FamilySymbols;

#endregion

namespace HoloBlok.Tools.Electrical.DataSync
{
    [Transaction(TransactionMode.Manual)]
    public class ImportElecData : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Initialize the Revit context with the UIApplication
            RevitContext.Initialize(commandData.Application);

            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            List<string> marks = null;
            List<double> amps = null;
            List<double> mca = null;

            try
            {
                using (var excelReader = new ExcelImporter())
                {
                    // Select Excel file
                    string filePath = excelReader.SelectExcelFile();
                    if (string.IsNullOrEmpty(filePath))
                    {
                        return Result.Cancelled;
                    }

                    // Open the file
                    if (!excelReader.OpenFile(filePath))
                    {
                        return Result.Failed;
                    }

                    marks = excelReader.GetColumnDataAsString("A", startRow: 2);
                    amps = excelReader.GetColumnDataAsDouble("I", startRow: 2);
                    mca = excelReader.GetColumnDataAsDouble("J", startRow: 2);
                }

                var symbol = HBCollectors.GetSymbol(doc, BuiltInCategory.OST_DataDevices, "Junction Boxes - Data", "100 Square");
                List<FamilyInstance> instances = HBCollectors.GetFamilyInstancesBySymbol(doc, symbol);

                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Import Excel Data");

                    List<string> missingMarks = new List<string>();

                    for (int i = 0; i < marks.Count; i++)
                    {
                        string markValue = marks[i];
                        double ampValue = amps[i];
                        double mcaValue = mca[i];

                        // Find the first FamilyInstance where the Mark parameter contains the mark string
                        FamilyInstance matchingInstance = instances
                            .FirstOrDefault(fi =>
                            {
                                string instanceMark = fi.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsValueString();
                                return !string.IsNullOrEmpty(instanceMark) && instanceMark.Contains(markValue);
                            });

                        if (matchingInstance != null)
                        {
                            // Set the Amps parameter
                            var ampsParam = matchingInstance.LookupParameter("Total Amps");
                            bool? ampsSet = ampsParam?.Set(ampValue);

                            // Set the MCA parameter
                            var mcaParam = matchingInstance.LookupParameter("MCA");
                            bool? mcaSet = mcaParam?.Set(mcaValue);
                        }
                        else
                        {
                            missingMarks.Add(markValue);
                        }
                    }
                    if (missingMarks.Any())
                    {
                        string missingMarksText = string.Join(Environment.NewLine, missingMarks);
                        TaskDialog.Show("Missing FamilyInstances", $"No FamilyInstance found for the following marks: \n\n{missingMarksText}");
                    }

                    t.Commit();
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
