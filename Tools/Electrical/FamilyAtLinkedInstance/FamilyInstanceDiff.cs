using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Tools.Electrical.FamilyAtLinkedInstance
{
    public class FamilyInstanceDiff
    {
        public bool NeedsMove { get; set; }
        public bool NeedsMarkUpdate { get; set; }
        public bool NeedsElevationUpdate { get; set; }
        public XYZ DesiredLocation { get; set; }
        public string DesiredMark { get; set; }
        public double DesiredElevation { get; set; }

        public bool HasChanges => NeedsMove || NeedsMarkUpdate || NeedsElevationUpdate;
    }
}
