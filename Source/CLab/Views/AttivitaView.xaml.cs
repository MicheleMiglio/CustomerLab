using CLab.Models;
using CLab.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.Views
{
    public partial class AttivitaView : UserControl
    {
        public AttivitaView()
        {
            InitializeComponent();
        }

        private void gridAttivita_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AttivitaViewModel vm && gridAttivita.SelectedItem is Attivita attivita)
                vm.ModificaCommand.Execute(attivita);
        }
    }
}