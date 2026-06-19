using CLab.Models;
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

        private void ContattoTelefono_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Contatti contatto && DataContext is ClientiViewModel vm)
            {
                vm.SelezionaTelefonoCommand.Execute(contatto);
            }
        }

        private void ContattoEmail_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is Contatti contatto && DataContext is ClientiViewModel vm)
            {
                vm.SelezionaEmailCommand.Execute(contatto);
            }
        }
    }
}