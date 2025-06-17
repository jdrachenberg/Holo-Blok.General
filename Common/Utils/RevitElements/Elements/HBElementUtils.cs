using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Common.Utils.RevitElements.Elements
{
    internal class HBElementUtils
    {
        public static double GetRotation(Element item)
        {
            double rotationAngle = 0;

            // CropBox
            if (item.Category == null)
            {
                Element view = item.Document.GetElement(item.get_Parameter(BuiltInParameter.ID_PARAM).AsElementId());
                if (view is View v)
                {
                    IList<CurveLoop> cropShape = v.GetCropRegionShapeManager().GetCropShape();
                    if (cropShape.Count > 0 && cropShape[0].Count() > 1)
                    {
                        List<Curve> curves = cropShape[0].ToList();
                        Line line = curves[1] as Line;
                        if (line != null)
                        {
                            rotationAngle = Math.Abs(line.Direction.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) * 180 / Math.PI);
                        }
                    }
                }
            }
            else if (item.Location != null)
            {
                Location loc = item.Location;

                // Generic Annotation, ImportInstance and LinkInstance
                if (item is ImportInstance || item is RevitLinkInstance)
                {
                    Transform trans = null;

                    if (item is ImportInstance importInstance)
                        trans = importInstance.GetTotalTransform();
                    else if (item is RevitLinkInstance linkInstance)
                        trans = linkInstance.GetTotalTransform();

                    if (trans != null)
                    {
                        rotationAngle = Math.Abs(trans.BasisX.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) * 180 / Math.PI);
                        rotationAngle = Math.Abs(rotationAngle - 360);
                    }
                }
                // Point-based elements (e.g. most loadable families)
                else if (loc is LocationPoint locPoint)
                {
                    rotationAngle = locPoint.Rotation * 180 / Math.PI;
                }
                else if (item is MEPCurve mepCurve)
                {
                    foreach (Connector connector in mepCurve.ConnectorManager.Connectors)
                    {
                        rotationAngle = Math.Asin(connector.CoordinateSystem.BasisY.X) * 180 / Math.PI;
                        break; // Take first connector
                    }
                }
                else if (item is Grid grid)
                {
                    Line line = grid.Curve as Line;
                    if (line != null)
                    {
                        XYZ vector = line.Direction;
                        rotationAngle = Math.Abs(vector.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) * 180 / Math.PI - 180);
                    }
                }
                else if (item is ReferencePlane refPlane)
                {
                    Document doc = item.Document;
                    View view = doc.ActiveView;
                    IList<Curve> curves = refPlane.GetCurvesInView(DatumExtentType.ViewSpecific, view);
                    if (curves.Count > 0)
                    {
                        Line line = curves[0] as Line;
                        if (line != null)
                        {
                            XYZ vector = line.Direction;
                            rotationAngle = Math.Abs(vector.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) * 180 / Math.PI - 360);
                        }
                    }
                }
                // Line-based elements (e.g. walls)
                else if (loc is LocationCurve locCurve)
                {
                    Line line = locCurve.Curve as Line;
                    if (line != null)
                    {
                        XYZ vector = line.Direction;
                        rotationAngle = Math.Abs(vector.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) * 180 / Math.PI - 360);
                    }
                }
                else
                {
                    // Sketch-based elements (ceiling, floor and roof)
                    if (item is HostObject hostObject)
                    {
                        IList<Reference> topFaces = HostObjectUtils.GetTopFaces(hostObject);
                        if (topFaces.Count > 0)
                        {
                            GeometryObject geomObj = hostObject.GetGeometryObjectFromReference(topFaces[0]);
                            if (geomObj is Face geomFace)
                            {
                                BoundingBoxUV bbox = geomFace.GetBoundingBox();
                                UV maxUV = bbox.Max;
                                Transform trans = geomFace.ComputeDerivatives(maxUV);

                                if (item is Ceiling)
                                {
                                    rotationAngle = Math.Abs(trans.BasisZ.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) - 2 * Math.PI) * 180 / Math.PI;
                                }
                                else
                                {
                                    rotationAngle = Math.Abs(trans.BasisY.AngleOnPlaneTo(XYZ.BasisX, XYZ.BasisZ) - Math.PI) * 180 / Math.PI;
                                }
                            }
                        }
                    }
                }
            }

            // Normalize to 0 if effectively 360
            if (Math.Round(rotationAngle, 3) == 360)
            {
                rotationAngle = 0;
            }

            return Math.Round(rotationAngle, 3);
        }

        public static object GetElementRotations(object input)
        {
            if (input is IList<Element> elements)
            {
                return elements.Select(GetRotation).ToList();
            }
            else if (input is Element element)
            {
                return GetRotation(element);
            }
            else
            {
                throw new ArgumentException("Input must be Element or List<Element>");
            }
        }
    }
}
