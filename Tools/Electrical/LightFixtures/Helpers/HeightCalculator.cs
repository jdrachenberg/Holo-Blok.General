#region Namespaces
using NPOI.SS.Formula.Functions;
using Transform = Autodesk.Revit.DB.Transform;

#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class HeightCalculator
    {
        private readonly Document _hostDoc;
        private readonly List<RevitLinkInstance> _linkedModels;
        private readonly FamilySymbol _fixtureType;
        private readonly double _defaultHeight;

        public HeightCalculator(Document hostDoc, List<RevitLinkInstance> linkedModels, FamilySymbol fixtureType, double defaultHeight = 6.0)
        {
            _hostDoc = hostDoc;
            _linkedModels = linkedModels;
            _fixtureType = fixtureType;
            _defaultHeight = defaultHeight;
        }
        
        


        public HostElementData GetLowestHostElementData(XYZ location, LinkedRoomData room)
        {
            //var fixtureExtents = GetFixtureExtents(_fixtureType);
            //var testPoints = GenerateTestPoints(location, fixtureExtents);


            // HostElementData bestLinkInstance = null;
            // Face lowestFace = null;
            HostElementData hostData = GetHostElementData(location, room);

            //var hostData = GetHostElementData(location, room);

            //foreach (var testPoint in testPoints)
            //{
            //    var testData = GetHostElementData(testPoint, room);
            //    if (hostData != null)
            //    {
            //        bestHostData = hostData;
            //    }
            //}

            return hostData;
        }

        private BoundingBoxXYZ GetFixtureExtents(FamilySymbol fixtureType)
        {
            // get family geometry to determine extents
            // TO-DO: Implement full logic
            return fixtureType.get_BoundingBox(null);
        }

        private List<XYZ> GenerateTestPoints(XYZ placementPoint, BoundingBoxXYZ extents)
        {
            var points = new List<XYZ> { placementPoint };
            
            if (extents != null)
            {
                // Get the actual bounding box corners at the placement elevation
                double z = placementPoint.Z;

                // Four corners of the bounding box
                points.Add(new XYZ(extents.Min.X, extents.Min.Y, z));
                points.Add(new XYZ(extents.Min.X, extents.Max.Y, z));
                points.Add(new XYZ(extents.Max.X, extents.Min.Y, z));
                points.Add(new XYZ(extents.Max.X, extents.Max.Y, z));

                // Midpoints of bounding box edges
                points.Add(new XYZ((extents.Min.X + extents.Max.X) / 2, extents.Min.Y, z));
                points.Add(new XYZ((extents.Min.X + extents.Max.X) / 2, extents.Max.Y, z));
                points.Add(new XYZ(extents.Min.X, (extents.Min.Y + extents.Max.Y) / 2, z));
                points.Add(new XYZ(extents.Max.X, (extents.Min.Y + extents.Max.Y) / 2, z));
            }

            return points;
        }

        private HostElementData GetHostElementData(XYZ point, LinkedRoomData roomData)
        {
            // Create vertical ray
            var rayOrigin = new XYZ(point.X, point.Y, roomData.Room.Level.ProjectElevation);
            var rayDirection = XYZ.BasisZ;

            // TO-DO: allow user to select reference 3D view to use
            View3D view3D = new FilteredElementCollector(_hostDoc)
                .OfClass(typeof(View3D))
                .FirstOrDefault(v => !(v as View3D).IsTemplate) as View3D;
            if (view3D == null)
                throw new InvalidOperationException("No non-template 3D views found.");

            var refIntersector = new ReferenceIntersector(
                GetStructuralElementFilter(),
                FindReferenceTarget.Element,
                view3D);

            refIntersector.FindReferencesInRevitLinks = true;

            ReferenceWithContext nearestRef = refIntersector.FindNearest(rayOrigin, rayDirection);

            if (nearestRef == null)
                return null;

            Reference reference = nearestRef.GetReference();
            RevitLinkInstance linkInstance = _hostDoc.GetElement(reference.ElementId) as RevitLinkInstance;
            if (linkInstance == null)
                return null;

            Document linkedDoc = linkInstance.GetLinkDocument();
            Element linkedElement = linkedDoc.GetElement(reference.LinkedElementId);
            if (linkedElement == null)
                return null;

            // Get the specific face and intersection point
            var (face, intersectionHeight) = GetFaceAndIntersection(linkedElement, rayOrigin, rayDirection, linkInstance.GetTransform());

            if (face == null)
                return null;

            double gridRotation = GetCeilingGridRotationRadians(linkedDoc, linkedElement);

            return new HostElementData(face, linkedElement, linkInstance, linkedDoc, intersectionHeight, gridRotation);
        }

        private (Face face, double height) GetFaceAndIntersection(Element linkedElement, XYZ rayOrigin, XYZ rayDirection, Transform linkTransform)
        {
            Options geomOptions = new Options { ComputeReferences = true };
            GeometryElement geomElement = linkedElement.get_Geometry(geomOptions);

            Face lowestFace = null;
            double lowestZ = double.MaxValue;

            foreach (GeometryObject geomObj in geomElement)
            {
                Solid solid = geomObj as Solid;
                if (solid == null || solid.Faces.IsEmpty)
                    continue;

                foreach (Face face in solid.Faces)
                {
                    IntersectionResult result = face.Project(rayOrigin);
                    if (result != null)
                    {
                        XYZ intersection = linkTransform.OfPoint(result.XYZPoint);

                        // Check that the intersection is in the direction of the ray (i.e. above ray origin)
                        XYZ vectorFromOrigin = intersection - rayOrigin;
                        if (vectorFromOrigin.DotProduct(rayDirection) > 0)
                        {
                            // Check if its the lowest so far
                            if (intersection.Z < lowestZ)
                            {
                                lowestZ = intersection.Z;
                                lowestFace = face;
                            }
                        }
                    }
                }
            }

            return (lowestFace, lowestZ);
        }

        private ElementFilter GetStructuralElementFilter()
        {
            var categories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_Roofs
            };

            var filters = categories.Select(c => new ElementCategoryFilter(c)).Cast<ElementFilter>().ToList();
            return new LogicalOrFilter(filters);
        }

        public static double GetCeilingGridRotationRadians(Document doc, Element ceilingElement)
        {
            CeilingType ceilingType = doc.GetElement(ceilingElement.GetTypeId()) as CeilingType;
            if (ceilingType == null)
                return 0;

            // Get compound structure instead of using GetMaterialIds()
            CompoundStructure compoundStructure = ceilingType.GetCompoundStructure();
            if (compoundStructure == null)
                return 0;

            // Get materials from compound structure layers
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();
            if (layers == null || layers.Count == 0)
                return 0;

            // Find the first layer with a valid material

            Material material = doc.GetElement(layers.Last().MaterialId) as Material;

            if (material == null)
                return 0;

            // Get the surface pattern ID
            ElementId patternId = material.SurfaceForegroundPatternId;
            if (patternId == ElementId.InvalidElementId)
                return 0;

            // Get the fill pattern element
            FillPatternElement patternElement = doc.GetElement(patternId) as FillPatternElement;
            if (patternElement == null)
                return 0;

            FillPattern fillPattern = patternElement.GetFillPattern();
            if (fillPattern.Target != FillPatternTarget.Model)
                return 0; // Only process model patterns

            // Get fill grids
            var grids = fillPattern.GetFillGrids();
            if (grids.Count == 0)
                return 0;

            // Find the grid with the largest Shift
            var dominantGrid = grids.OrderByDescending(g => g.Offset).First();

            // Get and normalize the angle
            double angleInRadians = NormalizeAngle(dominantGrid.Angle);

            // OPTIONAL: Snap to 0 or PI/2 for horizontal/vertical
            if (IsApproximatelyEqual(angleInRadians, 0) || IsApproximatelyEqual(angleInRadians, Math.PI))
                return 0;
            else if (IsApproximatelyEqual(angleInRadians, Math.PI / 2) || IsApproximatelyEqual(angleInRadians, 3 * Math.PI / 2))
                return Math.PI / 2;
            else
                return angleInRadians;
        }

        // Normalize angle between 0 and 2*PI
        private static double NormalizeAngle(double angle)
        {
            while (angle < 0)
                angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI)
                angle -= 2 * Math.PI;
            return angle;
        }

        // Helper to compare doubles with tolerance
        private static bool IsApproximatelyEqual(double a, double b, double tolerance = 0.01)
        {
            return Math.Abs(a - b) < tolerance;
        }

    }
}
