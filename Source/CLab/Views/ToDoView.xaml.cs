using CLab.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.Views
{
    public partial class ToDoView : UserControl
    {
        public ToDoView()
        {
            InitializeComponent();
        }

        /// <summary>Cancella rapido della ricerca: solo presentazione,
        /// il filtro resta una responsabilità del ViewModel (FiltroTesto).</summary>
        private void CancellaRicerca_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToDoViewModel vm)
                vm.FiltroTesto = string.Empty;
        }

        private void NuovaSottoAttivita_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (DataContext is ToDoViewModel vm && vm.AggiungiSottoAttivitaCommand.CanExecute(null))
                vm.AggiungiSottoAttivitaCommand.Execute(null);
        }
    }
}
