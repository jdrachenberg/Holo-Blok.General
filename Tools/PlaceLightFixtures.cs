#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HoloBlok.Common.Utils.RevitElements.Doors;
using HoloBlok.Common.Utils.RevitElements.Elements;
using HoloBlok.Common.Utils.RevitElements.Tags;
using HoloBlok.Utils;
using HoloBlok.Utils.Families;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.RevitElements.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using static HoloBlok.ProgressManager;
using Creation = Autodesk.Revit.Creation;
using Line = Autodesk.Revit.DB.Line;
using Transform = Autodesk.Revit.DB.Transform;

#endregion

namespace HoloBlok
{
    
    public static class PlacementConstants
    {
        public const double WALL_OFFSET_FEET = 2.0;
        public const double DEFAULT_HEIGHT_FEET = 6.0;
        public const int BATCH_SIZE = 10;
    }
    [Transaction(TransactionMode.Manual)]
    public class PlaceLightFixtures : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;

            // this is a variable for the current Revit model
            Document doc = uiapp.ActiveUIDocument.Document;

            //Get current view
            View currentView = doc.ActiveView;

            //Start transaction
            try
            {
                // 1. Get linked models (architectural and structural)
                List<RevitLinkInstance> linkedArchModels = GetArchitecturalLinkedModels(doc);
                List<RevitLinkInstance> linkedStrucModels = GetStructuralLinkedModels(doc);
                List<RevitLinkInstance> allLinkedModels = linkedArchModels.Concat(linkedStrucModels).ToList();

                if (!linkedArchModels.Any())
                {
                    TaskDialog.Show("Error", "No architectural or structural linked models found.");
                    return Result.Failed;
                }

                // 2. Get rooms from linked model
                var roomSelector = new LinkedRoomSelector(linkedArchModels.First());
                List<LinkedRoomData> selectedRooms = roomSelector.SelectRooms(); // TO-DO: Create options for selecting rooms

                // 3. Get light fixture family type - ENTER CORRECT FIXTURE NAMES
                var fixtureType = GetLightFixtureType(doc, "Ceiling Light - Flat Round", "60W - 230V");
                if (fixtureType == null)
                {
                    TaskDialog.Show("Error", "No light fixture family type found.");
                    return Result.Failed;
                }

                // 4. Get spacing configuration
                var spacingConfig = new GridSpacingConfiguration(4.0, 2.0); // 8 feet default


                // 5. Process rooms in batches
                var placementengine = new LightFixturePlacementEngine(doc, allLinkedModels, fixtureType);
                var progressManager = new ProgressManager(selectedRooms.Count);

                using (Transaction t = new Transaction(doc, "Place Light Fixtures"))
                {
                    t.Start();

                    foreach (var roomBatch in selectedRooms.Batch(PlacementConstants.BATCH_SIZE))
                    {
                        var results = placementengine.PlaceFixturesInRooms(roomBatch, spacingConfig, progressManager);

                        // Handle any errors or warnings
                        if (results.HasErrors)
                        {
                            // TO-DO: Implement error handling
                        }
                    }

                    t.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        

        private List<RevitLinkInstance> GetArchitecturalLinkedModels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsArchitectural(link))
                .ToList();
        }

        private bool IsArchitectural(RevitLinkInstance link)
        {
            string linkName = link.Name.ToLower();
            return linkName.Contains("arch") && !linkName.Contains("struc");
        }


        private List<RevitLinkInstance> GetStructuralLinkedModels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsStructural(link))
                .ToList();
        }

        private bool IsStructural(RevitLinkInstance link)
        {
            string linkName = link.Name.ToLower();
            return linkName.Contains("struc") && !linkName.Contains("arch");
        }

        private FamilySymbol GetLightFixtureType(Document doc, string familyName, string typeName)
        {
            // Get first available light fixture type
            // In future, this could be a user selection dialog
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_LightingFixtures)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.FamilyName == familyName && fs.Name == typeName);
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }


    }

    

    

    public class LinkedRoomSelector
    {
        private readonly RevitLinkInstance _linkedModel;

        public LinkedRoomSelector(RevitLinkInstance linkedModel)
        {
            _linkedModel = linkedModel;
        }

        public List<LinkedRoomData> SelectRooms()
        {
            var rooms = new List<LinkedRoomData>();

                Document linkDoc = _linkedModel.GetLinkDocument();
                if (linkDoc == null) return null;

                var linkRooms = new FilteredElementCollector(linkDoc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .Cast<Room>()
                    .Where(r => r.Area > 0) // Valid rooms only
                    .Select(r => new LinkedRoomData(r, _linkedModel))
                    .ToList();

                rooms.AddRange(linkRooms);

            return rooms;
        }

        private bool IsArchitectural(RevitLinkInstance link)
        {
            string linkName = link.Name.ToLower();
            return linkName.Contains("arch");
        }

        // Future method for filtering by department, name, etc.
        public List<LinkedRoomData> FilterByDepartment(List<LinkedRoomData> rooms, string department)
        {
            return rooms.Where(r => r.Department == department).ToList();
        }
    }

    public class LinkedRoomData
    {
        public Room Room { get; }
        public RevitLinkInstance LinkInstance { get; }
        public Transform LinkTransform { get; }

        public string Name => Room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";
        public string Number => Room.get_Parameter(BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";
        public string Department => Room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT)?.AsString() ?? "";

        public LinkedRoomData(Room room, RevitLinkInstance linkInstance)
        {
            Room = room;
            LinkInstance = linkInstance;
            LinkTransform = linkInstance.GetTotalTransform();
        }

        public List<XYZ> GetBoundaryPointsInHostCoordinates()
        {
            var boundaries = Room.GetBoundarySegments(new SpatialElementBoundaryOptions());
            var points = new List<XYZ>();

            foreach (var loop in boundaries)
            {
                foreach (var segment in loop)
                {
                    var curve = segment.GetCurve();
                    points.Add(LinkTransform.OfPoint(curve.GetEndPoint(0)));
                }
            }

            return points;
        }
    }

    public interface ISpacingStrategy
    {
        List<XYZ> CalculateFixtureLocations(LinkedRoomData room, double wallOffset);
    }

    internal class GridSpacingConfiguration : ISpacingStrategy
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

        public Face GetLowestHostFace(XYZ location, LinkedRoomData room)
        {
            var fixtureExtents = GetFixtureExtents(_fixtureType);
            var testPoints = GenerateTestPoints(location, fixtureExtents);

            double lowestIntersection = double.MaxValue; //TO-DO: VERIFY THAT TEST POINTS ARE CORRECT
            Face lowestFace = null;

            foreach (var testPoint in testPoints)
            {
                var (face, height) = GetHostFaceAndHeight(testPoint, room);
                if (face != null && height < lowestIntersection)
                {
                    lowestIntersection = height;
                    lowestFace = face;
                }
            }

            return lowestFace;
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

        private (Face face, double height) GetHostFaceAndHeight(XYZ point, LinkedRoomData roomData)
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
            
            if (nearestRef != null)
            {
                Reference reference = nearestRef.GetReference();

                RevitLinkInstance linkInstance = _hostDoc.GetElement(reference.ElementId) as RevitLinkInstance;
                if (linkInstance == null) return (lowestFace, lowestZ);

                Document linkedDoc = linkInstance.GetLinkDocument();
                Element linkedElement = linkedDoc.GetElement(reference.LinkedElementId);
                if (linkedElement == null) return (lowestFace, lowestZ);

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
                                }
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
    }

    internal class LightFixturePlacementEngine
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
            var locations = spacingStrategy.CalculateFixtureLocations(roomData, PlacementConstants.WALL_OFFSET_FEET); //TO-DO: Replace with user-selected value

            // Build list of valid placement data
            var placementData = new List<Creation.FamilyInstanceCreationData>();
            var level = _doc.GetElement(roomData.Room.LevelId) as Level;

            foreach (var location in locations)
            {
                // Check for duplicates
                if (_duplicateChecker.IsDuplicateLocation(location))
                {
                    result.SkippedCount++;
                    continue;
                }

                // Get host element
                Face hostFace = _heightCalculator.GetLowestHostFace(location, roomData);
                var placementPoint = new XYZ(location.X, location.Y, location.Z);

                // Create placement data for batch operation
                var creationData = new Creation.FamilyInstanceCreationData(
                    hostFace,
                    placementPoint,
                    XYZ.BasisX,
                    _fixtureType);

                placementData.Add(creationData);
            }
            // Batch place all fixtures
            if (placementData.Any())
            {
                try
                {
                    ICollection<ElementId> createdIds = _doc.Create.NewFamilyInstances2(placementData);

                    result.PlacedCount = createdIds.Count;

                    int failedCount = placementData.Count - createdIds.Count;
                    if (failedCount > 0)
                    {
                        result.Errors.Add($"{failedCount} fixtures failed to place in room {roomData.Number}");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Batch placement failed: {ex.Message}");
                }
            }

            return result;
        }
    }

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

    public class ProgressManager
    {
        private readonly int _totalItems;
        private int _processedItems;

        public ProgressManager(int totalItems)
        {
            _totalItems = totalItems;
            _processedItems = 0;
        }

        public void ReportProgress()
        {
            _processedItems++;
            // TO-DO: Implement progress UI or logging
            Debug.WriteLine($"Progress: {_processedItems}/{_totalItems}");
        }
    }

    // Helper classes for results and progress
    public class PlacementResults
    {
        public List<RoomPlacementResult> RoomResults { get; } = new List<RoomPlacementResult>();
        public List<string> Errors { get; } = new List<string>();
        public bool HasErrors => Errors.Any();
    }

    public class RoomPlacementResult
    {
        public string RoomNumber { get; set; }
        public int PlacedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; } = new List<string>();
    }
}

public static class EnumerableExtensions
{
    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        var batch = new List<T>(batchSize);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }
        if (batch.Any())
            yield return batch;
    }
}