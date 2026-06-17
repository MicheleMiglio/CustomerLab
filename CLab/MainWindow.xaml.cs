using CLab.ViewModels;
using System.Windows;

namespace CLab
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ClientiViewModel();
        }
    }
}