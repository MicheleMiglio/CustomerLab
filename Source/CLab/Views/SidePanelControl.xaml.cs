using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CLab.Views
{
    /// <summary>
    /// Contenitore condiviso per l'editing a pannello laterale (FASE 3 CLab 2.0).
    /// Solo logica di presentazione: overlay, header sticky, body scrollabile,
    /// footer sticky, larghezza dinamica, chiusura da overlay / Esc / pulsante
    /// con conferma se ci sono modifiche non salvate, animazione di slide.
    /// I contenuti, i comandi e lo stato restano a VM/View.
    /// </summary>
    public partial class SidePanelControl : UserControl
    {
        public static readonly DependencyProperty IsApertoProperty = DependencyProperty.Register(
            nameof(IsAperto), typeof(bool), typeof(SidePanelControl),
            new PropertyMetadata(false, SuIsApertoChanged));

        public static readonly DependencyProperty TitoloProperty = DependencyProperty.Register(
            nameof(Titolo), typeof(string), typeof(SidePanelControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SottotitoloProperty = DependencyProperty.Register(
            nameof(Sottotitolo), typeof(string), typeof(SidePanelControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ContenutoProperty = DependencyProperty.Register(
            nameof(Contenuto), typeof(object), typeof(SidePanelControl), new PropertyMetadata(null));

        /// <summary>Contenuto facoltativo nell'header, sotto il titolo (es. schede a tab).</summary>
        public static readonly DependencyProperty HeaderExtraProperty = DependencyProperty.Register(
            nameof(HeaderExtra), typeof(object), typeof(SidePanelControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ComandoChiudiProperty = DependencyProperty.Register(
            nameof(ComandoChiudi), typeof(ICommand), typeof(SidePanelControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ComandoSalvaProperty = DependencyProperty.Register(
            nameof(ComandoSalva), typeof(ICommand), typeof(SidePanelControl), new PropertyMetadata(null));

        public static readonly DependencyProperty EtichettaSalvaProperty = DependencyProperty.Register(
            nameof(EtichettaSalva), typeof(string), typeof(SidePanelControl), new PropertyMetadata("Salva"));

        public static readonly DependencyProperty MostraFooterProperty = DependencyProperty.Register(
            nameof(MostraFooter), typeof(bool), typeof(SidePanelControl), new PropertyMetadata(true));

        public static readonly DependencyProperty HaModificheNonSalvateProperty = DependencyProperty.Register(
            nameof(HaModificheNonSalvate), typeof(bool), typeof(SidePanelControl), new PropertyMetadata(false));

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

        private bool _pannelloVisibile;

        public SidePanelControl()
        {
            InitializeComponent();
        }

        private static void SuIsApertoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var controllo = (SidePanelControl)d;

            if ((bool)e.NewValue)
            {
                controllo._pannelloVisibile = true;
                controllo.Visibility = Visibility.Visible;
                controllo.AnimaPannello(1, 0, 280, easeOut: true);

                // Focus sul controllo: rende ESC operativo anche se l'utente
                // non ha ancora cliccato dentro il pannello.
                controllo.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                    new Action(() => controllo.Focus()));
            }
            else if (controllo._pannelloVisibile)
            {
                controllo._pannelloVisibile = false;
                controllo.AnimaPannello(0, 1, 220, easeOut: false);
                controllo.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                    {
                        if (!controllo._pannelloVisibile)
                            controllo.Visibility = Visibility.Collapsed;
                    }));
            }
        }

        /// <summary>Slide orizzontale del pannello (solo presentazione, X in frazioni della larghezza).</summary>
        private void AnimaPannello(double fromFrazione, double toFrazione, int durataMs, bool easeOut)
        {
            var larghezza = pannello.ActualWidth > 0 ? pannello.ActualWidth : 420;

            var anim = new DoubleAnimation
            {
                From = larghezza * fromFrazione,
                To = larghezza * toFrazione,
                Duration = TimeSpan.FromMilliseconds(durataMs),
                EasingFunction = new CubicEase
                {
                    EasingMode = easeOut ? EasingMode.EaseOut : EasingMode.EaseIn
                }
            };

            trasformazionePannello.BeginAnimation(
                System.Windows.Media.TranslateTransform.XProperty, anim);
        }

        /// <summary>Chiusura richiesta (overlay / Esc / pulsante): conferma se modifiche non salvate.</summary>
        private void RichiediChiusura()
        {
            if (HaModificheNonSalvate)
            {
                var esito = MessageBox.Show(
                    "Ci sono modifiche non salvate. Chiudere comunque il pannello?",
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
