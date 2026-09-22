using CLab.ViewModels;
using System.Windows.Controls;

namespace CLab.Views
{
    public partial class StudiView : UserControl
    {
        public StudiView()
        {
            InitializeComponent();
        }

        /// <summary>Doppio click sulla riga: apre lo stesso modal usato da
        /// "Nuovo studio" e dall'azione "Modifica" (nessuna sidebar dedicata).</summary>
        private void gridStudi_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is StudiViewModel vm && gridStudi.SelectedItem is VoceStudio studio)
                vm.ModificaStudioCommand.Execute(studio);
        }

        /// <summary>Cancella rapido della ricerca: solo presentazione,
        /// il filtro resta una responsabilità del ViewModel (FiltroTesto).</summary>
        private void CancellaRicerca_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is StudiViewModel vm)
                vm.FiltroTesto = string.Empty;
        }
    }
}
