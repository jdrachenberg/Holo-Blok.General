using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using CheckBox = System.Windows.Controls.CheckBox;

namespace HoloBlok.Forms
{
    public partial class ViewSelectionWindow : Window
    {
        private Document _doc;
        private List<ViewItem> _allViewItems;
        private List<FilterItem> _currentFilterItems = new List<FilterItem>();
        private int _lastSelectedIndex = -1;

        public List<ViewItem> ViewItems { get; set; }
        public List<View> SelectedViews { get; private set; }

        public ViewSelectionWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            // Defer initialization until window is loaded
            this.Loaded += ViewSelectionWindow_Loaded;
        }

        private void ViewSelectionWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAllViews();
            LoadFilters();
            UpdateViewDisplay();
        }

        private void LoadAllViews()
        {
            _allViewItems = new List<ViewItem>();

            // Get all floor plans
            var planViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan)
                .OrderBy(v => v.Name);

            foreach (var view in planViews)
            {
                _allViewItems.Add(new ViewItem
                {
                    View = view,
                    ViewName = view.Name,
                    IsSelected = false,
                    ViewTemplateId = view.ViewTemplateId
                });
            }
        }

        private void LoadFilters()
        {
            FilterListBox.ItemsSource = _currentFilterItems;

            // Start with "All Views" selected - no filters needed
            FilterButtonsPanel.Visibility = System.Windows.Visibility.Collapsed;
            FilterListBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void FilterTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Skip if window isn't loaded yet
            if (!IsLoaded)
                return;

            var selectedItem = FilterTypeComboBox.SelectedItem as ComboBoxItem;
            var filterType = selectedItem?.Content.ToString();

            _currentFilterItems.Clear();

            switch (filterType)
            {
                case "All Views":
                    FilterButtonsPanel.Visibility = System.Windows.Visibility.Collapsed;
                    FilterListBox.Visibility = System.Windows.Visibility.Collapsed;
                    break;

                case "View Templates":
                    LoadViewTemplateFilters();
                    FilterButtonsPanel.Visibility = System.Windows.Visibility.Visible;
                    FilterListBox.Visibility = System.Windows.Visibility.Visible;
                    break;

                case "Sheets":
                    LoadSheetFilters();
                    FilterButtonsPanel.Visibility = System.Windows.Visibility.Visible;
                    FilterListBox.Visibility = System.Windows.Visibility.Visible;
                    break;
            }

            FilterListBox.Items.Refresh();
            UpdateViewDisplay();
        }

        private void LoadViewTemplateFilters()
        {
            // Get all view templates used by floor plans
            var templateIds = _allViewItems
                .Where(v => v.ViewTemplateId != ElementId.InvalidElementId)
                .Select(v => v.ViewTemplateId)
                .Distinct();

            foreach (var templateId in templateIds)
            {
                var template = _doc.GetElement(templateId) as View;
                if (template != null)
                {
                    var viewCount = _allViewItems.Count(v => v.ViewTemplateId == templateId);
                    _currentFilterItems.Add(new FilterItem
                    {
                        Id = templateId,
                        DisplayName = $"{template.Name} ({viewCount} views)",
                        IsSelected = true // Start with all selected
                    });
                }
            }

            // Add item for views with no template
            var noTemplateCount = _allViewItems.Count(v => v.ViewTemplateId == ElementId.InvalidElementId);
            if (noTemplateCount > 0)
            {
                _currentFilterItems.Add(new FilterItem
                {
                    Id = ElementId.InvalidElementId,
                    DisplayName = $"No Template ({noTemplateCount} views)",
                    IsSelected = false
                });
            }
        }

        private void LoadSheetFilters()
        {
            // Get all sheets that contain floor plan views
            var sheetsWithViews = new Dictionary<ElementId, string>();
            var viewSheetCounts = new Dictionary<ElementId, int>();

            foreach (var viewItem in _allViewItems)
            {
                // Get viewports that reference this view
                var viewports = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Viewport))
                    .Cast<Viewport>()
                    .Where(vp => vp.ViewId == viewItem.View.Id);

                foreach (var viewport in viewports)
                {
                    var sheet = _doc.GetElement(viewport.SheetId) as ViewSheet;
                    if (sheet != null)
                    {
                        if (!sheetsWithViews.ContainsKey(sheet.Id))
                        {
                            sheetsWithViews[sheet.Id] = $"{sheet.SheetNumber} - {sheet.Name}";
                            viewSheetCounts[sheet.Id] = 0;
                        }
                        viewSheetCounts[sheet.Id]++;
                    }
                }
            }

            foreach (var sheet in sheetsWithViews)
            {
                _currentFilterItems.Add(new FilterItem
                {
                    Id = sheet.Key,
                    DisplayName = $"{sheet.Value} ({viewSheetCounts[sheet.Key]} views)",
                    IsSelected = true
                });
            }

            // Add item for views not on any sheet
            var viewsOnSheets = new HashSet<ElementId>();
            var allViewports = new FilteredElementCollector(_doc)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>();

            foreach (var viewport in allViewports)
            {
                viewsOnSheets.Add(viewport.ViewId);
            }

            var notOnSheetCount = _allViewItems.Count(v => !viewsOnSheets.Contains(v.View.Id));
            if (notOnSheetCount > 0)
            {
                _currentFilterItems.Add(new FilterItem
                {
                    Id = new ElementId(-1), // Special ID for "not on sheet"
                    DisplayName = $"Not on Sheet ({notOnSheetCount} views)",
                    IsSelected = false
                });
            }
        }

        private void UpdateViewDisplay()
        {
            var filteredViews = GetFilteredViews();

            ViewItems = filteredViews.ToList();
            ViewListBox.ItemsSource = ViewItems;

            UpdateViewCount();
        }

        private IEnumerable<ViewItem> GetFilteredViews()
        {
            var selectedItem = FilterTypeComboBox.SelectedItem as ComboBoxItem;
            var filterType = selectedItem?.Content.ToString();

            switch (filterType)
            {
                case "All Views":
                    return _allViewItems;

                case "View Templates":
                    if (_currentFilterItems == null || !_currentFilterItems.Any())
                        return _allViewItems;

                    var selectedTemplateIds = _currentFilterItems
                        .Where(f => f.IsSelected)
                        .Select(f => f.Id)
                        .ToHashSet();

                    return _allViewItems.Where(v => selectedTemplateIds.Contains(v.ViewTemplateId));

                case "Sheets":
                    if (_currentFilterItems == null || !_currentFilterItems.Any())
                        return _allViewItems;

                    var selectedSheetIds = _currentFilterItems
                        .Where(f => f.IsSelected && f.Id.IntegerValue != -1)
                        .Select(f => f.Id)
                        .ToHashSet();

                    var includeNotOnSheet = _currentFilterItems
                        .Any(f => f.IsSelected && f.Id.IntegerValue == -1);

                    var viewsOnSelectedSheets = new HashSet<ElementId>();
                    if (selectedSheetIds.Any())
                    {
                        var viewports = new FilteredElementCollector(_doc)
                            .OfClass(typeof(Viewport))
                            .Cast<Viewport>()
                            .Where(vp => selectedSheetIds.Contains(vp.SheetId));

                        foreach (var viewport in viewports)
                        {
                            viewsOnSelectedSheets.Add(viewport.ViewId);
                        }
                    }

                    var allViewsOnSheets = new HashSet<ElementId>();
                    if (includeNotOnSheet)
                    {
                        var allViewports = new FilteredElementCollector(_doc)
                            .OfClass(typeof(Viewport))
                            .Cast<Viewport>();

                        foreach (var viewport in allViewports)
                        {
                            allViewsOnSheets.Add(viewport.ViewId);
                        }
                    }

                    return _allViewItems.Where(v =>
                        viewsOnSelectedSheets.Contains(v.View.Id) ||
                        (includeNotOnSheet && !allViewsOnSheets.Contains(v.View.Id)));

                default:
                    return _allViewItems;
            }
        }

        private void UpdateViewCount()
        {
            var count = ViewItems?.Count ?? 0;
            ViewCountLabel.Text = $"({count} views)";
        }

        private void FilterItem_CheckChanged(object sender, RoutedEventArgs e)
        {
            UpdateViewDisplay();
        }

        private void ViewItem_CheckChanged(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var viewItem = checkBox?.DataContext as ViewItem;

            if (viewItem == null) return;

            var currentIndex = ViewItems.IndexOf(viewItem);

            // Handle Shift+Click for range selection
            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift
                && _lastSelectedIndex >= 0
                && _lastSelectedIndex != currentIndex)
            {
                var startIndex = Math.Min(_lastSelectedIndex, currentIndex);
                var endIndex = Math.Max(_lastSelectedIndex, currentIndex);

                // Set all items in range to the same state as the clicked item
                for (int i = startIndex; i <= endIndex; i++)
                {
                    ViewItems[i].IsSelected = viewItem.IsSelected;
                }

                ViewListBox.Items.Refresh();
            }

            _lastSelectedIndex = currentIndex;
        }

        private void SelectAllFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _currentFilterItems)
            {
                item.IsSelected = true;
            }
            FilterListBox.Items.Refresh();
            UpdateViewDisplay();
        }

        private void DeselectAllFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _currentFilterItems)
            {
                item.IsSelected = false;
            }
            FilterListBox.Items.Refresh();
            UpdateViewDisplay();
        }

        private void SelectAllViewsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ViewItems)
            {
                item.IsSelected = true;
            }
            ViewListBox.Items.Refresh();
            _lastSelectedIndex = -1; // Reset
        }

        private void DeselectAllViewsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ViewItems)
            {
                item.IsSelected = false;
            }
            ViewListBox.Items.Refresh();
            _lastSelectedIndex = -1; // Reset
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedViews = ViewItems
                .Where(item => item.IsSelected)
                .Select(item => item.View)
                .ToList();

            if (SelectedViews.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select at least one view.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    public class ViewItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public View View { get; set; }
        public string ViewName { get; set; }
        public ElementId ViewTemplateId { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FilterItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ElementId Id { get; set; }
        public string DisplayName { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}