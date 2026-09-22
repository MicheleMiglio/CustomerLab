using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CLab.Views
{
    /// <summary>
    /// Modal dialog CLab 2.0: contenitore condiviso per i form dei moduli.
    /// Solo logica di presentazione: scrim neutro, dialog centrato con
    /// header sticky, body scrollabile, footer sticky, chiusura da
    /// overlay / Esc / X con conferma se ci sono modifiche non salvate,
    /// fade discreto in apertura/chiusura. I contenuti, i comandi e lo
    /// stato restano a VM/View (stesso contratto di SidePanelControl).
    /// </summary>
    public partial class ModalDialogControl : UserControl
    {
        public static readonly DependencyProperty IsApertoProperty = DependencyProperty.Register(
            nameof(IsAperto), typeof(bool), typeof(ModalDialogControl),
            new PropertyMetadata(false, SuIsApertoChanged));

        public static readonly DependencyProperty TitoloProperty = DependencyProperty.Register(
            nameof(Titolo), typeof(string), typeof(ModalDialogControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SottotitoloProperty = DependencyProperty.Register(
            nameof(Sottotitolo), typeof(string), typeof(ModalDialogControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ContenutoProperty = DependencyProperty.Register(
            nameof(Contenuto), typeof(object), typeof(ModalDialogControl), new PropertyMetadata(null));

        /// <summary>Contenuto facoltativo nell'header, sotto il titolo (es. schede a tab).</summary>
        public static readonly DependencyProperty HeaderExtraProperty = DependencyProperty.Register(
            nameof(HeaderExtra), typeof(object), typeof(ModalDialogControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ComandoChiudiProperty = DependencyProperty.Register(
            nameof(ComandoChiudi), typeof(ICommand), typeof(ModalDialogControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ComandoSalvaProperty = DependencyProperty.Register(
            nameof(ComandoSalva), typeof(ICommand), typeof(ModalDialogControl), new PropertyMetadata(null));

        public static readonly DependencyProperty EtichettaSalvaProperty = DependencyProperty.Register(
            nameof(EtichettaSalva), typeof(string), typeof(ModalDialogControl), new PropertyMetadata("Salva"));

        public static readonly DependencyProperty MostraFooterProperty = DependencyProperty.Register(
            nameof(MostraFooter), typeof(bool), typeof(ModalDialogControl), new PropertyMetadata(true));

        public static readonly DependencyProperty HaModificheNonSalvateProperty = DependencyProperty.Register(
            nameof(HaModificheNonSalvate), typeof(bool), typeof(ModalDialogControl), new PropertyMetadata(false));

        /// <summary>
        /// Larghezza extra opzionale del dialog (0 = comportamento standard del
        /// converter, usato dai moduli esistenti). Il modulo Clienti la usa per
        /// la scheda cliente, più ampia per la quantità di dati. La larghezza
        /// effettiva non supera mai lo spazio disponibile nella finestra.
        /// </summary>
        public static readonly DependencyProperty LarghezzaExtraProperty = DependencyProperty.Register(
            nameof(LarghezzaExtra), typeof(double), typeof(ModalDialogControl), new PropertyMetadata(0d));

        public bool IsAperto
        {
            get => (bool)GetValue(IsApertoProperty);
            set => SetValue(IsApertoProperty, value);
        }

        public string Titolo
        {
            get => (string)GetValue(TitoloProperty);
            set => SetValue(TitoloProperty, value);
        }

        public string Sottotitolo
        {
            get => (string)GetValue(SottotitoloProperty);
            set => SetValue(SottotitoloProperty, value);
        }

        public object? Contenuto
        {
            get => GetValue(ContenutoProperty);
            set => SetValue(ContenutoProperty, value);
        }

        public object? HeaderExtra
        {
            get => GetValue(HeaderExtraProperty);
            set => SetValue(HeaderExtraProperty, value);
        }

        public ICommand? ComandoChiudi
        {
            get => (ICommand?)GetValue(ComandoChiudiProperty);
            set => SetValue(ComandoChiudiProperty, value);
        }

        public ICommand? ComandoSalva
        {
            get => (ICommand?)GetValue(ComandoSalvaProperty);
            set => SetValue(ComandoSalvaProperty, value);
        }

        public string EtichettaSalva
        {
            get => (string)GetValue(EtichettaSalvaProperty);
            set => SetValue(EtichettaSalvaProperty, value);
        }

        public bool MostraFooter
        {
            get => (bool)GetValue(MostraFooterProperty);
            set => SetValue(MostraFooterProperty, value);
        }

        public bool HaModificheNonSalvate
        {
            get => (bool)GetValue(HaModificheNonSalvateProperty);
            set => SetValue(HaModificheNonSalvateProperty, value);
        }

        public double LarghezzaExtra
        {
            get => (double)GetValue(LarghezzaExtraProperty);
            set => SetValue(LarghezzaExtraProperty, value);
        }

        private bool _dialogVisibile;
        private Window? _windowCollegata;

        public ModalDialogControl()
        {
            InitializeComponent();
        }

        private static void SuIsApertoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var controllo = (ModalDialogControl)d;

            if ((bool)e.NewValue)
            {
                controllo._dialogVisibile = true;
                controllo.Visibility = Visibility.Visible;
                controllo.ApplicaLarghezzaExtra();
                controllo.AnimaOpacita(0, 1, 160);

                // Focus sul controllo: rende Esc operativo anche se l'utente
                // non ha ancora cliccato dentro il dialog.
                controllo.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                    new Action(() => controllo.Focus()));
            }
            else if (controllo._dialogVisibile)
            {
                controllo._dialogVisibile = false;
                controllo.StaccaFinestra();
                controllo.AnimaOpacita(1, 0, 130);
                controllo.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        if (!controllo._dialogVisibile)
                            controllo.Visibility = Visibility.Collapsed;
                    }));
            }
        }

        /// <summary>
        /// Larghezza extra (solo se LarghezzaExtra > 0): aggancia il dialog alla
        /// larghezza reale della finestra, restando dentro i margini. Con
        /// LarghezzaExtra = 0 non fa nulla: il converter standard resta attivo.
        /// </summary>
        private void ApplicaLarghezzaExtra()
        {
            if (LarghezzaExtra <= 0) return;

            _windowCollegata = Window.GetWindow(this);
            if (_windowCollegata != null)
            {
                _windowCollegata.SizeChanged += SuSizeChangedFinestra;
                AggiornaLarghezzaDialog(_windowCollegata.ActualWidth);
            }
        }

        private void SuSizeChangedFinestra(object sender, SizeChangedEventArgs e)
            => AggiornaLarghezzaDialog(e.NewSize.Width);

        private void AggiornaLarghezzaDialog(double larghezzaFinestra)
        {
            if (LarghezzaExtra <= 0) return;

            double disponibile = larghezzaFinestra > 0 ? larghezzaFinestra - 80 : LarghezzaExtra;
            dialog.Width = Math.Min(LarghezzaExtra, Math.Max(560, disponibile));
        }

        /// <summary>Rimuove l'aggancio al resize quando il dialog si chiude.</summary>
        private void StaccaFinestra()
        {
            if (_windowCollegata != null)
            {
                _windowCollegata.SizeChanged -= SuSizeChangedFinestra;
                _windowCollegata = null;
            }
        }

        /// <summary>Fade discreto del dialog (solo presentazione, nessun movimento).</summary>
        private void AnimaOpacita(double da, double a, int durataMs)
        {
            var anim = new DoubleAnimation
            {
                From = da,
                To = a,
                Duration = TimeSpan.FromMilliseconds(durataMs)
            };

            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        /// <summary>Chiusura richiesta (overlay / Esc / X): conferma se modifiche non salvate.</summary>
        private void RichiediChiusura()
        {
            if (HaModificheNonSalvate)
            {
                var esito = MessageBox.Show(
                    "Ci sono modifiche non salvate. Chiudere comunque la finestra?",
                    "Modifiche non salvate",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

                if (esito != MessageBoxResult.Yes)
                    return;
            }

            ComandoChiudi?.Execute(null);
        }

        private void SuClickOverlay(object sender, MouseButtonEventArgs e) => RichiediChiusura();

        private void SuClickChiudi(object sender, RoutedEventArgs e) => RichiediChiusura();

        private void SuTasto(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RichiediChiusura();
                e.Handled = true;
            }
        }
    }
}

