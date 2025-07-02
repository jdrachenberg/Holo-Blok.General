#region Namespaces
using Transform = Autodesk.Revit.DB.Transform;

#endregion

namespace HoloBlok.Tools.LightFixtures
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

        public (Face face, RevitLinkInstance linkInstance) GetLowestHostFace(XYZ location, LinkedRoomData room)
        {
            var fixtureExtents = GetFixtureExtents(_fixtureType);
            var testPoints = GenerateTestPoints(location, fixtureExtents);
            RevitLinkInstance bestLinkInstance = null;

            double lowestIntersection = double.MaxValue; //TO-DO: VERIFY THAT TEST POINTS ARE CORRECT
            Face lowestFace = null;

            foreach (var testPoint in testPoints)
            {
                var (face, height, linkinstance) = GetHostFaceAndHeight(testPoint, room);
                if (face != null && height < lowestIntersection)
                {
                    lowestIntersection = height;
                    lowestFace = face;
                    bestLinkInstance = linkinstance;
                }
            }

            return (lowestFace, bestLinkInstance);
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

        private (Face face, double height, RevitLinkInstance linkInstance) GetHostFaceAndHeight(XYZ point, LinkedRoomData roomData)
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

            Face lowestFace = null;
            double lowestZ = double.MaxValue;
            RevitLinkInstance matchingLinkInstance = null;
            
            if (nearestRef != null)
            {
                Reference reference = nearestRef.GetReference();

                RevitLinkInstance linkInstance = _hostDoc.GetElement(reference.ElementId) as RevitLinkInstance;
                if (linkInstance == null) return (null, double.MaxValue, null);

                Document linkedDoc = linkInstance.GetLinkDocument();
                Element linkedElement = linkedDoc.GetElement(reference.LinkedElementId);
                if (linkedElement == null) return (null, double.MaxValue, null);

                Options geomOptions = new Options { ComputeReferences = true };
                GeometryElement geomElement = linkedElement.get_Geometry(geomOptions);
                Transform linkTransform = linkInstance.GetTransform();

                foreach (GeometryObject geomObj in geomElement)
                {
                    Solid solid = geomObj as Solid;
                    if (solid == null || solid.Faces.IsEmpty) continue;

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
                                    matchingLinkInstance = linkInstance;
                                }
                            }
                            
                        }
                    }
                }
            }

            return (lowestFace, lowestZ, matchingLinkInstance);
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
    }
}
