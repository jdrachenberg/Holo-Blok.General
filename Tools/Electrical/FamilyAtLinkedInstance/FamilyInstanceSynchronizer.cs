using HoloBlok.Common;
using HoloBlok.Common.DataSets;
using HoloBlok.Common.MathExtensions;
using HoloBlok.Utils;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.FamilyAtLinkedInstance
{
    internal static class FamilyInstanceSynchronizer
    {
        public class SyncResults
        {
            public List<(FamilyInstance FamilyInstance, MechEquipPlacementData MechData, FamilyInstanceDiff Diff)> InstancesToUpdate { get; set; } = new();
            public List<MechEquipPlacementData> EquipmentToCreate { get; set; } = new();
            public List<FamilyInstance> OrphanedInstances { get; set; } = new();
            public List<FamilyInstance> UnchangedInstances { get; set; } = new();
        }

        public static SyncResults GetPlacementResults(List<MechEquipPlacementData> mechData, List<FamilyInstance> existingInstances)
        {
            var results = new SyncResults();

            // Create a lookup dictionary for each mech equipment by UniqueId
            var mechDataById = mechData.ToDictionary(m => m.UniqueId);

            var matchedMechIds = new HashSet<string>();

            foreach (var instance in existingInstances)
            {
                string linkedId = HBParameterUtils.GetParameterValue<string>(instance, "LinkedId");

                // Check if instance is orphaned
                if (string.IsNullOrEmpty(linkedId) || !mechDataById.TryGetValue(linkedId, out var mechEquip))
                {
                    // No match found: mark as orphaned
                    results.OrphanedInstances.Add(instance);
                    continue;
                }

                matchedMechIds.Add(linkedId);

                // Check if this instance needs to be updated
                var diff = GetDifferences(instance, mechEquip);
                if (diff.HasChanges)
                    results.InstancesToUpdate.Add((instance, mechEquip, diff));
                else
                    results.UnchangedInstances.Add(instance);
            }

            // Any mech equipment without a match needs a new family instance
            foreach (var mech in mechData)
            {
                if (!matchedMechIds.Contains(mech.UniqueId))
                    results.EquipmentToCreate.Add(mech);
            }

            return results;
        }

        public static void UpdateFamilyInstanceIfNecessary(Document doc, FamilyInstance instance, MechEquipPlacementData mechData, FamilyInstanceDiff diff)
        {
            if (diff.NeedsMove)
                ElementTransformUtils.MoveElement(doc, instance.Id, diff.DesiredLocation.Subtract((instance.Location as LocationPoint)?.Point));

            if (diff.NeedsMarkUpdate)
                HBParameterUtils.SetParameterValue(instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK), diff.DesiredMark);

            if (diff.NeedsElevationUpdate)
                HBParameterUtils.SetParameterValue(instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM), diff.DesiredElevation);
        }


        private static FamilyInstanceDiff GetDifferences(FamilyInstance instance, MechEquipPlacementData mechData)
        {
            // Compare Mark, Elevation, and Location
            var (currentLocation, desiredLocation) = GetLocationData(instance, mechData);
            var currentMark = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString();
            var expectedMark = $"JB-{mechData.Mark}";
            var currentElevation = instance.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM).AsDouble();

            return new FamilyInstanceDiff
            {
                NeedsMove = !currentLocation.IsAlmostEqualTo(desiredLocation),
                NeedsMarkUpdate = !currentMark.Equals(expectedMark, StringComparison.OrdinalIgnoreCase),
                NeedsElevationUpdate = !currentElevation.IsAlmostEqualTo(mechData.ElevationFromLevel),
                DesiredLocation = desiredLocation,
                DesiredMark = expectedMark,
                DesiredElevation = mechData.ElevationFromLevel
            };
        }

        private static (XYZ currentLocation, XYZ desiredLocation) GetLocationData(FamilyInstance instance, MechEquipPlacementData mechData)
        {
            var currentLocation = (instance.Location as LocationPoint)?.Point;
            var desiredLocation = PlacementPointAdjuster.GetDesiredLocation(mechData);
            return (currentLocation, desiredLocation);
        }
    }
}
