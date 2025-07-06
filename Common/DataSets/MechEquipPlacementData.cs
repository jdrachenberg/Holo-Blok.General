#region Namespaces
#endregion

namespace HoloBlok.Common.DataSets
{
    public class MechEquipPlacementData
    {
        
        public ElementId ElementId { get; set; }
        public Transform Transform { get;  set; }
        public XYZ LocationPoint { get; set; }
        public double ElevationFromLevel { get; set; }
        public string LevelName { get; set; }
        public string Mark { get; set; }
        public string UniqueId { get; set; }
    }

}
