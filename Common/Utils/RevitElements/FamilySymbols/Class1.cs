using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils.RevitElements.FamilySymbols
{
    internal static class HBFamilySymbolUtils
    {
        internal static void ActivateSymbolIfNotActive(FamilySymbol symbol)
        {
            if (!symbol.IsActive)
                symbol.Activate();
        }
    }
}
