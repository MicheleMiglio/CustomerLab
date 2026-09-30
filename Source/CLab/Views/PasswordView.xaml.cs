using CLab.ViewModels;
using System;
using System.ComponentModel;
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
    /// sola volta, quando l'albero è pronto, e restano l'unico posto
    /// in cui la password esiste in chiaro mentre è digitata o mostrata.
    /// Il ViewModel non espone proprietà con segreti digitati e non conserva
    /// mai la password della credenziale nella lista.
    ///
    /// I controlli sono riconosciuti per AutomationId e non per x:Name: il
    /// contenuto del ModalDialogControl ha un namescope proprio, quindi un
    /// x:Name dentro il modal non sarebbe raggiungibile (ed è la causa tipica
    /// dell'errore MC3093 in compilazione XAML).
    ///
    /// Il collegamento è diviso in due gruppi indipendenti (nessuna condizione
    /// "tutto-o-niente"):
    ///  - accesso/configurazione: le card sono nel visual tree della View fin
    ///    dal caricamento, quindi si collegano subito (la configurazione
    ///    iniziale funziona anche se il form del modal non è ancora presente);
    ///  - form del modal: il contenuto di ModalDialogControl.Contenuto esiste
    ///    come oggetto fin dal caricamento anche a dialog chiuso, quindi i
    ///    campi vengono cercati dentro quel contenuto senza attendere la
    ///    prima apertura del pannello. Il ritentativo all'apertura del
    ///    pannello resta come rete di sicurezza.
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

        private bool _controlliAccessoCollegati;
        private bool _controlliFormCollegati;

        public PasswordView()
        {
            InitializeComponent();

            _viewModel = new PasswordViewModel();
            DataContext = _viewModel;

            _timerNotifica = new DispatcherTimer { Interval = IntervalloNotifica };
            _timerNotifica.Tick += TimerNotifica_Tick;

            // Rete di sicurezza: se i campi del form non erano ancora collegati,
            // si ritenta quando il pannello viene aperto.
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            Loaded += PasswordView_Loaded;
            Unloaded += PasswordView_Unloaded;
        }

        // --- Collegamento dei campi segreti ---

        private void PasswordView_Loaded(object sender, RoutedEventArgs e)
        {
            _timerNotifica.Start();

            CollegaControlliMancanti();

            if (_controlliAccessoCollegati && _controlliFormCollegati)
                return;

            // Se qualcosa non era ancora pronto, ritenta una volta a idle:
            // nessun gruppo deve restare scollegato per colpa dell'altro.
            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(CollegaControlliMancanti));
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(PasswordViewModel.PannelloAperto) ||
                !_viewModel.PannelloAperto ||
                _controlliFormCollegati)
            {
                return;
            }

            // Il pannello si è appena aperto: il contenuto del modal entra nel
            // visual tree. Se il form non era già collegato, ritenta dopo il
            // layout.
            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(CollegaControlliMancanti));
        }

        /// <summary>Collega i gruppi di controlli ancora scollegati.</summary>
        private void CollegaControlliMancanti()
        {
            CollegaControlliAccesso();
            CollegaControlliForm();
        }

        /// <summary>
        /// Campi di accesso (configurazione, sblocco, recupero): vivono nelle
        /// card della View e sono nel visual tree fin dal caricamento.
        /// </summary>
        private void CollegaControlliAccesso()
        {
            if (_controlliAccessoCollegati)
                return;

            var configurazione = TrovaControllo<PasswordBox>(this, IdPasswordConfigurazione);
            var configurazioneConferma = TrovaControllo<PasswordBox>(this, IdPasswordConfigurazioneConferma);
            var sblocco = TrovaControllo<PasswordBox>(this, IdPasswordSblocco);
            var recovery = TrovaControllo<TextBox>(this, IdRecoveryCode);
            var nuovaMaster = TrovaControllo<PasswordBox>(this, IdNuovaMaster);
            var nuovaMasterConferma = TrovaControllo<PasswordBox>(this, IdNuovaMasterConferma);

            if (configurazione == null || configurazioneConferma == null || sblocco == null ||
                recovery == null || nuovaMaster == null || nuovaMasterConferma == null)
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

            _controlliAccessoCollegati = true;
        }

        /// <summary>
        /// Campi del form credenziale: vivono nel contenuto del modal. Il
        /// contenuto esiste come oggetto anche a dialog chiuso (è uno
        /// StackPanel reale, non un DataTemplate), quindi la ricerca parte da
        /// ModalDialogControl.Contenuto e non attende la prima apertura del
        /// pannello.
        /// </summary>
        private void CollegaControlliForm()
        {
            if (_controlliFormCollegati)
                return;

            var modal = TrovaControlloPerTipo<ModalDialogControl>(this);
            var radiceForm = modal?.Contenuto as DependencyObject;

            if (radiceForm == null)
                return;

            var form = TrovaControllo<PasswordBox>(radiceForm, IdPasswordForm);
            var formChiara = TrovaControllo<TextBox>(radiceForm, IdPasswordFormChiara);
            var generata = TrovaControllo<PasswordBox>(radiceForm, IdPasswordGenerata);

            if (form == null || formChiara == null || generata == null)
                return;

            _viewModel.CollegaControlliForm(form, formChiara, generata);

            _controlliFormCollegati = true;
        }

        /// <summary>
        /// Ricerca in profondità nel visual tree per AutomationId: funziona
        /// anche dentro il modal, che non espone nomi al namescope della View,
        /// e anche su un sottoalbero non ancora agganciato alla finestra (il
        /// contenuto del modal prima dell'apertura), perché gli elementi
        /// mantengono i propri figli visuali interni.
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

        /// <summary>Ricerca in profondità nel visual tree per tipo di controllo.</summary>
        private static T? TrovaControlloPerTipo<T>(DependencyObject parent)
            where T : DependencyObject
        {
            if (parent == null)
                return null;

            var count = VisualTreeHelper.GetChildrenCount(parent);

            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T controllo)
                    return controllo;

                var risultato = TrovaControlloPerTipo<T>(child);

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
        // --- Doppio click sulla griglia ---

        /// <summary>
        /// Doppio click su una riga della lista: apre la voce in modifica
        /// riutilizzando lo stesso comando del pulsante "Modifica" (nessuna
        /// logica duplicata). Un doppio click su header, scrollbar, area
        /// vuota o su una riga che non contiene una credenziale non fa nulla;
        /// il click singolo resta la normale selezione.
        /// </summary>
        private void VociDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TrovaAntenato<DataGridRow>(e.OriginalSource as DependencyObject) is not DataGridRow riga ||
                riga.DataContext is not PasswordVoceViewModel voce)
            {
                return;
            }

            if (_viewModel.ModificaVoceCommand.CanExecute(voce))
                _viewModel.ModificaVoceCommand.Execute(voce);
        }

        /// <summary>Risale il visual tree fino al primo antenato del tipo richiesto.</summary>
        private static T? TrovaAntenato<T>(DependencyObject? elemento)
            where T : DependencyObject
        {
            while (elemento != null)
            {
                if (elemento is T trovato)
                    return trovato;

                elemento = VisualTreeHelper.GetParent(elemento);
            }

            return null;
        }
    }
}