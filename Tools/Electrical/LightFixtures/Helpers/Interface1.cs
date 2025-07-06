using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    /// <summary>
    /// Interface for strategies that determine which fixture type to use based on host element
    /// </summary>
    public interface IFixtureSelectionStrategy
    {
        /// <summary>
        /// Determines the appropriate fixture type based on the host element
        /// </summary>
        /// <param name="hostElement">The host element (ceiling, floor, etc.)</param>
        /// <param name="linkedDoc">The document containing the host element</param>
        /// <returns>The appropriate fixture family symbol, or null if no match found</returns>
        FamilySymbol SelectFixtureType(Element hostElement, Document linkedDoc);

        /// <summary>
        /// Gets the default fixture type if no specific match is found
        /// </summary>
        FamilySymbol GetDefaultFixtureType();

        /// <summary>
        /// Ensures all fixture types used by this strategy are activated
        /// </summary>
        void ActivateAllFixtureTypes();
    }
}
