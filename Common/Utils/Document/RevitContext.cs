using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoloBlok.Utils
{
    public static class RevitContext
    {
        public static UIApplication UiApp { get; private set; }

        public static void Initialize(UIApplication uiApp)
        {
            UiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));
        }

        public static void Clear()
        {
            UiApp = null;
        }
    }
}
