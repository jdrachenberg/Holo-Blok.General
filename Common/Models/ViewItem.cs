using HoloBlok.Forms;

namespace HoloBlok.Common.Models
{
    public class ViewItem : SelectableItem
    {
        public View View { get; set; }
        public string ViewName { get; set; }
        public ElementId ViewTemplateId { get; set; }
    }
}
