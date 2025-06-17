#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HoloBlok.Common.Utils.RevitElements.Elements;
using HoloBlok.Common.Utils.RevitElements.Tags;
using HoloBlok.Utils;
using HoloBlok.Utils.Families;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Shapes;
using Line = Autodesk.Revit.DB.Line;
using Transform = Autodesk.Revit.DB.Transform;

#endregion

/* FEATURES:
 *  Dialog box if no door tag is loaded
 *  Choose door tag type
 *  
 * */

namespace HoloBlok
{
    [Transaction(TransactionMode.Manual)]
    public class TagDoors : IExternalCommand
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
            using (Transaction t = new Transaction(doc))
            {
                t.Start("Renumber doors");

                TagAllDoorsInView(doc, currentView);

                t.Commit();
            }

            return Result.Succeeded;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }

        private TaggingResult TagAllDoorsInView(Document doc, View view)
        {
            FilteredElementCollector doorCollector = HBCollectors.GetDoorsInView(doc, view);
            int scale = view.Scale;

            if (!doorCollector.Any())
                return new TaggingResult { Success = false, ErrorMessage = "No doors found in current view" };

            var doorTagType = GetDefaultDoorTagType(doc);
            if (doorTagType == null)
                return new TaggingResult { Success = false, ErrorMessage = "No door tag type found" };

            var alreadyTaggedIds = GetAllTaggedElementIds(doc, view);

            int taggedCount = 0;
            var failedDoors = new List<string>();

            var untaggedDoors = doorCollector.Where(door => !alreadyTaggedIds.Contains(door.Id));

            foreach (FamilyInstance door in untaggedDoors)
            {
                try
                {
                    if (CreateAlignedDoorTag(doc, view, door, doorTagType))
                        taggedCount++;
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
                FailedItems = failedDoors
            };
        }

        public bool CreateAlignedDoorTag(Document doc, View view, FamilyInstance door, ElementId tagTypeId)
        {
            Element doorPanel = GetNestedDoorPanel(doc, door);
            XYZ panelLocation = GetFamilyLocationPoint(doorPanel as FamilyInstance);
            ElementId planSwingId = ArchSmarterUtils.Parameters.GetParameterByName(door, "Plan Swing")?.AsElementId();
            string planSwingString = GetPlanSwingString(doc, planSwingId);
            var doorTagPositionCalculator = new DoorTagPositionCalculator();

            var tagResult = doorTagPositionCalculator.CalculateTagPositionWithSwingData(doc, view, door, panelLocation, tagTypeId);

            if (panelLocation == null) return false;
            IndependentTag tag = PlaceTag(doc, view, door, tagResult.Position);
            var rotation = RotateTag(tag, door, tagResult.Position, tagResult.HingePoint, tagResult.SwingType, tagResult.SwingAngle);

            return tag != null;
        }

        /// <summary>
        /// Gets planswing string from element ID
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="planSwingId"></param>
        /// <returns></returns>
        private string GetPlanSwingString(Document doc, ElementId planSwingId)
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

        public IndependentTag RotateTag(IndependentTag tag, FamilyInstance taggedFamily, XYZ tagPlacementPoint, XYZ hingePoint, string swingType, int swingAngle)
        {
            IndependentTag tagRotatedToOrientaiton = RotateToWallOrientation(tag, taggedFamily, tagPlacementPoint);
            bool isMirrored = taggedFamily.Mirrored;
            
            if (swingType == "Hinged" || swingType == "Pivot")
            {
                RotateTag(tag, tagPlacementPoint, 90);
                if (swingAngle == 45)
                    RotateTag(tag, hingePoint, !isMirrored ? 45 : -45);
                if (swingAngle == 135)
                    RotateTag(tag, hingePoint, !isMirrored ? -45 : 45);
                if (swingAngle == 180)
                    RotateTag(tag, hingePoint, !isMirrored ? -90 : 90);
            }

            return tag;
        }

        private static IndependentTag RotateToWallOrientation(IndependentTag tag, FamilyInstance taggedFamily, XYZ rotationPoint)
        {
            Element host = taggedFamily.Host;
            double hostRotation = HBElementUtils.GetRotation(host);

            XYZ tagLocation = tag.TagHeadPosition;

            //Create a rotation axis (Z-axis through the tag's location
            return RotateTag(tag, tagLocation, hostRotation);
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

            //var panelId = door.GetSubComponentIds().FirstOrDefault();
            return furthestRight;
        }

        public XYZ GetFamilyLocationPoint(FamilyInstance famInstance)
        {
            if (famInstance.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point;
            }
            return null;
        }

        private HashSet<ElementId> GetAllTaggedElementIds(Document doc, View view)
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

        private bool IsDoorAlreadyTagged(HashSet<ElementId> taggedIds, Element door)
        {
            return taggedIds.Contains(door.Id);
        }

        public ElementId GetDefaultDoorTagType(Document doc)
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
            public string ErrorMessage { get; set; }
            public List<string> FailedItems { get; set; } = new List<string>();
        }
        
    }
}
