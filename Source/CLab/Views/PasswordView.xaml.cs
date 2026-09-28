using CLab.ViewModels;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace CLab.Views
{
    public partial class PasswordView : UserControl
    {
        private readonly DispatcherTimer _timerNotifica;

        private PasswordViewModel? _viewModel;

        public PasswordView()
        {
            InitializeComponent();

            _viewModel = new PasswordViewModel();
            DataContext = _viewModel;

            // Collega i controlli sensibili del form al ViewModel.
            var pwForm = TrovaControllo<PasswordBox>(
    this,
    "PasswordForm");

            var txtFormChiara = TrovaControllo<TextBox>(
                this,
                "PasswordFormChiara");

            var pwGenerata = TrovaControllo<PasswordBox>(
                this,
                "PasswordGenerata");

            if (pwForm == null || txtFormChiara == null || pwGenerata == null)
            {
                throw new InvalidOperationException(
                    "Impossibile collegare i controlli del modulo Password.");
            }

            _viewModel.CollegaControlliForm(
                pwForm,
                txtFormChiara,
                pwGenerata);

            _timerNotifica = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };

            _timerNotifica.Tick += TimerNotifica_Tick;
            _timerNotifica.Start();

            Unloaded += PasswordView_Unloaded;
        }

        private static T? TrovaControllo<T>(
    DependencyObject parent,
    string automationId)
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

        private void TimerNotifica_Tick(object? sender, EventArgs e)
        {
            _viewModel?.AggiornaStato();
        }

        private void PasswordView_Unloaded(object sender, RoutedEventArgs e)
        {
            _timerNotifica.Stop();
        }

        private void PwForm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _viewModel?.SegnaPasswordFormModificata();
        }

        private void MostraRecoveryCode()
        {
            if (_viewModel == null)
                return;

            var codice = _viewModel.RecoveryCodeMonouso;

            if (string.IsNullOrWhiteSpace(codice))
                return;

            MessageBox.Show(
                "Conserva questo codice di recupero in un posto sicuro.\n\n" +
                "NON verrà mostrato mai più e non è recuperabile in alcun modo.\n" +
                "Serve unicamente per reimpostare la password principale " +
                "se la dimentichi.\n\n" +
                codice,
                "Codice di recupero — mostra una sola volta",
                MessageBoxButton.OK,
                MessageBoxImage.Warning,
                MessageBoxResult.OK);

            _viewModel.ChiudiRecoveryCodeCommand.Execute(null);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (_viewModel == null)
                return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PasswordViewModel.MostraRecoveryCode))
            {
                if (_viewModel?.MostraRecoveryCode == true)
                {
                    Dispatcher.BeginInvoke(
                        DispatcherPriority.Input,
                        new Action(MostraRecoveryCode));
                }
            }
        }
    }
}