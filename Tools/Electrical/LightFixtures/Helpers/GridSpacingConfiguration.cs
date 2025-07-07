#region Namespaces
#endregion

using NPOI.SS.Formula.Functions;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
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

            // Find bounding box of fixture space
            var minX = boundaryPoints.Min(p => p.X) + wallOffset;
            var maxX = boundaryPoints.Max(p => p.X) - wallOffset;
            var minY = boundaryPoints.Min(p => p.Y) + wallOffset;
            var maxY = boundaryPoints.Max(p => p.Y) - wallOffset;

            double roomWidth = maxX - minX;
            double roomHeight = maxY - minY;

            // Calculate how many spacings fit in the available width/height
            int countX = (int)Math.Floor(roomWidth / SpacingX) + 1;
            int countY = (int)Math.Floor(roomHeight / SpacingY) + 1;

            // Calculate total size occupied by grid
            double gridWidth = (countX - 1) * SpacingX;
            double gridHeight = (countY - 1) * SpacingY;

            //Calculate starting X and Y to center the grid
            double startX = minX + (roomWidth - gridWidth) / 2;
            double startY = minY + (roomHeight - gridHeight) / 2;


            // Generate grid points, centered in the room
            for (int i = 0; i < countX; i++)
            {
                double x = startX + i * SpacingX;

                for (int j = 0; j < countY; j++)
                {
                    double y = startY + j * SpacingY;

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
