using HoloBlok.Common.DataSets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.FamilyAtLinkedInstance
{
    
    internal class PlacementPointAdjuster
    {
        public const double translationDistance = 3;

        public static XYZ GetDesiredLocation(MechEquipPlacementData mechData)
        {
            return mechData.LocationPoint.Add(mechData.Transform.BasisX * translationDistance);
        }
    }
}
