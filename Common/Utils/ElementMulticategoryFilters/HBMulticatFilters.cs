using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * I might need to rethink the entire logic of this solution.
 * Yet I also feel like I might be on the right track. I want to have a well-organized collection of functions
 * That I can re-use over and over again and with increasing power as I build more and more Revit-Addins.
 * Sorting into different categories of Revit elements does make sense to me, but I also need to follow best practices
 * for how to organize my code.
 * */

namespace HoloBlok.Utils.Geometry
{
    public static class HBMulticategoryFilterService
    {
        public static ElementMulticategoryFilter AllModelCategories
        {
            get
            {
                List<BuiltInCategory> modelCategories = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_StructuralStiffener,
                BuiltInCategory.OST_StructuralTruss,
                BuiltInCategory.OST_CurtainWallPanels,
                BuiltInCategory.OST_CurtainWallMullions,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_Railings,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_Casework,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_FurnitureSystems,
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_PlumbingFixtures,
                BuiltInCategory.OST_LightingFixtures,
                BuiltInCategory.OST_ElectricalEquipment,
                BuiltInCategory.OST_ElectricalFixtures,
                BuiltInCategory.OST_SpecialityEquipment
            };

                return new ElementMulticategoryFilter(modelCategories);
            }
        }
    }
}
