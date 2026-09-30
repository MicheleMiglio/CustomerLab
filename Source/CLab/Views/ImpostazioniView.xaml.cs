using CLab.ViewModels;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CLab.Views
{
    public partial class ImpostazioniView : UserControl
    {
        // --- AutomationId dei campi segreti del recupero (vedi ImpostazioniView.xaml) ---

        private const string IdCodiceRecupero = "RecoveryCodeImpostazioni";
        private const string IdNuovaMaster = "NuovaMasterImpostazioni";
        private const string IdNuovaMasterConferma = "NuovaMasterConfermaImpostazioni";

        private bool _controlliPasswordCollegati;

        public ImpostazioniView()
        {
            InitializeComponent();

            Loaded += ImpostazioniView_Loaded;
        }

        /// <summary>
        /// A ogni apertura della pagina: rilegge lo stato del modulo Password
        /// (configurazione e validità della sessione) e collega i campi segreti
        /// del recupero. I PasswordBox non sono bindabili, quindi vengono
        /// riconosciuti per AutomationId quando l'albero visuale è pronto.
        /// </summary>
        private void ImpostazioniView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ImpostazioniViewModel viewModel)
                viewModel.AggiornaStatoPassword();

            if (_controlliPasswordCollegati)
                return;

            CollegaControlliPassword();

            if (_controlliPasswordCollegati)
                return;

            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() =>
                {
                    if (!_controlliPasswordCollegati)
                        CollegaControlliPassword();
                }));
        }

        private void CollegaControlliPassword()
        {
            if (DataContext is not ImpostazioniViewModel viewModel)
                return;

            var codiceRecupero = TrovaControllo<TextBox>(this, IdCodiceRecupero);
            var nuovaMaster = TrovaControllo<PasswordBox>(this, IdNuovaMaster);
            var nuovaMasterConferma = TrovaControllo<PasswordBox>(this, IdNuovaMasterConferma);

            if (codiceRecupero == null || nuovaMaster == null || nuovaMasterConferma == null)
                return;

            viewModel.CollegaControlliPassword(codiceRecupero, nuovaMaster, nuovaMasterConferma);

            _controlliPasswordCollegati = true;
        }

        private static T? TrovaControllo<T>(DependencyObject parent, string automationId)
            where T : DependencyObject
        {
            if (parent == null)
                return null;

            var count = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T controllo &&
                    AutomationProperties.GetAutomationId(controllo) == automationId)
                {
                    return controllo;
                }

                var risultato = TrovaControllo<T>(child, automationId);

                if (risultato != null)
                    return risultato;
            }

            return null;
        }
    }
}