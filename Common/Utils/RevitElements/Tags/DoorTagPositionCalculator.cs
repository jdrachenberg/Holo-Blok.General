using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HoloBlok.Common.Utils.Geometry;
using HoloBlok.Common.Utils.RevitElements.Elements;
using HoloBlok.Utils.Geometry;
using Panel = Autodesk.Revit.DB.Panel;

/*
 * Implementation:
 * - For doors that are tagged, update tag location if it has changed
 * - Update tag rotation if it has changed
 
 * - Adjust tags for non-hinge doors to align with edge of door at scales smaller than 1-50
 * - Warning if no door tag loaded
 * - Fine tune tag placement to avoid swing clashes
 * - Select door tag type
 * - Stacked wall functionality
 * - Adjust tag location for barn doors
 * - Section view integration

 */

namespace HoloBlok.Common.Utils.RevitElements.Tags
{
    internal class DoorTagPositionCalculator
    {
        //Constants
        private const double BUFFER_MM = 40;
        private const double TAG_WIDTH_RATIO = 0.9;
        private const double TAG_HEIGHT_MULTIPLIER = 0.5;
        private const string CURTAIN_WALL_FAMILY = "Curtain Wall";
        private const string BASIC_WALL_FAMILY = "Basic Wall";
        private const string HINGED_SWING = "Hinged";
        private const string PIVOT_SWING = "Pivot";
        private const string BIFOLD_SWING = "Bifold";
        private const string SLIDING_SWING = "Sliding";
        private const string DOUBLE_DOOR_INDICATOR = "Double";
        private const string UNEQUAL_DOOR_INDICATOR = "Unequal";

        //Data structures
        public struct TagPositionResult
        {
            public XYZ Position { get; set; }
            public XYZ HingePoint { get; set; }
            public string SwingType { get; set; }
            public int SwingAngle { get; set; }
        }
        
        public struct DoorData
        {
            public Transform Transform { get; set; }
            public double TotalDoorWidth { get; set; }
            public double DoorPanelWidth { get; set; }
            public double DoorThickness { get; set; }
            public double FrameDepth { get; set; }
            public double HostWallWidth { get; set; }
            public double PivotOffset { get; set; }
            public string SwingType { get; set; }
            public int SwingAngle { get; set; }
            public bool IsMirrored { get; set; }
            public bool HasFixedFrame { get; set; }
            public XYZ DoorLocation { get; set; }
            public string DoorFamilyName { get; set; }
        }

        public struct TagData
        {
            public double Width { get; set; }
            public double Height { get; set; }
        }

        public struct GeometricVectors
        {
            public XYZ XVector { get; set; }
            public XYZ YVector { get; set; }
        }

        /// <summary>
        /// Calculates the optimal position for placing a door tag in a Revit view
        /// </summary>
        public TagPositionResult CalculateTagPositionWithSwingData(Document doc, View view, FamilyInstance door, XYZ panelLocation, ElementId tagTypeId)
        {
            var doorData = ExtractDoorData(door, doc);
            var tagData = ExtractTagData(doc.GetElement(tagTypeId) as FamilySymbol, view.Scale);

            var vectors = new GeometricVectors
            {
                XVector = doorData.Transform.BasisX,
                YVector = doorData.Transform.BasisY
            };

            XYZ hingePoint = CalculateHingePoint(doorData, vectors, panelLocation);
            XYZ tagPosition = CalculatePositionBySwingType(doorData, tagData, vectors, hingePoint, view);

            return new TagPositionResult
            {
                Position = tagPosition,
                HingePoint = hingePoint,
                SwingType = doorData.SwingType,
                SwingAngle = doorData.SwingAngle
            };
        }

        private DoorData ExtractDoorData(FamilyInstance door, Document doc)
        {
            FamilySymbol doorSymbol = door.Symbol;
            Wall hostWall = door.Host as Wall;
            WallType hostWallType = hostWall.WallType;
            string hostWallFamily = hostWallType.FamilyName;

            ElementId planSwingId = ArchSmarterUtils.Parameters.GetParameterByName(door, "Plan Swing")?.AsElementId();
            string planSwingString = GetPlanSwingString(doc, planSwingId);
            (string swingType, int swingAngle) = ParseSwingType(planSwingString);


            return new DoorData
            {
                Transform = door.GetTransform(),
                TotalDoorWidth = GetDoorWidth(hostWallFamily, doorSymbol, door),
                DoorPanelWidth = GetDoorPanelWidth(hostWallFamily, doorSymbol, door),
                DoorThickness = doorSymbol.GetParameterAsDouble("Thickness"),
                FrameDepth = GetFrameDepth(doorSymbol, hostWallFamily),
                PivotOffset = doorSymbol.GetParameterAsDouble("Pivot Offset"),
                HostWallWidth = GetHostWallWidth(hostWallType, hostWallFamily),
                SwingType = swingType,
                SwingAngle = swingAngle,
                IsMirrored = door.Mirrored,
                HasFixedFrame = HasFixedFrame(hostWallFamily, doorSymbol),
                DoorLocation = GetDoorLocation(doc, hostWallFamily, door),
                DoorFamilyName = doorSymbol.FamilyName
            };
        }

        private XYZ GetDoorLocation(Document doc, string hostWallFamily, FamilyInstance door)
        {
            if (hostWallFamily == CURTAIN_WALL_FAMILY)
            {
                return GetCurtainDoorLocationXYZ(door);
            }
            return (door.Location as LocationPoint).Point;
        }

        private static XYZ GetCurtainDoorLocationXYZ(FamilyInstance door)
        {
            Wall curtainWall = door.Host as Wall;
            if (curtainWall != null)
            {
                CurtainCell curtainCell = GetCurtainCell(door, curtainWall);
                return GetCurtainCellCenterPoint(curtainCell);
            }
            return null;
        }

        private static CurtainCell GetCurtainCell(FamilyInstance door, Wall curtainWall)
        {
            ElementId gridUId = null;
            ElementId gridVId = null;

            CurtainGrid curtainGrid = curtainWall.CurtainGrid;
            if (curtainGrid != null)
            {
                ICollection<ElementId> gridUIds = curtainGrid.GetUGridLineIds();
                gridUIds.Add(ElementId.InvalidElementId);
                ICollection<ElementId> gridVIds = curtainGrid.GetVGridLineIds();
                gridVIds.Add(ElementId.InvalidElementId);

                foreach (ElementId curGridUId in gridUIds)
                {
                    foreach (ElementId curGridVId in gridVIds)
                    {
                        ElementId doorId = door.Id;
                        ElementId panelId = curtainGrid.GetPanel(curGridUId, curGridVId).Id;

                        if (panelId == door.Id)
                        {
                            gridUId = curGridUId;
                            gridVId = curGridVId;
                            break;
                        }
                    }
                }
            }
            return curtainGrid.GetCell(gridUId, gridVId);
        }

        private static XYZ GetCurtainCellCenterPoint(CurtainCell curtainCell)
        {
            var lineList = GetVerticalLineList(curtainCell);
            var lineEndXYZList = GetLineEndPoints(lineList);
            
            return GetAverageXYZFromList(lineEndXYZList);
        }

        private static List<Line> GetVerticalLineList(CurtainCell curtainCell)
        {
            CurveArrArray curveArrArray = curtainCell.PlanarizedCurveLoops;
            XYZ zVector = XYZ.BasisZ;
            List<Line> zLineList = new List<Line>();


            foreach (CurveArray curveArray in curveArrArray)
            {
                foreach (Curve curve in curveArray)
                {
                    Line line = curve as Line;
                    if (line != null)
                    {
                        if (line.Direction.Absolute().IsAlmostEqualTo(zVector))
                        {
                            zLineList.Add(line);
                        }
                    }
                }
            }
            return zLineList;
        }

        private static List<XYZ> GetLineEndPoints(List<Line> lineList)
        {
            List<XYZ> lineEndPointsList = new List<XYZ>();
            foreach (var line in lineList)
            {
                lineEndPointsList.Add(line.GetEndPoint(0));
            }
            return lineEndPointsList;
        }

        private static XYZ GetAverageXYZFromList(List<XYZ> lineEndXYZList)
        {
            XYZ totalXyz = new XYZ(0, 0, 0);
            foreach (XYZ xyz in lineEndXYZList)
            {
                totalXyz += xyz;
            }
            return totalXyz / lineEndXYZList.Count;
        }

        private XYZ GetBoundingBoxCentroid(BoundingBoxXYZ bbox)
        {
            return bbox != null
                ? (bbox.Min + bbox.Max) / 2
                : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hostWallFamily"></param>
        /// <param name="doorSymbol"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private double GetDoorPanelWidth(string hostWallFamily, FamilySymbol doorSymbol, FamilyInstance door)
        {
            if (hostWallFamily == CURTAIN_WALL_FAMILY)
            {
                return AdjustedCurtainPanelWidth(doorSymbol, door);
            }
            return doorSymbol.GetParameterAsDouble("Door Panel A Width");
        }

        /// <summary>
        /// Adjusts panel width based on curtain door type
        /// </summary>
        /// <param name="doorSymbol"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        private double AdjustedCurtainPanelWidth(FamilySymbol doorSymbol, FamilyInstance door)
        {
            double panelWidth = door.GetParameterAsDouble("Width");
            double frameThickness = doorSymbol.GetParameterAsDouble("Door Frame Insert Dim");
            bool hasDoorFrameInsert = doorSymbol.GetParameterAsBool("Show Door Frame Insert");

            var splitString = doorSymbol.FamilyName.Split('-');
            if (splitString.Length < 3)
                return panelWidth;

            if (hasDoorFrameInsert)
                panelWidth -= frameThickness * 2;

            if (splitString[2] == "Double" && splitString.Last() != "Unequal")
                panelWidth /= 2;

            if (splitString.Last() == "Unequal")
            {
                panelWidth -= doorSymbol.GetParameterAsDouble("Door Panel B Width");
                if (hasDoorFrameInsert)
                    panelWidth += frameThickness;

            }

            return panelWidth;
        }

        /// <summary>
        /// Extracts tag dimensions scaled to view
        /// </summary>
        private TagData ExtractTagData(FamilySymbol tagSymbol, int viewScale)
        {
            return new TagData
            {
                Width = tagSymbol.GetParameterAsDouble("Width") * viewScale,
                Height = tagSymbol.GetParameterAsDouble("Height") * viewScale
            };
        }

        /// <summary>
        /// Calculates tag position based on door swing type
        /// </summary>

        private XYZ CalculatePositionBySwingType(DoorData doorData, TagData tagData, GeometricVectors vectors, XYZ hingePoint, View view)
        {

            return doorData.SwingType == HINGED_SWING || doorData.SwingType == PIVOT_SWING
                ? CalculateHingedDoorTagPosition(doorData, tagData, vectors, hingePoint, view)
                : CalculateNonHingeDoorTagPosition(doorData, tagData, vectors, view);
        }

        /// <summary>
        /// Calculates the base point for hinge door tag positioning
        /// </summary>
        /// <param name="doorData"></param>
        /// <param name="vectors"></param>
        /// <param name="panelLocation"></param>
        /// <returns></returns>
        private XYZ CalculateHingePoint(DoorData doorData, GeometricVectors vectors, XYZ panelLocation)
        {
            XYZ xTransform = vectors.XVector.Multiply((doorData.DoorPanelWidth / 2) - doorData.PivotOffset);
            XYZ yTransform = vectors.YVector.Multiply(doorData.DoorThickness / 2);

            return panelLocation
                .Add(xTransform)
                .Add(doorData.IsMirrored ? yTransform.Negate() : yTransform);
        }

        /// <summary>
        /// Calculates tag position for hinged doors
        /// </summary>
        /// <param name="doorData"></param>
        /// <param name="tagData"></param>
        /// <param name="vectors"></param>
        /// <param name="hingePoint"></param>
        /// <returns></returns>
        /// <param name="view"></param>
        private XYZ CalculateHingedDoorTagPosition(DoorData doorData, TagData tagData, GeometricVectors vectors, XYZ hingePoint, View view)
        {
            double dynamicBuffer = view.Scale < 50 
                ? BUFFER_MM * (view.Scale / 50.0) 
                : BUFFER_MM;
            double buffer = UnitUtils.ConvertToInternalUnits(dynamicBuffer, UnitTypeId.Millimeters);
            double doorThicknessOffset = doorData.SwingType == PIVOT_SWING ? doorData.DoorThickness / 2 : doorData.DoorThickness;

            XYZ xSwingTransform = vectors.XVector
                .Multiply(doorThicknessOffset + buffer + (tagData.Height / 2))
                .Negate();

            double yValue = CalculateYValueForHingedDoor(doorData, tagData);
            XYZ ySwingTransform = vectors.YVector.Multiply(yValue);

            if (doorData.IsMirrored)
                ySwingTransform = ySwingTransform.Negate();

            return hingePoint.Add(xSwingTransform).Add(ySwingTransform);
        }

        /// <summary>
        /// Calculates Y offset for hinged door tags
        /// </summary>
        /// <param name="doorData"></param>
        /// <param name="tagData"></param>
        /// <returns></returns>
        private double CalculateYValueForHingedDoor(DoorData doorData, TagData tagData)
        {
            double yValue = (tagData.Width) / 2 - doorData.PivotOffset;

            if (tagData.Width > doorData.DoorPanelWidth * TAG_WIDTH_RATIO)
            {
                double yAdjustment = doorData.DoorPanelWidth - tagData.Width;
                if (yAdjustment < 0)
                    yAdjustment = Math.Abs(yAdjustment) * 2;
                
                yValue -= yAdjustment;
            //    yValue -= doorData.HasFixedFrame
            //        ? doorData.FrameDepth
            //        : doorData.HostWallWidth;
            }

            return yValue;
        }

        /// <summary>
        /// Calculates tag position for all doors other than hinge (sliding, bifold, etc.)
        /// </summary>
        /// <param name="doorData"></param>
        /// <param name="tagData"></param>
        /// <param name="vectors"></param>
        /// <returns></returns>
        /// <param name="view"></param>
        private XYZ CalculateNonHingeDoorTagPosition(DoorData doorData, TagData tagData, GeometricVectors vectors, View view)
        {
            double tagHeightAdjustment = 0;

            if (doorData.DoorFamilyName.EndsWith("Sliding") || doorData.DoorFamilyName.EndsWith("Pocket"))
                tagHeightAdjustment = 100;

            double dynamicScaleAdjustment = TAG_HEIGHT_MULTIPLIER * (view.Scale / 100.0);
            XYZ yTransform = 
                vectors.YVector.Multiply((tagData.Height * dynamicScaleAdjustment)
                + UnitUtils.ConvertToInternalUnits(tagHeightAdjustment, UnitTypeId.Millimeters));

            if (!doorData.IsMirrored)
                yTransform = yTransform.Negate();

            return doorData.DoorLocation.Add(yTransform);
        }

        #region Helper Methods

        /// <summary>
        /// Retreives door width based on host wall type
        /// </summary>
        /// <param name="hostWallFamily"></param>
        /// <param name="doorSymbol"></param>
        /// <param name="door"></param>
        /// <returns></returns>
        private double GetDoorWidth(string hostWallFamily, FamilySymbol doorSymbol, FamilyInstance door)
        {
            return hostWallFamily == CURTAIN_WALL_FAMILY
                ? door.GetParameterAsDouble("Width")
                : doorSymbol.GetParameterAsDouble("Width");
        }

        /// <summary>
        /// Retreives frame depth based on host wall type
        /// </summary>
        /// <param name="doorSymbol"></param>
        /// <param name="hostWallFamily"></param>
        /// <returns></returns>
        private double GetFrameDepth(FamilySymbol doorSymbol, string hostWallFamily)
        {
            var frameParameterName = hostWallFamily == CURTAIN_WALL_FAMILY
                ? "Frame Depth"
                : "Fixed Frame Depth";

            return doorSymbol.GetParameterAsDouble(frameParameterName);
        }

        /// <summary>
        /// Gets host wall width for basic walls (otherwise returns zero)
        /// </summary>
        /// <param name="hostWallType"></param>
        /// <param name="hostWallFamily"></param>
        /// <returns></returns>
        private double GetHostWallWidth(WallType hostWallType, string hostWallFamily)
        {
            return hostWallFamily == BASIC_WALL_FAMILY
                ? hostWallType.GetParameterAsDouble("Width")
                : 0.0;
        }

        /// <summary>
        /// Checks if door has fixed frame, accounting for host wall type
        /// </summary>
        /// <param name="door"></param>
        /// <param name="doorSymbol"></param>
        /// <param name="hostWallType"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool HasFixedFrame(string hostWallFamily, FamilySymbol doorSymbol)
        {
            if (hostWallFamily == CURTAIN_WALL_FAMILY)
            {
                int doorFrameInsertParam = doorSymbol.GetParameterAsInteger("Show Door Frame Insert");
                return doorFrameInsertParam == 1 ? true : false;
            }
            int fixedFrameParam = doorSymbol.GetParameterAsInteger("Fixed Frame");
            return fixedFrameParam == 1 ? true : false;
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

        /// <summary>
        /// Parses swing type and angle from plan swing string
        /// </summary>
        /// <param name="plansSwingString"></param>
        /// <returns></returns>
        private (string swingType, int swingAngle) ParseSwingType(string plansSwingString)
        {
            if (string.IsNullOrEmpty(plansSwingString))
                return (null, 0);

            var splitString = plansSwingString.Split(' ');
            var swingType = splitString.First();

            var swingAngle = (swingType == HINGED_SWING || swingType == PIVOT_SWING) && splitString.Length > 1
                ? ParseSwingAngle(splitString.Last())
                : 0;

            return (swingType, swingAngle);
        }

        /// <summary>
        /// Safely parses swing angle from string
        /// </summary>
        /// <param name="angleString"></param>
        /// <returns></returns>
        private int ParseSwingAngle(string angleString)
        {
            return int.TryParse(angleString, out int angle) ? angle : 0; ;
        }

        #endregion
    }
}
