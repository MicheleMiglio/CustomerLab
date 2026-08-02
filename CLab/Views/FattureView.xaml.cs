using CLab.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.Views
{
    public partial class FattureView : UserControl
    {
        public FattureView()
        {
            InitializeComponent();
        }

        private void gridFatture_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (gridFatture.SelectedItem is RigaFattura r && DataContext is FattureViewModel vm)
                vm.ModificaCommand.Execute(r);
        }
    }
}