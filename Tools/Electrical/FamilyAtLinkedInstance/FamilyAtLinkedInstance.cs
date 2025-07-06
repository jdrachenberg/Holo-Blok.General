#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
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
using HoloBlok.Common.DataSets;
using Creation = Autodesk.Revit.Creation;
using Org.BouncyCastle.Asn1.X509.Qualified;
using HoloBlok.Utils.RevitElements.FamilySymbols;
using HoloBlok.Utils.Parameters;

#endregion

namespace HoloBlok.Tools.Electrical.FamilyAtLinkedInstance
{
    [Transaction(TransactionMode.Manual)]
    public class FamilyAtLinkedInstance : IExternalCommand
    {
        private const string orphaned = "ORPHANED";
        
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Initialize the Revit context with the UIApplication
            RevitContext.Initialize(commandData.Application);

            try
            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;
                Document mechDoc = GetDocumentFromLinkedDiscipline(doc, LinkType.Mech);

                // 1. Get FamilySymbol to insert
                var symbol = HBCollectors.GetSymbol(doc, BuiltInCategory.OST_DataDevices, "Junction Boxes - Data", "100 Square"); // TO-DO: Allow user-entered values

                // 2. Extract all required mech data
                List<MechEquipPlacementData> mechData = MechEquipExtractor.ExtractAllMechEquipPlacementData(mechDoc);

                // 3. Get all existing family instances of the specified symbol
                List<FamilyInstance> existingInstances = HBCollectors.GetFamilyInstancesBySymbol(doc, symbol);

                // 4. Sync existing + determine what still needs creating
                var syncResults = FamilyInstanceSynchronizer.GetPlacementResults(mechData, existingInstances);
                
                
                using (Transaction t = new Transaction(doc))
                {
                    t.Start("Sync Fam Instances with Mech Equipment");

                    // Setup project parameter if required
                    Definition linkedIdDef = HBProjectParameterUtils.CreateLinkedIdParameter(doc, "LinkedId");

                    // 5. Update existing instances
                    foreach (var update in syncResults.InstancesToUpdate)
                        FamilyInstanceSynchronizer.UpdateFamilyInstanceIfNecessary(doc, update.FamilyInstance, update.MechData, update.Diff);

                    // 6. Update mark for orphaned instances
                    foreach (var famInstance in syncResults.OrphanedInstances)
                    {
                        Parameter markParam = famInstance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                        HBParameterUtils.SetParameterValue(markParam, orphaned);
                    }

                    // 7. Create missing family instances
                    var creationPairList = GetCreationEquipmentDataPairs(doc, symbol, syncResults.EquipmentToCreate);
                    var creationDataList = creationPairList.Select(p => p.creationData).ToList();
                    var newElementIds = doc.Create.NewFamilyInstances2(creationDataList);

                    // 8. Update instance parameters where required
                    UpdateFamilyInstanceParameters(doc, creationPairList, newElementIds, linkedIdDef);

                    // DEBUG
                    Debug.WriteLine($"Updated {syncResults.InstancesToUpdate.Count} existing instances.");
                    Debug.WriteLine($"Creating {syncResults.EquipmentToCreate.Count} new instances.");
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

        private static List<FamilyInstance> UpdateFamilyInstanceParameters(
            Document doc,
            List<(Creation.FamilyInstanceCreationData creationData, MechEquipPlacementData equipmentData)> creationEquipmentDataPairs,
            ICollection<ElementId> newElementIds,
            Definition linkedIdDef)
        {
            List<FamilyInstance> newFamilyInstances = GetFamilyInstancesFromIds(doc, newElementIds);
            Dictionary<FamilyInstance, MechEquipPlacementData> instanceToEquipmentMap =
                GetInstanceToEquipmentMap(creationEquipmentDataPairs, newElementIds, newFamilyInstances);

            foreach (var kvp in instanceToEquipmentMap)
            {
                FamilyInstance instance = kvp.Key;
                MechEquipPlacementData equipData = kvp.Value;

                string equipmentMark = equipData.Mark ?? string.Empty;
                string newMark = $"JB-{equipmentMark}";

                Parameter markParam = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                HBParameterUtils.SetParameterValue(markParam, newMark);

                Parameter elevationParam = instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                HBParameterUtils.SetParameterValue(elevationParam, equipData.ElevationFromLevel);

                Parameter linkedIdParam = instance.get_Parameter(linkedIdDef);
                HBParameterUtils.SetParameterValue(linkedIdParam, equipData.UniqueId);
            }
            return newFamilyInstances;
        }


        private static Document GetDocumentFromLinkedDiscipline(Document doc, LinkType linkType)
        {
            RevitLinkInstance linkInstance = HBCollectors.GetLinkedModelByDiscipline(doc, linkType);
            Document linkDoc = linkInstance.GetLinkDocument();
            return linkDoc;
        }

        // Get dictionary where new family instances are keys and associated mech equipment are values
        private static Dictionary<FamilyInstance, MechEquipPlacementData> GetInstanceToEquipmentMap(
            List<(Creation.FamilyInstanceCreationData creationData, MechEquipPlacementData equipmentData)> creationEquipmentDataPairs,
            ICollection<ElementId> newElementIds,
            List<FamilyInstance> newFamilyInstances)
        {
            var instanceToEquipmentMap = new Dictionary<FamilyInstance, MechEquipPlacementData>();

            for (int i = 0; i < newElementIds.Count; i++)
            {
                instanceToEquipmentMap[newFamilyInstances[i]] = creationEquipmentDataPairs[i].equipmentData;
            }

            return instanceToEquipmentMap;
        }

        private static List<FamilyInstance> GetFamilyInstancesFromIds(Document doc, ICollection<ElementId> newElementIds)
        {
            return newElementIds
                .Select(id => doc.GetElement(id))
                .OfType<FamilyInstance>()
                .ToList();
        }


        private List<(Creation.FamilyInstanceCreationData creationData, MechEquipPlacementData equipmentData)> GetCreationEquipmentDataPairs(
            Document doc, FamilySymbol symbol,
            List<MechEquipPlacementData> mechPlacementDataList)
        {

            /* Get pairs of family creation data / mech equipment data so mech data can later be associated with new
            family instances in same relationship. */
            var creationDataPairs = new List<(Creation.FamilyInstanceCreationData creationData, MechEquipPlacementData equipmentData)>();

            foreach (var mechPlacementData in mechPlacementDataList)
            {
                if (IsDataMissing(mechPlacementData))
                    continue;

                Level equipmentLevel = HBCollectors.GetLevelByName(doc, mechPlacementData.LevelName);

                // Move equipment XYZ so new family is placed beside equipment
                XYZ transformedXYZ = PlacementPointAdjuster.GetDesiredLocation(mechPlacementData); // Make constant or variable

                var creationData = new Creation.FamilyInstanceCreationData(
                    transformedXYZ,
                    symbol,
                    equipmentLevel,
                    StructuralType.NonStructural);

                creationDataPairs.Add((creationData, mechPlacementData));
            }

            return creationDataPairs;
        }
       


        private static bool IsDataMissing(MechEquipPlacementData equipLocationData)
        {
            if (equipLocationData == null || equipLocationData.LocationPoint == null || equipLocationData.LevelName == null)
            {
                Debug.WriteLine($"Skipping equipment due to missing data. Point: {equipLocationData?.LocationPoint}, Level: {equipLocationData?.LevelName}");
                return true;
            }

            return false;
        }



        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
