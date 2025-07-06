#region Namespaces
#endregion

namespace HoloBlok.Tools.Electrical.LightFixtures.Helpers
{
    public class ProgressManager
    {
        private readonly int _totalItems;
        private int _processedItems;

        public ProgressManager(int totalItems)
        {
            _totalItems = totalItems;
            _processedItems = 0;
        }

        public void ReportProgress()
        {
            _processedItems++;
            // TO-DO: Implement progress UI or logging
            Debug.WriteLine($"Progress: {_processedItems}/{_totalItems}");
        }
    }
}
