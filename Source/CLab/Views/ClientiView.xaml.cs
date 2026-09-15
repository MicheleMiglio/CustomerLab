using CLab.Models;
using CLab.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.Views
{
    public partial class ClientiView : UserControl
    {
        public ClientiView()
        {
            InitializeComponent();
        }

        // FASE 3: apertura/chiusura/animazione del pannello dettaglio sono
        // gestite da SidePanelControl (IsAperto bindato a OverlayAperto).

        private void ContattoTelefono_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.Tag is Contatti c &&
                DataContext is ClientiViewModel vm)

                vm.SelezionaTelefonoCommand.Execute(c);
        }


        private void ContattoEmail_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.Tag is Contatti c &&
                DataContext is ClientiViewModel vm)

                vm.SelezionaEmailCommand.Execute(c);
        }


        private void ModificaTelefono_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.Tag is Contatti c &&
                DataContext is ClientiViewModel vm)

                vm.SelezionaTelefonoCommand.Execute(c);
        }


        private void ModificaEmail_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe &&
                fe.Tag is Contatti c &&
                DataContext is ClientiViewModel vm)

                vm.SelezionaEmailCommand.Execute(c);
        }


        private void gridClienti_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (DataContext is ClientiViewModel vm &&
                gridClienti.SelectedItem is Cliente cliente)

                vm.ApriDettaglioCommand.Execute(cliente);
        }

        private void Referente_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is Referente r && DataContext is ClientiViewModel vm)
                vm.RinominaReferente(r);
        }

        private void Programma_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.Tag is Programma p && DataContext is ClientiViewModel vm)
                vm.RinominaProgramma(p);
        }
    }
}