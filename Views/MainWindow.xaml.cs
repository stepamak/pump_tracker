using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using SolanaPumpTracker.ViewModels;
using System.Net.Http;
using System.Diagnostics;


namespace SolanaPumpTracker.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
