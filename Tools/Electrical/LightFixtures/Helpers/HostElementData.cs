using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class HostElementData
    {
        public Face HostFace {  get; set; }
        public Element HostElement { get; set; }
        public RevitLinkInstance LinkInstance { get; set; }
        public Document LinkedDocument { get; set; }
        public double IntersectionHeight { get; set; }
        public double GridRotation { get; set; }

        public HostElementData() { }

        public HostElementData(Face face, Element element, RevitLinkInstance linkInstance, Document linkedDoc, double height, double gridRotation)
        {
            HostFace = face;
            HostElement = element;
            LinkInstance = linkInstance;
            LinkedDocument = linkedDoc;
            IntersectionHeight = height;
            GridRotation = gridRotation;
        }

        public Reference CreateHostReference()
        {
            if (HostFace?.Reference == null || LinkInstance == null)
                return null;

            return HostFace.Reference.CreateLinkReference(LinkInstance);
        }

        /// <summary>
        /// Gets the name of the host element type
        /// </summary>
        public string GetHostTypeName()
        {
            if (HostElement == null || LinkedDocument == null)
                return string.Empty;

            ElementId typeId = HostElement.GetTypeId();
            if (typeId == null || typeId == ElementId.InvalidElementId)
                return string.Empty;

            Element elementType = LinkedDocument.GetElement(typeId);
            return elementType?.Name ?? string.Empty;
        }
    }
}
