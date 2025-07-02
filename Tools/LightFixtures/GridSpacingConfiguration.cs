#region Namespaces
#endregion

namespace HoloBlok.Tools.LightFixtures
{
    public class GridSpacingConfiguration : ISpacingStrategy
    {
        public double SpacingX { get; set; }
        public double SpacingY { get; set; }

        public GridSpacingConfiguration(double spacingX, double spacingY)
        {
            SpacingX = spacingX;
            SpacingY = spacingY;
        }

        public List<XYZ> CalculateFixtureLocations(LinkedRoomData room, double wallOffset)
        {
            var locations = new List<XYZ>();

            // Get room boundary in host coordinates
            var boundaryPoints = room.GetBoundaryPointsInHostCoordinates();

            // Find bounding box of room
            var minX = boundaryPoints.Min(p => p.X);
            var maxX = boundaryPoints.Max(p => p.X);
            var minY = boundaryPoints.Min(p => p.Y);
            var maxY = boundaryPoints.Max(p => p.Y);

            // Apply wall offset
            minX += wallOffset;
            maxX -= wallOffset;
            minY += wallOffset;
            maxY -= wallOffset;

            // Generate grid points
            // TO-DO: this method starts at one end of the room and spaces them out until it reaches the closest point to the wall offset on the other side.
            // Create an alternate method where the points are centered in the middle of the room.
            for (double x = minX; x <= maxX; x += SpacingX)
            {
                for (double y = minY; y <= maxY; y += SpacingY)
                {
                    var point = new XYZ(x, y, 0); // Z will be determined later by height calculation method

                    // Check if point is inside room (simplified for rectangular rooms)
                    // TODO: Implement proper point-in-polygon test for irregular rooms
                    if (IsPointInRoom(point, room))
                    {
                        locations.Add(point);
                    }
                }
            }

            return locations;
        }

        private bool IsPointInRoom(XYZ point, LinkedRoomData room)
        {
            // Simplified implementation for rectangular rooms
            // Future: implement proper point-in-polygon algorithm
            return true;
        }
    }
}
