using System;
using System.Collections.Generic;
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

namespace HoloBlok.Forms
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        private bool _isCancelled = false;

        public bool IsCancelled => _isCancelled;

        public ProgressDialog()
        {
            InitializeComponent();
        }

        public void UpdateProgress(int current, int total, string status, string detail = "")
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Maximum = total;
                ProgressBar.Value = current;
                StatusText.Text = status;
                DetailText.Text = detail;
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            CancelButton.IsEnabled = false;
            StatusText.Text = "Cancelling...";
        }
    }
}
