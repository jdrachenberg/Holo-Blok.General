using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    internal class CeilingFixtureConfiguration
    {
        public FamilySymbol FixtureType { get; set; }
        public double SpacingX { get; set; }
        public double SpacingY { get; set; }

        public CeilingFixtureConfiguration(FamilySymbol fixtureType, double spacingX, double spacingY)
        {
            FixtureType = fixtureType;
            SpacingX = spacingX;
            SpacingY = spacingY;
        }
    }
}
