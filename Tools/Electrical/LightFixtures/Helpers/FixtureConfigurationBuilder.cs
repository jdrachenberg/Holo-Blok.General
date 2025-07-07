using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    /// <summary>
    /// Helper class to build fixture selection configurations
    /// </summary>
    public class FixtureConfigurationBuilder
    {
        private readonly Document _doc;
        private readonly Dictionary<string, FamilySymbol> _mappings;
        private FamilySymbol _defaultFixture;

        public FixtureConfigurationBuilder(Document doc)
        {
            _doc = doc;
            _mappings = new Dictionary<string, FamilySymbol>();
        }

        public FixtureConfigurationBuilder AddMapping(string ceilingTypeName, string fixtureFamily, string fixtureType)
        {
            var fixture = HBCollectors.GetSymbol(_doc, BuiltInCategory.OST_LightingFixtures, fixtureFamily, fixtureType);
            if (fixture != null)
            {
                _mappings[ceilingTypeName] = fixture;
            }
            else
            {
                throw new InvalidOperationException($"Fixture type '{fixtureFamily}' - '{fixtureType}' not found in project.");
            }
            return this;
        }

        /// <summary>
        /// Adds a mapping using an existing FamilySymbol
        /// </summary>
        public FixtureConfigurationBuilder AddMapping(string ceilingTypeName, FamilySymbol fixtureSymbol)
        {
            if (fixtureSymbol != null)
            {
                _mappings[ceilingTypeName] = fixtureSymbol;
            }
            return this;
        }

        /// <summary>
        /// Sets the default fixture to use when no mapping matches
        /// </summary>
        public FixtureConfigurationBuilder SetDefaultFixture(string fixtureFamily, string fixtureType)
        {
            _defaultFixture = HBCollectors.GetSymbol(_doc, BuiltInCategory.OST_LightingFixtures, fixtureFamily, fixtureType);
            if (_defaultFixture == null)
            {
                throw new InvalidOperationException($"Default fixture type '{fixtureFamily}' - '{fixtureType}' not found in project.");
            }
            return this;
        }

        /// <summary>
        /// Sets the default fixture using an existing FamilySymbol
        /// </summary>
        public FixtureConfigurationBuilder SetDefaultFixture(FamilySymbol fixtureSymbol)
        {
            _defaultFixture = fixtureSymbol;
            return this;
        }

        /// <summary>
        /// Builds the ceiling-based fixture selector
        /// </summary>
        public CeilingBasedFixtureSelector Build()
        {
            if (_defaultFixture == null)
            {
                throw new InvalidOperationException("Default fixture must be set before building.");
            }

            return new CeilingBasedFixtureSelector(_doc, _mappings, _defaultFixture);
        }

        /// <summary>
        /// Creates a pre-configured builder with common mappings
        /// </summary>
        public static FixtureConfigurationBuilder CreateWithDefaults(Document doc)
        {
            return new FixtureConfigurationBuilder(doc)
                .AddMapping("Gypsum", "Downlight - Recessed Can", "203mm Incandescent - 230V")
                .AddMapping("600x1200mm_Grid", "Ceiling Light - Linear Box", "0600x1200mm(2 Lamp) - 230V")
                .AddMapping("600x600mm_Grid", "Ceiling Light - Linear Box", "0600x0600mm(2 Lamp) - 230V")
                .SetDefaultFixture("Downlight - Recessed Can", "203mm Incandescent - 230V");
        }
    }
}
