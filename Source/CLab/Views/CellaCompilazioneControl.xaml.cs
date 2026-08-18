using CLab.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace CLab.Views
{
    public partial class CellaCompilazioneControl : UserControl
    {
        public CellaCompilazioneControl()
        {
            InitializeComponent();
        }

        // Binding a senso unico + evento esplicito, stesso schema affidabile
        // già usato nella matrice di Attività per lo stesso identico problema.
        private void ChkSiNo_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is CellaCompilazione cella) cella.ValoreBooleano = true;
        }

        private void ChkSiNo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is CellaCompilazione cella) cella.ValoreBooleano = false;
        }

        private void PulisciNumero_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CellaCompilazione cella) cella.ValoreNumero = null;
        }

        private void PulisciTendina_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is CellaCompilazione cella) cella.ValoreTendina = null;
        }
    }
}