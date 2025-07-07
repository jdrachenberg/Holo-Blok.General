using HoloBlok.Utils.Families;
using HoloBlok.Utils.RevitElements.FamilySymbols;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class LightFixturePlacementEngine
    {
        private readonly Document _doc;
        private readonly List<RevitLinkInstance> _allLinkedModels;
        private readonly IFixtureSelectionStrategy _fixtureSelectionStrategy;
        private readonly HeightCalculator _heightCalculator;
        private readonly DuplicateChecker _duplicateChecker;

        public LightFixturePlacementEngine(Document doc, List<RevitLinkInstance> allLinkedModels, IFixtureSelectionStrategy fixtureSelectionStrategy)
        {
            _doc = doc;
            _allLinkedModels = allLinkedModels;
            _fixtureSelectionStrategy = fixtureSelectionStrategy;

            var defaultFixture = fixtureSelectionStrategy.GetDefaultFixtureType();
            _heightCalculator = new HeightCalculator(doc, allLinkedModels, defaultFixture);
            _duplicateChecker = new DuplicateChecker(doc, BuiltInCategory.OST_LightingFixtures);
        }

        public PlacementResults PlaceFixturesInRooms(IEnumerable<LinkedRoomData> rooms, ISpacingStrategy spacingStrategy, ProgressManager progressManager)
        {
            var results = new PlacementResults();

            _fixtureSelectionStrategy.ActivateAllFixtureTypes();

            foreach (var room in rooms)
            {
                try
                {
                    var roomResult = PlaceFixturesInRoom(room, spacingStrategy);
                    results.RoomResults.Add(roomResult);

                    progressManager.ReportProgress();
                }
                catch (Exception ex)
                {
                    results.Errors.Add($"Error in room {room.Number}: {ex.Message}");
                }
            }

            return results;
        }


        private RoomPlacementResult PlaceFixturesInRoom(LinkedRoomData roomData, ISpacingStrategy spacingStrategy)
        {
            var result = new RoomPlacementResult { RoomNumber = roomData.Number };

            // Calculate fixture locations
            List<XYZ> locations = spacingStrategy.CalculateFixtureLocations(roomData, PlacementConstants.WALL_OFFSET_FEET); //TO-DO: Replace with user-selected value

            foreach (var location in locations)
            {
                // Check for duplicates
                if (_duplicateChecker.IsDuplicateLocation(location))
                {
                    result.SkippedCount++;
                    continue;
                }

                // Get host data
                var hostData = _heightCalculator.GetLowestHostElementData(location, roomData);
                if (hostData == null)
                {
                    result.Errors.Add($"No valid host face found for point {location}");
                    result.SkippedCount++;
                    continue;
                }

                // Determine which fixture type to use based on the host element
                FamilySymbol fixtureType = _fixtureSelectionStrategy.SelectFixtureType(
                    hostData.HostElement,
                    hostData.LinkedDocument);

                if (fixtureType == null)
                {
                    result.Errors.Add($"No fixture type mapping found for host element at {location}");
                    result.SkippedCount++;
                    continue;
                }

                // Get the link host reference
                Reference hostRef = hostData.CreateHostReference();
                if (hostRef == null)
                {
                    result.Errors.Add($"Failed to create host reference at {location}");
                    result.SkippedCount++;
                    continue;
                }

                try
                {
                    FamilyInstance newInstance = _doc.Create.NewFamilyInstance(
                        hostRef,
                        location,
                        XYZ.BasisX,
                        fixtureType);

                    result.PlacedCount++;

                    HBFamilyInstanceUtils.RotateFamilyInstance(_doc, newInstance, hostData.GridRotation, location, hostData.HostFace);

                    // Track which fixture type was used (optional)
                    if (!result.FixtureTypesUsed.ContainsKey(fixtureType.Name))
                        result.FixtureTypesUsed[fixtureType.Name] = 0;
                    result.FixtureTypesUsed[fixtureType.Name]++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to place fixture at {location}: {ex.Message}");
                    result.SkippedCount++;
                }
            }

            return result;
        }
    }
}
