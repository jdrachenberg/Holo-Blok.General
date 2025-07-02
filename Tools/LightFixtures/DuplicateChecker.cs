#region Namespaces
#endregion

namespace HoloBlok.Tools.LightFixtures
{
    internal class DuplicateChecker
    {
        private readonly Document _doc;
        private readonly List<XYZ> _existingLocations;
        private const double TOLERANCE = 0.1; // feet

        public DuplicateChecker(Document doc)
        {
            _doc = doc;
            _existingLocations = GetExistingFixtureLocations();
        }

        private List<XYZ> GetExistingFixtureLocations()
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .Select(f => (f.Location as LocationPoint)?.Point)
                .Where(p => p != null)
                .ToList();
        }

        public bool IsDuplicateLocation(XYZ location)
        {
            return _existingLocations.Any(existing => existing.DistanceTo(new XYZ(location.X, location.Y, existing.Z)) < TOLERANCE);
        }
    }
}
