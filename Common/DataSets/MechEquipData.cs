#region Namespaces
#endregion

namespace HoloBlok.Common.DataSets
{
    public class MechEquipData
    {
        public ElementId ElementId { get; set; }
        public string Mark { get; set; }
        public string Description { get; set; }
        public string SpaceName { get; set; }
        public string SpaceNumber { get; set; }
        public double Voltage { get; set; }
        public double ApparentLoadPhase1 { get; set; }
        public double ApparentLoadPhase2 { get; set; }
        public double TotalApparentLoad { get; set; }

        // Calculated fields (to be filled in Excel)
        public double TotalAmps { get; set; }
        public double PowerFactor { get; set; }
        public double HP { get; set; }

        public double MCA { get; set; }
        public string BreakerSize { get; set; }
        public string WireSize { get; set; }
        public string PanelName { get; set; }
        public string CircuitNumber { get; set; }
    }

}
