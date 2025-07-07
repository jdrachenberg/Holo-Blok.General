using HoloBlok.Utils.RevitElements.FamilySymbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class CeilingBasedFixtureSelector : IFixtureSelectionStrategy
    {
        private readonly Document _hostDoc;
        private readonly Dictionary<string, FamilySymbol> _ceilingTypeToFixtureMap;
        private readonly FamilySymbol _defaultFixtureType;

        public CeilingBasedFixtureSelector(Document hostDoc, Dictionary<string, FamilySymbol> ceilingTypeToFixtureMap, FamilySymbol defaultFixtureType)
        {
            _hostDoc = hostDoc;
            _ceilingTypeToFixtureMap = ceilingTypeToFixtureMap;
            _defaultFixtureType = defaultFixtureType;
        }
        
        public FamilySymbol SelectFixtureType(Element hostElement, Document linkedDoc)
        {
            if (hostElement == null)
                return _defaultFixtureType;

            // Check if the host element is a ceiling
            if (hostElement.Category?.Id.IntegerValue != (int)BuiltInCategory.OST_Ceilings)
                return _defaultFixtureType;

            // Get the ceiling type
            ElementId typeId = hostElement.GetTypeId();
            if (typeId == null || typeId == ElementId.InvalidElementId)
                return _defaultFixtureType;

            Element ceilingType = linkedDoc.GetElement(typeId);
            if (ceilingType == null)
                return _defaultFixtureType;

            string typeName = ceilingType.Name;

            // Try exact match first
            if (_ceilingTypeToFixtureMap.ContainsKey(typeName))
                return _ceilingTypeToFixtureMap[typeName];

            // Try partial match (contains)
            foreach (var kvp in _ceilingTypeToFixtureMap)
            {
                if (typeName.Contains(kvp.Key) || kvp.Key.Contains(typeName))
                    return kvp.Value;
            }

            return _defaultFixtureType;
        }

        public FamilySymbol GetDefaultFixtureType()
        {
            return _defaultFixtureType;
        }

        public void ActivateAllFixtureTypes()
        {
            // Activate default fixture
            HBFamilySymbolUtils.ActivateSymbolIfNotActive(_defaultFixtureType);

            // Activate all mapped fixtures
            foreach (var fixtureType in _ceilingTypeToFixtureMap.Values.Distinct())
            {
                HBFamilySymbolUtils.ActivateSymbolIfNotActive(fixtureType);
            }
        }

        

        
    }
}
