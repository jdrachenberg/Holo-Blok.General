using Autodesk.Revit.DB;
using HoloBlok.Common.Enums;
using HoloBlok.Utils.Geometry;
using HoloBlok.Utils.RevitElements.FamilySymbols;
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

        #region FamilyInstances in doc
        internal static IEnumerable<FamilyInstance> GetMechanicalEquipmentInstances(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
                .WhereElementIsNotElementType()
                .OfType<FamilyInstance>();
        }

        #endregion

        internal static Level GetLevelByName(Document doc, string levelName)
        {
            var level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(level => level.Name.Equals(levelName, StringComparison.OrdinalIgnoreCase));

            if (level == null)
                throw new InvalidOperationException($"Level '{levelName}' not found in the document.");

            return level;
        }

        #region Get Specific Family Symbol

        internal static FamilySymbol GetSymbol(Document doc, BuiltInCategory category, string familyName, string typeName)
        {
            // Get first available light fixture type
            // In future, this could be a user selection dialog
            FamilySymbol symbol =  new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(category)
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.FamilyName == familyName && fs.Name == typeName);
            if (symbol == null)
                throw new InvalidOperationException($"Could not find symbol {familyName}, {typeName}.");


            //HBFamilySymbolUtils.ActivateSymbolIfNotActive(symbol);

            return symbol;
        }

        #endregion

        #region Linked Models
        internal static List<RevitLinkInstance> GetAllLinkedModels(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();
        }
        internal static List<RevitLinkInstance> GetLinkedModelsByDiscipline(Document doc, LinkType linkType)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsLinkOfType(link, linkType))
                .ToList();
        }
        internal static RevitLinkInstance GetLinkedModelByDiscipline(Document doc, LinkType linkType)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsLinkOfType(link, linkType))
                .First();
        }

        private static bool IsLinkOfType(RevitLinkInstance link, LinkType linkType)
        {
            string linkName = link.Name.ToLower();

            return linkType switch
            {
                LinkType.Arch => linkName.Contains("arch"),
                LinkType.Struc => linkName.Contains("struc"),
                LinkType.Mech => linkName.Contains("mech") || linkName.Contains("hvac"),
                LinkType.Elec => linkName.Contains("elec"),
                _ => false,
            };
        }

        internal static List<FamilyInstance> GetFamilyInstancesBySymbol(Document doc, FamilySymbol symbol)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance))
                .Cast<FamilyInstance>()
                .Where(instance => instance.Symbol.Id.Equals(symbol.Id))
                .ToList();
        }

        #endregion
    }
}
