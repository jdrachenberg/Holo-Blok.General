#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HoloBlok.Common.Enums;
using HoloBlok.Common.Utils.RevitElements.Doors;
using HoloBlok.Common.Utils.RevitElements.Elements;
using HoloBlok.Common.Utils.RevitElements.Tags;
using HoloBlok.Tools.Electrical.LightFixtures.Helpers;
using HoloBlok.Utils;
using HoloBlok.Utils.Collectors;
using HoloBlok.Utils.Families;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.RevitElements.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static HoloBlok.Tools.Electrical.LightFixtures.Helpers.ProgressManager;
using Creation = Autodesk.Revit.Creation;
using Line = Autodesk.Revit.DB.Line;

#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures
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
                List<RevitLinkInstance> linkedArchModels = HBCollectors.GetLinkedModelsByDiscipline(doc, LinkType.Arch);
                List<RevitLinkInstance> linkedStrucModels = HBCollectors.GetLinkedModelsByDiscipline(doc, LinkType.Struc);
                List<RevitLinkInstance> allLinkedModels = linkedArchModels.Concat(linkedStrucModels).ToList();

                if (!linkedArchModels.Any())
                {
                    TaskDialog.Show("Error", "No architectural or structural linked models found.");
                    return Result.Failed;
                }

                // 2. Get rooms from linked model
                var roomSelector = new LinkedRoomSelector(linkedArchModels.First());
                List<LinkedRoomData> selectedRooms = roomSelector.SelectRooms(); // TO-DO: Create options for selecting rooms

                if (!selectedRooms.Any())
                {
                    TaskDialog.Show("Warning", "No valid rooms found in linked mode.");
                    return Result.Cancelled;
                }

                // 3. Configure fixture selection based on ceiling types
                IFixtureSelectionStrategy fixtureStrategy;
                try
                {
                    fixtureStrategy = BuildFixtureConfiguration(doc);
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Configuration Error", $"Failed to configure fixtures: {ex.Message}");
                    return Result.Failed;
                }

                // 4. Get spacing configuration
                var spacingConfig = new GridSpacingConfiguration(
                    UnitUtils.ConvertToInternalUnits(1800, UnitTypeId.Millimeters),
                    UnitUtils.ConvertToInternalUnits(1200, UnitTypeId.Millimeters)); // 8 feet default


                // 5. Process rooms in batches
                var placementengine = new LightFixturePlacementEngine(doc, allLinkedModels, fixtureStrategy);
                var progressManager = new ProgressManager(selectedRooms.Count);

                using (Transaction t = new Transaction(doc, "Place Light Fixtures"))
                {
                    t.Start();

                    var allResults = new PlacementResults();

                    foreach (var roomBatch in selectedRooms.Batch(PlacementConstants.BATCH_SIZE))
                    {
                        var batchResults = placementengine.PlaceFixturesInRooms(roomBatch, spacingConfig, progressManager);

                        // Aggregate results
                        allResults.RoomResults.AddRange(batchResults.RoomResults);
                        allResults.Errors.AddRange(batchResults.Errors);
                    }

                    t.Commit();

                    // Show summary
                    ShowPlacementSummary(allResults);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private IFixtureSelectionStrategy BuildFixtureConfiguration(Document doc)
        {
            // Build the configuration
            // TODO: Replace these with actual family and type names from your project
            var configBuilder = new FixtureConfigurationBuilder(doc);

            // Check for available fixtures and use what's available
            var fixtureA = HBCollectors.GetSymbol(doc, BuiltInCategory.OST_LightingFixtures, "Downlight - Recessed Can", "203mm Incandescent - 230V");
            var fixtureB = HBCollectors.GetSymbol(doc, BuiltInCategory.OST_LightingFixtures, "Ceiling Light - Linear Box", "0600x1200mm(2 Lamp) - 230V");
            var fixtureC = HBCollectors.GetSymbol(doc, BuiltInCategory.OST_LightingFixtures, "Ceiling Light - Linear Box", "0600x0600mm(2 Lamp) - 230V");
            var defaultFixture = HBCollectors.GetSymbol(doc, BuiltInCategory.OST_LightingFixtures, "Downlight - Recessed Can", "203mm Incandescent - 230V");

            // Use first available fixture as default if specific default not found
            if (defaultFixture == null)
            {
                defaultFixture = fixtureA ?? fixtureB ?? fixtureC;
                if (defaultFixture == null)
                {
                    throw new InvalidOperationException("No light fixture families found in project.");
                }
            }



            configBuilder.SetDefaultFixture(defaultFixture);

            // Add mappings based on what's available
            if (fixtureA != null)
                configBuilder.AddMapping("Gypsum", fixtureA);

            if (fixtureB != null)
                configBuilder.AddMapping("600x1200mm_Grid", fixtureB);

            if (fixtureC != null)
                configBuilder.AddMapping("600x600mm_Grid", fixtureC);

            return configBuilder.Build();
        }



        private void ShowPlacementSummary(PlacementResults results)
        {
            var summary = new StringBuilder();
            summary.AppendLine("Light Fixture Placement Summary:");
            summary.AppendLine("================================");

            int totalPlaced = 0;
            int totalSkipped = 0;
            var fixtureTypeTotals = new Dictionary<string, int>();

            foreach (var roomResult in results.RoomResults)
            {
                totalPlaced += roomResult.PlacedCount;
                totalSkipped += roomResult.SkippedCount;

                foreach (var kvp in roomResult.FixtureTypesUsed)
                {
                    if (!fixtureTypeTotals.ContainsKey(kvp.Key))
                        fixtureTypeTotals[kvp.Key] = 0;
                    fixtureTypeTotals[kvp.Key] += kvp.Value;
                }
            }

            summary.AppendLine($"Total Fixtures Placed: {totalPlaced}");
            summary.AppendLine($"Total Locations Skipped: {totalSkipped}");
            summary.AppendLine($"Rooms Processed: {results.RoomResults.Count}");

            if (fixtureTypeTotals.Any())
            {
                summary.AppendLine("\nFixture Types Used:");
                foreach (var kvp in fixtureTypeTotals.OrderBy(x => x.Key))
                {
                    summary.AppendLine($"  {kvp.Key}: {kvp.Value}");
                }
            }

            if (results.HasErrors)
            {
                summary.AppendLine($"\nErrors Encountered: {results.Errors.Count}");
                // Show first 5 errors
                foreach (var error in results.Errors.Take(5))
                {
                    summary.AppendLine($"  - {error}");
                }
                if (results.Errors.Count > 5)
                {
                    summary.AppendLine($"  ... and {results.Errors.Count - 5} more errors");
                }
            }

            TaskDialog.Show("Placement Complete", summary.ToString());
        }

        public static string GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
