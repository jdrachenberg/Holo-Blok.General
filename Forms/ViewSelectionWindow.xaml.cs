using Autodesk.Revit.DB.Mechanical;
using FilterTreeControlWPF;
using HoloBlok.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FilterItem = HoloBlok.Common.Models.FilterItem;

namespace HoloBlok.Forms
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ViewSelectionWindow : Window
    {
        private Document _doc;
        private ObservableCollection<ViewItem> _allViewItems;
        private ObservableCollection<FilterItem> _currentFilterItems = new ObservableCollection<FilterItem>();

        public ObservableCollection<ViewItem> ViewItems { get; set; }
        public List<View> SelectedViews { get; private set; }


        /// <summary>
        /// Constructor - initializes the window but defers loading data
        /// </summary>
        public ViewSelectionWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            // Defer initialization until window is loaded
            this.Loaded += ViewSelectionWindow_Loaded;
        }

        /// <summary>
        /// Called when window is fully loaded - safe to access all controls now
        /// </summary
        private void ViewSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllViews();
            LoadFilters();
            UpdateViewDisplay();
        }

        /// <summary>
        /// Loads all floor plan views from the Revit document
        /// </summary>
        private void LoadAllViews()
        {
            _allViewItems = new ObservableCollection<ViewItem>();

            var planViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan)
                .OrderBy(v => v.Name);

            // Convert each Revit view to our ViewItem wrapper class
            foreach (var view in planViews)
            {
                _allViewItems.Add(new ViewItem
                {
                    View = view,
                    ViewName = view.Name,
                    IsSelected = false,
                    ViewTemplateId = view.ViewTemplateId,
                });
            }
        }

        ///<summary>
        /// Sets up the initial filter state, starting with "All Views" (no filtering)
        /// </summary>
        private void LoadFilters()
        {
            // Bind the filter list to the FilterListBox in XAML
            FilterListBox.ItemsSource = _currentFilterItems;

            // Hide filter controls initially since "All Views" is selected
            FilterButtonsPanel.Visibility = System.Windows.Visibility.Collapsed;
            FilterListBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void FilterTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // IMPORTANT: Skip if window isn't loaded yet to avoid null reference errors
            if (!IsLoaded)
                return;

            // Get the selected filter type from the dropdown
            var selectedItem = FilterTypeComboBox.SelectedItem as ComboBoxItem;
            var filterType = selectedItem?.Content.ToString();

            // Clear existing filter items
            _currentFilterItems.Clear();

            bool showFilters = false;

            // Load appropriate filters based on selection
            switch (filterType)
            {
                case "All Views":
                    // No filters needed - hide filter controls
                    showFilters = false;
                    break;

                case "View Templates":
                    LoadViewTemplateFilters();
                    showFilters = true;
                    break;

                case "Sheets":
                    LoadSheetFilters();
                    showFilters = true;
                    break;
            }

            FilterButtonsPanel.Visibility = showFilters ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            FilterListBox.Visibility = showFilters ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            // Update the list of views to suit the given filters
            UpdateViewDisplay();
        }

        /// <summary>
        /// Creates filter items for each view template used by the views
        /// </summary>
        private void LoadViewTemplateFilters()
        {
            // Get unique template IDS from all views
            var templateIds = _allViewItems
                .Where(v => v.ViewTemplateId != ElementId.InvalidElementId)
                .Select(v => v.ViewTemplateId)
                .Distinct();

            // Create a filter item for each template
            foreach (var templateId in templateIds)
            {
                var template = _doc.GetElement(templateId) as View;
                if (template != null)
                {
                    // Count how many views use this template
                    var viewCount = _allViewItems.Count(v => v.ViewTemplateId == templateId);
                    _currentFilterItems.Add(new FilterItem
                    {
                        Id = templateId,
                        DisplayName = $"{template.Name} ({viewCount} views)", // Shows count in UI
                        IsSelected = false // Start with all templates deselected
                    });
                }
            }

            // Add special filter for views without a template
            var noTemplateCount = _allViewItems.Count(v => v.ViewTemplateId == ElementId.InvalidElementId);
            if (noTemplateCount > 0)
            {
                _currentFilterItems.Add(new FilterItem
                {
                    Id = ElementId.InvalidElementId,
                    DisplayName = $"No Template ({noTemplateCount} views)"
                });
            }
        }

        /// <summary>
        /// Creates filter items for each sheet that contains floor plan views
        /// </summary>
        private void LoadSheetFilters()
        {
            List<Viewport> allViewports = GetAllViewports();
            Dictionary<ElementId, ElementId> viewIdToSheetId = BuildViewToSheetMap(allViewports);
            var (sheetsWithViews, viewSheetCounts) = BuildSheetViewCounts(viewIdToSheetId);

            AddSheetFilters(sheetsWithViews, viewSheetCounts);
            AddNotOnSheetFilter(viewIdToSheetId);
        }

        #region LoadSheetFilters helpers
        private List<Viewport> GetAllViewports()
        {
            return new FilteredElementCollector(_doc)
                            .OfClass(typeof(Viewport))
                            .Cast<Viewport>()
                            .ToList();
        }

        private static Dictionary<ElementId, ElementId> BuildViewToSheetMap(List<Viewport> allViewports)
        {
            // Step 2: Build lookup of viewId -> sheetId
            return allViewports
                .GroupBy(vp => vp.ViewId)
                .ToDictionary(g => g.Key, g => g.First().SheetId);
            // One sheet per view
        }

        private (Dictionary<ElementId, string>, Dictionary<ElementId, int>) BuildSheetViewCounts(Dictionary<ElementId, ElementId> viewToSheet)
        {
            // Step 3: Build mapping of sheet IDs to their display names and count how many views are placed on each sheet
            var sheetsWithViews = new Dictionary<ElementId, string>();
            var viewSheetCounts = new Dictionary<ElementId, int>();

            // For each view, find what sheets it's placed on
            foreach (var viewItem in _allViewItems)
            {
                if (viewToSheet.TryGetValue(viewItem.View.Id, out var sheetId))
                {
                    // Track which sheet has this view
                    var sheet = _doc.GetElement(sheetId) as ViewSheet;
                    if (sheet != null)
                    {
                        if (!sheetsWithViews.ContainsKey(sheet.Id))
                        {
                            sheetsWithViews[sheet.Id] = viewItem.ViewName;
                            viewSheetCounts[sheet.Id] = 0;
                        }
                        viewSheetCounts[sheet.Id]++;
                    }
                }
            }

            return (sheetsWithViews, viewSheetCounts);
        }
        private void AddSheetFilters(Dictionary<ElementId, string> sheetsWithViews, Dictionary<ElementId, int> viewSheetCounts)
        {
            // Step 4: Create filter items for each sheet
            foreach (var sheet in sheetsWithViews)
            {
                _currentFilterItems.Add(new FilterItem
                {
                    Id = sheet.Key,
                    DisplayName = $"{sheet.Value} ({viewSheetCounts[sheet.Key]} views)",
                    IsSelected = false // Start with no views selected
                });
            }
        }

        private void AddNotOnSheetFilter(Dictionary<ElementId, ElementId> viewIdToSheetId)
        {
            // Step 5: Add "Not on Sheet" filter
            var viewsOnSheets = new HashSet<ElementId>(viewIdToSheetId.Keys);
            var notOnSheetCount = _allViewItems.Count(v => !viewsOnSheets.Contains(v.View.Id));

            if (notOnSheetCount > 0)
            {
                _currentFilterItems.Add(new FilterItem
                {
                    Id = ElementId.InvalidElementId,
                    DisplayName = $"Not on Sheet ({notOnSheetCount} views)",
                    IsSelected = false
                });
            }
        }

        #endregion


        /// <summary>
        /// Updates the view list based on current filters and refreshes UI
        /// </summary>
        private void UpdateViewDisplay()
        {
            // Get filtered views based on current filter settings
            var filteredViews = GetFilteredViews();

            // Update the ViewListBox's ItemSource to match the new filter
            ViewItems = new ObservableCollection<ViewItem>(filteredViews);
            ViewListBox.ItemsSource = ViewItems;

            UpdateViewCount();
        }

        /// <summary>
        /// Applies current filters to get the views that should be displayed
        /// </summary>
        private IEnumerable<ViewItem> GetFilteredViews()
        {
            var selectedItem = FilterTypeComboBox.SelectedItem as ComboBoxItem;
            var filterType = selectedItem?.Content.ToString();
            return GetViewsForFilterType(filterType);
        }

        private IEnumerable<ViewItem> GetViewsForFilterType(string filterType)
        {
            switch (filterType)
            {
                case "All Views":
                    //no filtering - return all views
                    return _allViewItems;

                case "View Templates":
                    return GetViewTemplateSelectionViews();

                case "Sheets":
                    return GetSheetSelectionViews();

                default:
                    throw new InvalidOperationException($"Unknown filter type: {filterType}");
            }
        }

        private IEnumerable<ViewItem> GetViewTemplateSelectionViews()
        {
            if (IsEmptyFilter())
                return _allViewItems;

            // Get selected template IDs
            var selectedTemplateIds = _currentFilterItems
                .Where(f => f.IsSelected)
                .Select(f => f.Id)
                .ToHashSet();

            // Return views that have one of the selected templates
            return _allViewItems.Where(v => selectedTemplateIds.Contains(v.ViewTemplateId));
        }

        private IEnumerable<ViewItem> GetSheetSelectionViews()
        {
            if (IsEmptyFilter())
                return _allViewItems;

            HashSet<ElementId> selectedSheetIds = GetSelectedSheetIds();
            HashSet<ElementId> viewsOnSelectedSheets = GetViewsFromSheets(selectedSheetIds);

            bool includeNotOnSheet = IsNotOnSheetSelected();

            HashSet<ElementId> allViewsOnSheets = null;
            if (includeNotOnSheet)
                allViewsOnSheets = GetAllViewsOnSheets();

            return ViewsMatchingCriteria(viewsOnSelectedSheets, includeNotOnSheet, allViewsOnSheets);
        }


        #region GetSheetSelectionViews Helpers
        private HashSet<ElementId> GetSelectedSheetIds()
        {
            // Get selected sheet IDs (excluding the "not on sheet" ID)
            return _currentFilterItems
                .Where(f => f.IsSelected && f.Id != ElementId.InvalidElementId)
                .Select(f => f.Id)
                .ToHashSet();
        }

        private HashSet<ElementId> GetViewsFromSheets(IEnumerable<ElementId> selectedSheetIds)
        {

            // Find views on selected sheets
            var viewsOnSelectedSheets = new HashSet<ElementId>();
            if (selectedSheetIds.Any())
            {
                var viewports = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Viewport))
                    .Cast<Viewport>()
                    .Where(vp => selectedSheetIds.Contains(vp.Id));

                foreach (var viewport in viewports)
                {
                    viewsOnSelectedSheets.Add(viewport.ViewId);
                }
            }

            return viewsOnSelectedSheets;
        }

        private bool IsNotOnSheetSelected()
        {
            // Check if "not on sheet" is selected
            return _currentFilterItems
                .Any(f => f.IsSelected && f.Id == ElementId.InvalidElementId);
        }

        private HashSet<ElementId> GetAllViewsOnSheets()
        {
            return GetAllViewports().Select(vp => vp.ViewId).ToHashSet();
        }

        private IEnumerable<ViewItem> ViewsMatchingCriteria(HashSet<ElementId> viewsOnSelectedSheets, bool includeNotOnSheet, HashSet<ElementId> allViewsOnSheets)
        {

            // Return views that match the filter criteria
            return _allViewItems.Where(v =>
                viewsOnSelectedSheets.Contains(v.View.Id) ||
                (includeNotOnSheet && !allViewsOnSheets.Contains(v.View.Id)));
        }
        #endregion

        /// <summary>
        /// Updates the view count label in the UI
        /// </summary>
        private void UpdateViewCount()
        {
            var count = ViewItems?.Count ?? 0;
            ViewCountLabel.Text = $"({count} views)"; // Updates TextBlock in XAML
        }

        /// <summary>
        /// Called when a filter checkbox is checked/unchecked
        /// Connected via Checked/Unchecked events in XAML DataTemplate
        /// </summary>
        private void FilterItem_CheckChanged(object sender, RoutedEventArgs e)
        {
            UpdateViewDisplay();  // Re-filter and refresh the view list
        }

        private void ViewListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MarkSelectedAsSelected();
            MarkUnselectedAsUnselected();

            UpdateViewDisplay();  // Re-filter views or refresh the display
        }

        #region ViewListBox_SelectionChanged helpers

        private void MarkUnselectedAsUnselected()
        {
            foreach (ViewItem item in _allViewItems.Except(ViewListBox.SelectedItems.Cast<ViewItem>()))
                item.IsSelected = false;
        }

        private void MarkSelectedAsSelected()
        {
            foreach (ViewItem item in ViewListBox.SelectedItems)
                item.IsSelected = true;
        }

        private bool IsEmptyFilter()
        {
            return _currentFilterItems == null || !_currentFilterItems.Any();
        }
        #endregion

        /// <summary>
        /// "Select All" button for filters - connected via Click event in XAML
        /// </summary>
        private void SelectAllFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SetIsSelected(_currentFilterItems, true);
            UpdateViewDisplay();
        }

        private void SelectNoneFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            SetIsSelected(_currentFilterItems, false);
            UpdateViewDisplay();
        }

        

        private void SelectAllViewsButton_Click(object sender, RoutedEventArgs e)
        {
            SetIsSelected(ViewItems, true);
        }

        private void SelectNoneViewsButton_Click(object sender, RoutedEventArgs e)
        {
            SetIsSelected(ViewItems, false);
        }

        private void SetIsSelected(IEnumerable<SelectableItem> selectableItems, bool isSelected)
        {
            foreach (var item in selectableItems)
            {
                item.IsSelected = isSelected;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Build final list of selected Revit views
            SelectedViews = ViewItems
                .Where(item => item.IsSelected)
                .Select(item => item.View)
                .ToList();

            // Validate that at least one view is selected
            if (SelectedViews.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one view.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return; // Don't close dialog
            }

            DialogResult = true; // Indicates success to calling code
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Indicates cancellation to calling code
            Close();
        }

        
    }
}
