using HoloBlok.Common;
using HoloBlok.Tools.Electrical.DataSync;
using HoloBlok.Tools.Electrical.FamilyAtLinkedInstance;
using HoloBlok.Tools.Electrical.LightFixtures;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

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
            RibbonPanel excelPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Excel Link");
            RibbonPanel electricalPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Electrical");
            RibbonPanel dimensionPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Dimensions");
            RibbonPanel detailComponentPanel = HBRibbonUtils.CreateRibbonPanel(app, "holo-blok", "Detail Components");

            #region ButtonDataClasses

            // Doors
            ButtonDataClass tagDoorsData = new ButtonDataClass("Tag in\nActive View", "Tag in\nActive View", TagDoorsInView.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Tag doors in active view");
            ButtonDataClass tagDoorsMultipleViewsData = new ButtonDataClass("Tag in\nMultiple Views", "Tag in\nMultiple Views", TagDoorsInViews.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Tag doors in multiple views");
            ButtonDataClass renumberDoorsData = new ButtonDataClass("Renumber", "Renumber", RenumberDoors.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Renumbers all doors");

            // Excel Link
            ButtonDataClass exportMechData = new ButtonDataClass("Export\nMech Data", "Export\nMech Data", ExportMechData.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Pull relevant mechanical equipment data from linked model and push to Excel spreadsheet");
            ButtonDataClass importElecData = new ButtonDataClass("Import\nElec Data", "Import\nElec Data", ImportElecData.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Pull electrical data from Excel spreadsheet");

            // Electrical
            ButtonDataClass placeLightFixturesData = new ButtonDataClass("Place\nLight Fixtures", "Place\nLight Fixtures", PlaceLightFixtures.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Place light fixtures at correct elevations");
            ButtonDataClass familyAtLinkedInstanceData = new ButtonDataClass("Family at\nLinked Instance", "Family at\nLinked Instance", FamilyAtLinkedInstance.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Place family instances relative to linked model elements");

            // Dimension grids
            ButtonDataClass dimensionGridsData = new ButtonDataClass("Grids in\nActive View", "Grids in\nActive View", DimensionGrids.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Dimension grids in active view");
            
            // Detail components
            ButtonDataClass breaklinesInViewData = new ButtonDataClass("Breaklines in\nActive View", "Breaklines in\nActive View", BreaklinesByView.GetMethod(),
                Properties.Resources.holoblok_32, Properties.Resources.holoblok_16, "Add breaklines to current view");
            #endregion


            #region SplitButtons
            SplitButtonData splitButtonData = new SplitButtonData("Tag Doors", "Tag all doors aligned");
            SplitButton tagDoorsSplitButton = doorPanel.AddItem(splitButtonData) as SplitButton;

            tagDoorsSplitButton.AddPushButton(tagDoorsData.Data);
            tagDoorsSplitButton.AddPushButton(tagDoorsMultipleViewsData.Data);
            #endregion

            #region PushButtons
            //Doors
            PushButton renumberDoors = doorPanel.AddItem(renumberDoorsData.Data) as PushButton;

            // Excel Link
            PushButton exportMech = excelPanel.AddItem(exportMechData.Data) as PushButton;
            PushButton importElec = excelPanel.AddItem(importElecData.Data) as PushButton;

            // Electrical
            PushButton familyAtLinkedInstance = electricalPanel.AddItem(familyAtLinkedInstanceData.Data) as PushButton;
            PushButton lightFixtures = electricalPanel.AddItem(placeLightFixturesData.Data) as PushButton;

            //Dimensions
            PushButton dimensionGrids = dimensionPanel.AddItem(dimensionGridsData.Data) as PushButton;

            //Detail Components
            PushButton breaklines = detailComponentPanel.AddItem(breaklinesInViewData.Data) as PushButton;




            #endregion

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}