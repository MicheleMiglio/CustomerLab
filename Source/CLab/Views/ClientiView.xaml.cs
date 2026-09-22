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

        // CLab 2.0: cancellazione rapida della ricerca (X nel campo, pattern Fatture)
        private void CancellaRicerca_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ClientiViewModel vm)
                vm.FiltroTesto = string.Empty;
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

        private void ScrollModifica_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void ScrollViewerModifica_Loaded(object sender, RoutedEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            // handledEventsToo: true → l'handler scatta SEMPRE, anche se qualcosa
            // a monte ha già marcato l'evento come gestito
            sv.AddHandler(UIElement.PreviewMouseWheelEvent,
                new MouseWheelEventHandler(ScrollViewer_PreviewMouseWheel), true);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var sv = (ScrollViewer)sender;
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}