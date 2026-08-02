using CLab.Models;
using CLab.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.Views
{
    public partial class ScadenzarioView : UserControl
    {
        public ScadenzarioView()
        {
            InitializeComponent();
        }

        private void gridRitenute_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (gridRitenute.SelectedItem is RitenutaAcconto r && DataContext is ScadenzarioViewModel vm)
                vm.ModificaRitenutaCommand.Execute(r);
        }
    }
}