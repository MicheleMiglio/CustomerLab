using CLab.ViewModels;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CLab.Views
{
    /// <summary>
    /// View del modulo Password.
    ///
    /// I PasswordBox di WPF non sono bindabili (Password non è una
    /// DependencyProperty): i campi segreti vengono agganciati al ViewModel una
    /// sola volta, quando l'albero visuale è completo, e restano l'unico posto
    /// in cui la password esiste in chiaro mentre è digitata o mostrata.
    /// Il ViewModel non espone proprietà con segreti digitati e non conserva
    /// mai la password della credenziale nella lista.
    ///
    /// I controlli sono riconosciuti per AutomationId e non per x:Name: il
    /// contenuto del ModalDialogControl ha un namescope proprio, quindi un
    /// x:Name dentro il modal non sarebbe raggiungibile (ed è la causa tipica
    /// dell'errore MC3093 in compilazione XAML).
    ///
    /// Il DispatcherTimer notifica solo la UI (stato sessione e reazione alla
    /// scadenza del timeout): la validità reale è sempre decisa da
    /// PasswordSessionService, su riferimento monotono.
    /// </summary>
    public partial class PasswordView : UserControl
    {
        // --- AutomationId dei campi segreti (vedi PasswordView.xaml) ---

        private const string IdPasswordConfigurazione = "PasswordConfigurazione";
        private const string IdPasswordConfigurazioneConferma = "PasswordConfigurazioneConferma";
        private const string IdPasswordSblocco = "PasswordSblocco";
        private const string IdRecoveryCode = "TextBoxRecovery";
        private const string IdNuovaMaster = "PasswordNuovaMaster";
        private const string IdNuovaMasterConferma = "PasswordNuovaConferma";
        private const string IdPasswordForm = "PasswordForm";
        private const string IdPasswordFormChiara = "PasswordFormChiara";
        private const string IdPasswordGenerata = "PasswordGenerata";

        /// <summary>Cadenza del controllo di sessione: la UI si blocca entro
        /// un secondo dalla scadenza, senza polling pesante.</summary>
        private static readonly TimeSpan IntervalloNotifica = TimeSpan.FromSeconds(1);

        private readonly DispatcherTimer _timerNotifica;
        private readonly PasswordViewModel _viewModel;

        private bool _controlliCollegati;

        public PasswordView()
        {
            InitializeComponent();

            _viewModel = new PasswordViewModel();
            DataContext = _viewModel;

            _timerNotifica = new DispatcherTimer { Interval = IntervalloNotifica };
            _timerNotifica.Tick += TimerNotifica_Tick;

            Loaded += PasswordView_Loaded;
            Unloaded += PasswordView_Unloaded;
        }

        // --- Collegamento dei campi segreti ---

        private void PasswordView_Loaded(object sender, RoutedEventArgs e)
        {
            _timerNotifica.Start();

            if (_controlliCollegati)
                return;

            CollegaControlli();

            // Se il visual tree non era ancora completo (template del modal non
            // applicato), ritenta una volta a idle: i campi del form non devono
            // restare scollegati.
            if (_controlliCollegati)
                return;

            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() =>
                {
                    if (!_controlliCollegati)
                        CollegaControlli();
                }));
        }

        private void CollegaControlli()
        {
            var configurazione = TrovaControllo<PasswordBox>(this, IdPasswordConfigurazione);
            var configurazioneConferma = TrovaControllo<PasswordBox>(this, IdPasswordConfigurazioneConferma);
            var sblocco = TrovaControllo<PasswordBox>(this, IdPasswordSblocco);
            var recovery = TrovaControllo<TextBox>(this, IdRecoveryCode);
            var nuovaMaster = TrovaControllo<PasswordBox>(this, IdNuovaMaster);
            var nuovaMasterConferma = TrovaControllo<PasswordBox>(this, IdNuovaMasterConferma);
            var form = TrovaControllo<PasswordBox>(this, IdPasswordForm);
            var formChiara = TrovaControllo<TextBox>(this, IdPasswordFormChiara);
            var generata = TrovaControllo<PasswordBox>(this, IdPasswordGenerata);

            if (configurazione == null || configurazioneConferma == null || sblocco == null ||
                recovery == null || nuovaMaster == null || nuovaMasterConferma == null ||
                form == null || formChiara == null || generata == null)
            {
                return;
            }

            _viewModel.CollegaPasswordBox(
                configurazione,
                configurazioneConferma,
                sblocco,
                recovery,
                nuovaMaster,
                nuovaMasterConferma);

            _viewModel.CollegaControlliForm(form, formChiara, generata);

            _controlliCollegati = true;
        }

        /// <summary>
        /// Ricerca in profondità nel visual tree per AutomationId: funziona
        /// anche dentro il modal, che non espone nomi al namescope della View.
        /// </summary>
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

        // --- Notifica dello stato di sessione ---

        private void TimerNotifica_Tick(object? sender, EventArgs e)
        {
            _viewModel.AggiornaStato();
        }

        private void PasswordView_Unloaded(object sender, RoutedEventArgs e)
        {
            // La sessione resta valida (è il servizio a deciderlo): qui si ferma
            // solo il timer che notifica questa istanza di View.
            _timerNotifica.Stop();
        }

        // --- Eventi dei campi segreti del form ---

        /// <summary>Digitazione nella PasswordBox mascherata: modifica da salvare.</summary>
        private void PwForm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel.SegnaPasswordFormModificata();
        }

        /// <summary>Digitazione nel campo in chiaro: conta come modifica solo
        /// quando il campo è davvero visibile; le sincronizzazioni interne di
        /// apertura/chiusura del form non devono risultare modifiche.</summary>
        private void TxtFormChiara_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel.PasswordFormVisibile)
                _viewModel.SegnaPasswordFormModificata();
        }
    }
}