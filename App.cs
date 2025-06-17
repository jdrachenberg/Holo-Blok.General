using HoloBlok.Common;
using HoloBlok;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using System.Diagnostics;

namespace HoloBlok
{
    internal class App : IExternalApplication
    {
        private byte[] BitmapToByteArray(Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public BitmapImage ConvertToImageSource(byte[] imageData)
        {
            using (MemoryStream mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                BitmapImage bmi = new BitmapImage();
                bmi.BeginInit();
                bmi.StreamSource = mem;
                bmi.CacheOption = BitmapCacheOption.OnLoad;
                bmi.EndInit();
                return bmi;
            }
        }

        public Result OnStartup(UIControlledApplication app)
        {
            try
            {
                app.CreateRibbonTab("Holo-Blok");
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }

            RibbonPanel panel = HBRibbonUtils.CreateRibbonPanel(app, "Holo-Blok", "Documentation");

            ButtonDataClass tagDoorsData = new ButtonDataClass("Tag Doors", "Tag Doors", TagDoors.GetMethod(), Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Tag all doors according to Holo-Blok standard");
            ButtonDataClass renumberDoorsData = new ButtonDataClass("Renumber Doors", "Renumber Doors", RenumberDoors.GetMethod(), Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Renumbers all doors according to Holo-Blok standard");
            ButtonDataClass dimensionGridsData = new ButtonDataClass("Dimension Grids", "Dimension Grids", DimensionGrids.GetMethod(), Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Dimension all grids in view");
            ButtonDataClass BreaklinesInViewData = new ButtonDataClass("Breaklines (View)", "Breaklines (View)", BreaklinesByView.GetMethod(), Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Add breaklines to current view");

            PushButton tagDoors = panel.AddItem(tagDoorsData.Data) as PushButton;
            PushButton renumberDoors = panel.AddItem(renumberDoorsData.Data) as PushButton;
            PushButton dimensionGrids = panel.AddItem(dimensionGridsData.Data) as PushButton;
            PushButton breaklines = panel.AddItem(BreaklinesInViewData.Data) as PushButton;

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}