using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.Families
{
    internal class HBFamilyInstanceUtils
    {
        internal static XYZ GetLocationPoint(FamilyInstance famInstance)
        {
            if (famInstance.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point;
            }
            return null;
        }

        //Move a family instance in facing orientation by given distance
        internal static void MoveFamilyInstanceInFacingOrientation(Document doc, FamilyInstance famInstance, double distance)
        {
            XYZ moveVector = CalculateFacingOrientationVector(famInstance, distance);
            ElementTransformUtils.MoveElement(doc, famInstance.Id, moveVector);
        }

        //Get facing orientation vector of family instance
        private static XYZ CalculateFacingOrientationVector(FamilyInstance famInstance, double distance)
        {
            XYZ facingOrientation = famInstance.FacingOrientation;
            return facingOrientation * distance;
        }

        //Rotate a family instance by given angle
        internal static FamilyInstance RotateFamilyInstance(Document doc, FamilyInstance famInstance, double rotationAngle, XYZ locationPoint, View view)
        {
            Line axis = CreateRotationAxis(locationPoint, view.ViewDirection);
            ElementTransformUtils.RotateElement(doc, famInstance.Id, axis, rotationAngle);

            return famInstance;
        }

        internal static FamilyInstance RotateFamilyInstance(Document doc, FamilyInstance famInstance, double rotationAngle, XYZ locationPoint, Face face)
        {
            Line axis = CreateRotationAxis(locationPoint, face.ComputeNormal(new UV()));
            ElementTransformUtils.RotateElement(doc, famInstance.Id, axis, rotationAngle);

            return famInstance;
        }

        private static Line CreateRotationAxis(XYZ locationPoint, XYZ direction)
        {
            XYZ startPt = locationPoint;
            XYZ endPt = locationPoint + direction;
            return Line.CreateBound(startPt, endPt);
        }

        //Place family symbols at midpoints of given curves
        internal static FamilyInstance PlaceFamilyInstanceInView(Document doc, View activeView, FamilySymbol familySymbol, XYZ insertionPoint)
        {
            FamilyInstance famInstance = ArchSmarterUtils.Families.InsertFamilyToView(doc, activeView, familySymbol, insertionPoint);
            return famInstance;
        }

        internal static void PlaceBreaklineAtCurveMidpoint(Document doc, View activeView, FamilySymbol breaklineSymbol, Curve intersectingCurve, double symbolSize)
        {
            XYZ familyPlacementPoint = HBCurveUtils.GetMidpoint(intersectingCurve);
            FamilyInstance breaklineInstance = HBFamilyInstanceUtils.PlaceFamilyInstanceInView(doc, activeView, breaklineSymbol, familyPlacementPoint);
            double curveRotation = HBCurveUtils.GetRotationInView(intersectingCurve, activeView);

            HBFamilyInstanceUtils.RotateFamilyInstance(doc, breaklineInstance, curveRotation, familyPlacementPoint, activeView);
            double curveLength = intersectingCurve.Length;

            HBParameterUtils.SetByName(doc, breaklineInstance.Id, "Jag Width", symbolSize);
            HBParameterUtils.SetByName(doc, breaklineInstance.Id, "left", curveLength / 2);
            HBParameterUtils.SetByName(doc, breaklineInstance.Id, "right", curveLength / 2);

            HBFamilyInstanceUtils.MoveFamilyInstanceInFacingOrientation(doc, breaklineInstance, -symbolSize / 2.5);
        }

    }
}
