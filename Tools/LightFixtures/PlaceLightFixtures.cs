#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static HoloBlok.Tools.LightFixtures.ProgressManager;
using Creation = Autodesk.Revit.Creation;
using Line = Autodesk.Revit.DB.Line;

#endregion

namespace HoloBlok.Tools.LightFixtures
{
    [Transaction(TransactionMode.Manual)]
    public class PlaceLightFixtures : IExternalCommand
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
            try
            {
                // 1. Get linked models (architectural and structural)
                List<RevitLinkInstance> linkedArchModels = GetArchitecturalLinkedModels(doc);
                List<RevitLinkInstance> linkedStrucModels = GetStructuralLinkedModels(doc);
                List<RevitLinkInstance> allLinkedModels = linkedArchModels.Concat(linkedStrucModels).ToList();

                if (!linkedArchModels.Any())
                {
                    TaskDialog.Show("Error", "No architectural or structural linked models found.");
                    return Result.Failed;
                }

                // 2. Get rooms from linked model
                var roomSelector = new LinkedRoomSelector(linkedArchModels.First());
                List<LinkedRoomData> selectedRooms = roomSelector.SelectRooms(); // TO-DO: Create options for selecting rooms

                // 3. Get light fixture family type - ENTER CORRECT FIXTURE NAMES
                var fixtureType = GetLightFixtureType(doc, "Ceiling Light - Flat Round", "60W - 230V");
                if (fixtureType == null)
                {
                    TaskDialog.Show("Error", "No light fixture family type found.");
                    return Result.Failed;
                }

                // 4. Get spacing configuration
                var spacingConfig = new GridSpacingConfiguration(4.0, 2.0); // 8 feet default


                // 5. Process rooms in batches
                var placementengine = new LightFixturePlacementEngine(doc, allLinkedModels, fixtureType);
                var progressManager = new ProgressManager(selectedRooms.Count);

                using (Transaction t = new Transaction(doc, "Place Light Fixtures"))
                {
                    t.Start();

                    foreach (var roomBatch in selectedRooms.Batch(PlacementConstants.BATCH_SIZE))
                    {
                        var results = placementengine.PlaceFixturesInRooms(roomBatch, spacingConfig, progressManager);

                        // Handle any errors or warnings
                        if (results.HasErrors)
                        {
                            // TO-DO: Implement error handling
                        }
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
        }

        

        private List<RevitLinkInstance> GetArchitecturalLinkedModels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsArchitectural(link))
                .ToList();
        }

        private bool IsArchitectural(RevitLinkInstance link)
        {
            string linkName = link.Name.ToLower();
            return linkName.Contains("arch") && !linkName.Contains("struc");
        }


        private List<RevitLinkInstance> GetStructuralLinkedModels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsStructural(link))
                .ToList();
        }

        private bool IsStructural(RevitLinkInstance link)
        {
            string linkName = link.Name.ToLower();
            return linkName.Contains("struc") && !linkName.Contains("arch");
        }

        private FamilySymbol GetLightFixtureType(Document doc, string familyName, string typeName)
        {
            // Get first available light fixture type
            // In future, this could be a user selection dialog
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.FamilyName == familyName && fs.Name == typeName);
        }

        public static string GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }


    }
}
