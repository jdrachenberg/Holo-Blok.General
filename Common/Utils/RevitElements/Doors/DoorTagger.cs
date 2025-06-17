using HoloBlok.Common.Utils.RevitElements.Elements;
using HoloBlok.Common.Utils.RevitElements.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Common.Utils.RevitElements.Doors
{
    internal static class DoorTagger
{
        internal static TaggingResult TagAllDoorsInView(Document doc, View view)
        {
            FilteredElementCollector doorCollector = HBCollectors.GetDoorsInView(doc, view);
            int scale = view.Scale;

            if (!doorCollector.Any())
                return new TaggingResult { Success = false, ErrorMessage = "No doors found in current view" };

            var doorTagType = GetDefaultDoorTagType(doc);
            if (doorTagType == null)
                return new TaggingResult { Success = false, ErrorMessage = "No door tag type found" };

            // Get all tags with their tagged elements
            var existingTags = GetAllTagsWithElements(doc, view);

            int taggedCount = 0;
            int updatedCount = 0;
            var failedDoors = new List<string>();

            foreach (FamilyInstance door in doorCollector)
            {
                try
                {
                    // Check if door is already tagged
                    if (existingTags.TryGetValue(door.Id, out IndependentTag existingTag))
                    {
                        // Door is already tagged - check if it needs updating
                        if (UpdateExistingTag(doc, view, door, existingTag, doorTagType))
                        {
                            updatedCount++;
                        }
                    }
                    else
                    {
                        // Door is not tagged - create new tag
                        if (CreateNewTag(doc, view, door, doorTagType) != null)
                            taggedCount++;
                    }
                }
                catch (Exception ex)
                {
                    failedDoors.Add($"Door ID {door.Id}: {ex.Message}");
                }
            }

            return new TaggingResult
            {
                Success = true,
                TaggedCount = taggedCount,
                UpdatedCount = updatedCount,
                FailedItems = failedDoors
            };
        }

        private static Dictionary<ElementId, IndependentTag> GetAllTagsWithElements(Document doc, View view)
        {
            var tagsByElement = new Dictionary<ElementId, IndependentTag>();

            var tags = new FilteredElementCollector(doc, view.Id)
                .OfCategory(BuiltInCategory.OST_DoorTags)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>();

            foreach (var tag in tags)
            {
                foreach (var elementId in tag.GetTaggedLocalElementIds())
                {
                    // Store only the first tag if multiple tags exist for same element
                    if (!tagsByElement.ContainsKey(elementId))
                    {
                        tagsByElement[elementId] = tag;
                    }
                }
            }

            return tagsByElement;
        }

        /// <summary>
        /// Common method to calculate tag position and rotation data
        /// </summary>
        private static TagPositionResult CalculateTagData(Document doc, View view, FamilyInstance door, ElementId tagTypeId)
        {
            Element doorPanel = GetNestedDoorPanel(doc, door);
            XYZ panelLocation = GetFamilyLocationPoint(doorPanel as FamilyInstance);

            if (panelLocation == null)
                throw new InvalidOperationException("Could not determine door panel location");

            return DoorTagPositionCalculator.CalculateTagPositionWithSwingData(doc, view, door, panelLocation, tagTypeId);
        }

        /// <summary>
        /// Creates a new tag for an untagged door
        /// </summary>
        private static IndependentTag CreateNewTag(Document doc, View view, FamilyInstance door, ElementId tagTypeId)
        {
            var tagResult = CalculateTagData(doc, view, door, tagTypeId);

            // Place the tag
            IndependentTag tag = PlaceTag(doc, view, door, tagResult.Position);

            // Apply rotation
            ApplyTagRotation(tag, door, tagResult);

            return tag;
        }

        /// <summary>
        /// Updates an existing tag if its position or rotation has changed
        /// </summary>
        private static bool UpdateExistingTag(Document doc, View view, FamilyInstance door, IndependentTag existingTag, ElementId tagTypeId)
        {
            var tagResult = CalculateTagData(doc, view, door, tagTypeId);

            // Get current tag state
            XYZ currentTagPosition = existingTag.TagHeadPosition;
            double currentRotation = existingTag.RotationAngle;

            // Calculate expected final position and rotation
            XYZ expectedFinalPosition = CalculateFinalTagPosition(door, tagResult);
            double expectedRotation = CalculateTotalRotation(door, tagResult);

            // Check if position or rotation needs updating
            const double positionTolerance = 0.001; // 1mm tolerance
            bool positionChanged = !currentTagPosition.IsAlmostEqualTo(expectedFinalPosition, positionTolerance);
            bool rotationChanged = Math.Abs(currentRotation - expectedRotation) > 0.001; // ~0.057 degrees

            if (positionChanged || rotationChanged)
            {
                // Reset to initial state before applying new transformations
                ResetTagRotation(existingTag);

                // Set initial position
                existingTag.TagHeadPosition = tagResult.Position;

                // Apply all rotations
                ApplyTagRotation(existingTag, door, tagResult);

                return true;
            }

            return false;
        }

        private struct RotationData
        {
            public double HostRotation { get; set; }
            public double InitialRotation { get; set; }
            public double SwingRotation { get; set; }
            public bool HasSwingRotation { get; set; }
        }

        private static RotationData getRotationData(FamilyInstance door, TagPositionResult tagResult)
        {
            Element host = door.Host;
            var data = new RotationData
            {
                HostRotation = HBElementUtils.GetRotation(host),
                InitialRotation = 0,
                SwingRotation = 0,
                HasSwingRotation = false
            };

            if (tagResult.SwingType == "Hinged" || tagResult.SwingType == "Pivot")
            {
                data.InitialRotation = 90;
                data.SwingRotation = GetSwingRotation(tagResult.SwingAngle, door.Mirrored);
                data.HasSwingRotation = Math.Abs(data.SwingRotation) > 0.001;
            }

            return data;
        }

        private static double GetSwingRotation(double swingAngle, bool isMirrored)
        {
            switch (swingAngle)
            {
                case 45:
                    return !isMirrored ? 45 : -45;
                case 135:
                    return !isMirrored ? -45 : 45;
                case 180:
                    return !isMirrored ? -90 : 90;
                default:
                    return 0;
            }
        }

        private static void ApplyTagRotation(IndependentTag tag, FamilyInstance door, TagPositionResult tagResult)
        {
            var rotations = getRotationData(door, tagResult);

            // Apply host rotation
            RotateTag(tag, tag.TagHeadPosition, rotations.HostRotation);

            // Apply swing-specific rotations
            if (rotations.InitialRotation != 0)
            {
                RotateTag(tag, tagResult.Position, rotations.InitialRotation);

                if (rotations.HasSwingRotation)
                    RotateTag(tag, tagResult.HingePoint, rotations.SwingRotation);
            }
        }

        private static XYZ CalculateFinalTagPosition(FamilyInstance door, TagPositionResult tagResult)
        {
            var rotations = getRotationData(door, tagResult);
            XYZ position = tagResult.Position;

            // Apply swing-specific rotations
            if (rotations.InitialRotation != 0 && rotations.HasSwingRotation)
                position = RotatePointAroundPoint(position, tagResult.HingePoint, rotations.SwingRotation);

            return position;
        }

        /// <summary>
        /// Rotates a point around another point by the specified angle in degrees
        /// </summary>
        private static XYZ RotatePointAroundPoint(XYZ pointToRotate, XYZ rotationCenter, double angleDegrees)
        {
            double angleRadians = UnitUtils.ConvertToInternalUnits(angleDegrees, UnitTypeId.Degrees);

            // Create rotation transform
            Transform rotation = Transform.CreateRotationAtPoint(XYZ.BasisZ, angleRadians, rotationCenter);

            // Apply rotation
            return rotation.OfPoint(pointToRotate);
        }

        /// <summary>
        /// Calculates the total expected rotation in radians
        /// </summary>
        private static double CalculateTotalRotation(FamilyInstance door, TagPositionResult tagResult)
        {
            Element host = door.Host;
            double hostRotation = HBElementUtils.GetRotation(host);
            double totalRotation = hostRotation;

            // Add rotation based on swing type
            if (tagResult.SwingType == "Hinged" || tagResult.SwingType == "Pivot")
            {
                totalRotation += 90;

                bool isMirrored = door.Mirrored;
                if (tagResult.SwingAngle == 45)
                    totalRotation += !isMirrored ? 45 : -45;
                else if (tagResult.SwingAngle == 135)
                    totalRotation += !isMirrored ? -45 : 45;
                else if (tagResult.SwingAngle == 180)
                    totalRotation += !isMirrored ? -90 : 90;
            }

            // Convert to radians
            return UnitUtils.ConvertToInternalUnits(totalRotation, UnitTypeId.Degrees);
        }

        private static double GetTagRotation(IndependentTag tag)
        {
            // Get the tag's current rotation
            if (tag.Location is LocationPoint locPoint)
            {
                return locPoint.Rotation;
            }
            return 0.0;
        }

        private static void ResetTagRotation(IndependentTag tag)
        {
            // Get current rotation and rotate back to zero
            double currentRotation = tag.RotationAngle;
            if (Math.Abs(currentRotation) > 0.001)
            {
                XYZ tagPosition = tag.TagHeadPosition;
                Line axis = Line.CreateBound(tagPosition, tagPosition + XYZ.BasisZ);
                tag.Location.Rotate(axis, -currentRotation);
            }
        }

        /// <summary>
        /// Gets planswing string from element ID
        /// </summary>
        private static string GetPlanSwingString(Document doc, ElementId planSwingId)
        {
            if (planSwingId == null)
                return null;

            var element = doc.GetElement(planSwingId) as NestedFamilyTypeReference;
            return element?.FamilyName;
        }

        private static IndependentTag PlaceTag(Document doc, View view, FamilyInstance family, XYZ insertionPoint)
        {
            var doorRef = new Reference(family);
            var tag = IndependentTag.Create(
                doc,
                view.Id,
                doorRef,
                false,
                TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.AnyModelDirection,
                insertionPoint
                );
            return tag;
        }

        private static IndependentTag RotateTag(IndependentTag tag, XYZ rotationPoint, double rotationDegrees)
        {
            double tagRotationRadians = UnitUtils.ConvertToInternalUnits(rotationDegrees, UnitTypeId.Degrees);
            XYZ rotationAxis = XYZ.BasisZ;
            Line axis = Line.CreateBound(rotationPoint, rotationPoint + rotationAxis);

            //Rotate the tag
            bool tagRotated = tag.Location.Rotate(axis, tagRotationRadians);
            return tag;
        }

        public static Element GetNestedDoorPanel(Document doc, FamilyInstance door)
        {
            var panelIds = door.GetSubComponentIds();
            XYZ basisX = door.GetTransform().BasisX;

            Element furthestRight = null;
            double maxProjection = double.MinValue;

            //TEST LOGIC
            foreach (var panelId in panelIds)
            {
                var panel = doc.GetElement(panelId);
                XYZ location = (panel.Location as LocationPoint).Point;
                double projection = location.DotProduct(basisX);

                if (projection > maxProjection)
                {
                    maxProjection = projection;
                    furthestRight = panel;
                }
            }

            return furthestRight;
        }

        public static XYZ GetFamilyLocationPoint(FamilyInstance famInstance)
        {
            if (famInstance.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point;
            }
            return null;
        }

        private static HashSet<ElementId> GetAllTaggedElementIds(Document doc, View view)
        {
            var taggedIds = new HashSet<ElementId>();

            var tags = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>();

            foreach (var tag in tags)
            {
                foreach (var elementId in tag.GetTaggedLocalElementIds())
                {
                    taggedIds.Add(elementId);
                }
            }

            return taggedIds;
        }

        private static bool IsDoorAlreadyTagged(HashSet<ElementId> taggedIds, Element door)
        {
            return taggedIds.Contains(door.Id);
        }

        public static ElementId GetDefaultDoorTagType(Document doc)
        {
            var doorTagType = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_DoorTags)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .OrderByDescending(x => x.Id.IntegerValue) // Higher IDs = more recently loaded
                .FirstOrDefault();
            return doorTagType?.Id;
        }

        public class TaggingResult
        {
            public bool Success { get; set; }
            public int TaggedCount { get; set; }
            public int UpdatedCount { get; set; }
            public string ErrorMessage { get; set; }
            public List<string> FailedItems { get; set; } = new List<string>();
        }

    }
}
