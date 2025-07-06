using HoloBlok.Common.DataSets;
using HoloBlok.Tools.Electrical.DataSync;


namespace HoloBlok.Utils.DataExtractors
{
    public static class MechEquipExtractor
    {
        public static List<MechEquipData> ExtractAllMechEquipData(Document doc)
        {
            var equipmentList = new List<MechEquipData>();

            // Collect all mechanical equipment
            IEnumerable<FamilyInstance> collector = HBCollectors.GetMechanicalEquipmentInstances(doc);

            foreach (var equipment in collector)
            {
                MechEquipData data = ExtractMechEquipData(equipment);
                if (data != null && !string.IsNullOrEmpty(data.Mark))
                {
                    equipmentList.Add(data);
                }
            }

            // Sort by Mark
            var sortedList = equipmentList.OrderBy(e => e.Mark, AlphanumericComparer.Instance).ToList();
            return sortedList;
        }

        public static List<MechEquipPlacementData> ExtractAllMechEquipPlacementData(Document doc)
        {
            var locationList = new List<MechEquipPlacementData>();

            // Collect all mech equipment
            IEnumerable<FamilyInstance> collector = HBCollectors.GetMechanicalEquipmentInstances(doc);

            foreach (var equipment in collector)
            {
                MechEquipPlacementData locationData = ExtractMechEquipPlacementData(doc, equipment);
                if (locationData != null)
                {
                    locationList.Add(locationData);
                }
            }

            return locationList;
        }

        private static MechEquipData ExtractMechEquipData(FamilyInstance equipment)
        {
            try
            {
                var data = new MechEquipData
                {
                    ElementId = equipment.Id,
                    Mark = HBParameterUtils.GetParameterValue<string>(equipment, BuiltInParameter.ALL_MODEL_MARK),
                    Description = HBParameterUtils.GetParameterValue<string>(equipment, BuiltInParameter.ALL_MODEL_DESCRIPTION),
                    SpaceName = equipment.Space?.Name,
                    SpaceNumber = equipment.Space?.Number
                };

                // Get electrical parameters from the type
                var equipmentSymbol = equipment.Symbol;

                data.Voltage = UnitUtils.ConvertFromInternalUnits(HBParameterUtils.GetParameterValue<double>(equipmentSymbol, "Voltage"), UnitTypeId.Volts);
                data.ApparentLoadPhase1 = UnitUtils.ConvertFromInternalUnits(HBParameterUtils.GetParameterValue<double>(equipmentSymbol, "Apparent Load Phase 1"), UnitTypeId.VoltAmperes);
                data.ApparentLoadPhase2 = UnitUtils.ConvertFromInternalUnits(HBParameterUtils.GetParameterValue<double>(equipmentSymbol, "Apparent Load Phase 2"), UnitTypeId.VoltAmperes);
                data.TotalApparentLoad = data.ApparentLoadPhase1 + data.ApparentLoadPhase2;

                return data;
            }
            catch (Exception ex)
            {
                // Log error but continue processing other equipment
                Debug.WriteLine($"Error extracting data for equipment {equipment.Id}: {ex.Message}");
                return null;
            }
        }

        private static MechEquipPlacementData ExtractMechEquipPlacementData(Document doc, FamilyInstance equipment)
        {
            try
            {
                ElementId levelId = HBParameterUtils.GetParameterValue<ElementId>(equipment, BuiltInParameter.FAMILY_LEVEL_PARAM);
                ElementId equipId = equipment.Id;

                var data = new MechEquipPlacementData
                {
                    ElementId = equipment.Id,
                    UniqueId = equipment.UniqueId,
                    Transform = equipment.GetTransform(),
                    LocationPoint = (equipment.Location as LocationPoint).Point,
                    LevelName = (doc.GetElement(levelId) as Level).Name,
                    ElevationFromLevel = HBParameterUtils.GetParameterValue<double>(equipment, BuiltInParameter.INSTANCE_ELEVATION_PARAM),
                    Mark = HBParameterUtils.GetParameterValue<string>(equipment, BuiltInParameter.ALL_MODEL_MARK)

                };

                return data;
            }
            catch (Exception ex)
            {
                // Log error but continue processing other equipment
                Debug.WriteLine($"Error extracting data for equipment {equipment.Id}: {ex.Message}");
                return null;
            }
        }

    }

}
