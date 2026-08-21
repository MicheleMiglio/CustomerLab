using CLab.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CLab.Views
{
    public partial class ToDoView : UserControl
    {
        private bool _pannelloAperto;
        private bool _filtriAperti;

        public ToDoView()
        {
            InitializeComponent();

            Loaded += ToDoView_Loaded;
        }

        private void ToDoView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ToDoViewModel vm)
                vm.PropertyChanged += Vm_PropertyChanged;
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (DataContext is not ToDoViewModel vm)
                return;

            if (e.PropertyName == nameof(ToDoViewModel.OverlayAperto))
            {
                if (vm.OverlayAperto && !_pannelloAperto)
                    AprirePannello(PannelloLaterale, PannelloTranslate, 400, () => _pannelloAperto = true);
                else if (!vm.OverlayAperto && _pannelloAperto)
                    ChiuderePannello(PannelloLaterale, PannelloTranslate, 400, () => _pannelloAperto = false);
            }

            if (e.PropertyName == nameof(ToDoViewModel.FiltriAperti))
            {
                if (vm.FiltriAperti && !_filtriAperti)
                    AprirePannello(PannelloFiltri, FiltriTranslate, 380, () => _filtriAperti = true);
                else if (!vm.FiltriAperti && _filtriAperti)
                    ChiuderePannello(PannelloFiltri, FiltriTranslate, 380, () => _filtriAperti = false);
            }
        }

        private void AprirePannello(FrameworkElement pannello, TranslateTransform transform, double larghezza, Action segnoAperto)
        {
            segnoAperto();

            pannello.IsHitTestVisible = true;
            pannello.Visibility = Visibility.Visible;
            OverlayScuro.Visibility = Visibility.Visible;

            AnimaTranslate(transform, larghezza, 0, 280, true);
        }

        private async void ChiuderePannello(FrameworkElement pannello, TranslateTransform transform, double larghezza, Action segnoChiuso)
        {
            segnoChiuso();

            pannello.IsHitTestVisible = false;
            AnimaTranslate(transform, 0, larghezza, 220, false);

            await Task.Delay(230);

            pannello.Visibility = Visibility.Collapsed;

            if (DataContext is ToDoViewModel vm && !vm.OverlayAperto && !vm.FiltriAperti)
                OverlayScuro.Visibility = Visibility.Collapsed;
        }

        private static void AnimaTranslate(TranslateTransform transform, double from, double to, int durationMs, bool easeOut)
        {
            var anim = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase
                {
                    EasingMode = easeOut ? EasingMode.EaseOut : EasingMode.EaseIn
                }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        private void Ordina_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.ContextMenu == null)
                return;

            btn.ContextMenu.DataContext = DataContext;
            btn.ContextMenu.PlacementTarget = btn;
            btn.ContextMenu.IsOpen = true;
        }

        private void NuovaSottoAttivita_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            if (DataContext is ToDoViewModel vm && vm.AggiungiSottoAttivitaCommand.CanExecute(null))
                vm.AggiungiSottoAttivitaCommand.Execute(null);
        }
    }
}
