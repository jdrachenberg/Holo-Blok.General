using Autodesk.Revit.DB;
using HoloBlok.Utils.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok
{
    internal static class HBCollectors
    {
        internal static FilteredElementCollector GetInstancesOfCategory(Document doc, BuiltInCategory builtInCategory)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(builtInCategory)
                .WhereElementIsNotElementType();
        }

        internal static FilteredElementCollector GetInstancesOfClass(Document doc, Type type)
        {
            return new FilteredElementCollector(doc)
                .OfClass(type)
                .WhereElementIsNotElementType();
        }

        internal static FilteredElementCollector GetInstancesOfClassInView(Document doc, View activeView, Type type)
        {
            return new FilteredElementCollector(doc, activeView.Id)
                .OfClass(type)
                .WhereElementIsNotElementType();
        }
        
        internal static FilteredElementCollector GetDoorsInView(Document doc, View view)
        {
            return HBCollectors.GetInstancesOfClassInView(doc, view, typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_Doors);
        }

        public static FilteredElementCollector GetModelElementsInView(Document doc, View view)
        {
            return new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .WherePasses(HBMulticategoryFilterService.AllModelCategories);
        }


        internal static IEnumerable<Grid> GetGridsInView(Document doc, View view)
        {
            return HBCollectors.GetInstancesOfClassInView(doc, view, typeof(Grid)).OfType<Grid>();
        }
    }
}
