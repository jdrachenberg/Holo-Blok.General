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
                app.CreateRibbonTab("holo-blok");
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }

            RibbonPanel doorPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Doors");
            RibbonPanel electricalPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Electrical");
            RibbonPanel dimensionPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Dimensions");
            RibbonPanel detailComponentPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Detail Components");

            #region ButtonDataClasses

            // Doors
            ButtonDataClass tagDoorsData = new ButtonDataClass("Tag in Active View", "Tag in Active View", TagDoorsInView.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Tag doors in active view");
            ButtonDataClass tagDoorsMultipleViewsData = new ButtonDataClass("Tag in Multiple Views", "Tag in Multiple Views", TagDoorsInViews.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Tag doors in multiple views");
            ButtonDataClass renumberDoorsData = new ButtonDataClass("Renumber", "Renumber", RenumberDoors.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Renumbers all doors");
            
            // Dimension grids
            ButtonDataClass dimensionGridsData = new ButtonDataClass("Grids in Active View", "Grids in Active View", DimensionGrids.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Dimension grids in active view");
            
            // Detail components
            ButtonDataClass BreaklinesInViewData = new ButtonDataClass("Breaklines in Active View", "Breaklines in Active View", BreaklinesByView.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Add breaklines to current view");
            
            // Electrical
            ButtonDataClass PlaceLightFixturesData = new ButtonDataClass("Breaklines in Active View", "Breaklines in Active View", BreaklinesByView.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Add breaklines to current view");

            #endregion

            #region SplitButtons
            SplitButtonData splitButtonData = new SplitButtonData("Tag Doors", "Tag all doors aligned");
            SplitButton tagDoorsSplitButton = doorPanel.AddItem(splitButtonData) as SplitButton;

            tagDoorsSplitButton.AddPushButton(tagDoorsData.Data);
            tagDoorsSplitButton.AddPushButton(tagDoorsMultipleViewsData.Data);

            #endregion

            #region PushButtons
            PushButton renumberDoors = doorPanel.AddItem(renumberDoorsData.Data) as PushButton;
            PushButton dimensionGrids = dimensionPanel.AddItem(dimensionGridsData.Data) as PushButton;
            PushButton breaklines = detailComponentPanel.AddItem(BreaklinesInViewData.Data) as PushButton;
            PushButton lightFixtures = electricalPanel.AddItem(PlaceLightFixturesData.Data) as PushButton;


            #endregion

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}