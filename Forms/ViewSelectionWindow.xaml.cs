using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;

namespace HoloBlok.Forms
{
    /// <summary>
    /// Interaction logic for ViewSelectionWindow.xaml
    /// </summary>

    public partial class ViewSelectionWindow : Window
    {
        public List<ViewItem> ViewItems { get; set; }
        public List<View> SelectedViews { get; private set; }

        public ViewSelectionWindow(Document doc)
        {
            InitializeComponent();
            LoadViews(doc);
            ViewListBox.ItemsSource = ViewItems;
        }

        private void LoadViews(Document doc)
        {
            ViewItems = new List<ViewItem>();

            // Get all floor plans
            var planViews = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewPlan))
                .Cast<ViewPlan>()
                .Where(v => !v.IsTemplate && v.ViewType == ViewType.FloorPlan)
                .OrderBy(v => v.Name);

            foreach (var view in planViews)
            {
                ViewItems.Add(new ViewItem
                {
                    View = view,
                    ViewName = view.Name,
                    IsSelected = false
                });
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ViewItems)
            {
                item.IsSelected = true;
            }
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in ViewItems)
            {
                item.IsSelected = false;
            }
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

        private void FilterTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }

    public class ViewItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public View View { get; set; }
        public string ViewName { get; set; }

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