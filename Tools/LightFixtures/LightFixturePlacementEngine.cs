#region Namespaces
#endregion

namespace HoloBlok.Tools.LightFixtures
{
    public class LightFixturePlacementEngine
    {
        private readonly Document _doc;
        private readonly List<RevitLinkInstance> _allLinkedModels;
        private readonly FamilySymbol _fixtureType;
        private readonly HeightCalculator _heightCalculator;
        private readonly DuplicateChecker _duplicateChecker;

        public LightFixturePlacementEngine(Document doc, List<RevitLinkInstance> allLinkedModels, FamilySymbol fixtureType)
        {
            _doc = doc;
            _allLinkedModels = allLinkedModels;
            _fixtureType = fixtureType;
            _heightCalculator = new HeightCalculator(doc, allLinkedModels, fixtureType);
            _duplicateChecker = new DuplicateChecker(doc);
        }

        public PlacementResults PlaceFixturesInRooms(IEnumerable<LinkedRoomData> rooms, ISpacingStrategy spacingStrategy, ProgressManager progressManager)
        {
            var results = new PlacementResults();

            // Ensure family symbol is active
            if (!_fixtureType.IsActive)
                _fixtureType.Activate();

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

                // Get host face
                var (hostFace, linkInstance) = _heightCalculator.GetLowestHostFace(location, roomData);
                if (hostFace == null)
                {
                    result.Errors.Add($"No valid host face found for point {location}");
                    result.SkippedCount++;
                    continue;
                }

                // Get the link instance and linked face reference
                Reference hostRef = hostFace.Reference.CreateLinkReference(linkInstance);
                XYZ faceNormal = hostFace.ComputeNormal(UV.Zero).Normalize();
                try
                {
                    _doc.Create.NewFamilyInstance(
                        hostRef,
                        location,
                        XYZ.BasisX,
                        _fixtureType);

                    result.PlacedCount++;

                    //int failedCount = placementData.Count - createdIds.Count;
                    //if (failedCount > 0)
                    //{
                        //result.Errors.Add($"{failedCount} fixtures failed to place in room {roomData.Number}");
                    //}
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Batch placement failed: {ex.Message}");
                }
            }

            return result;
        }
    }
}
