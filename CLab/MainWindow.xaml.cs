using CLab.Models;
using CLab.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace CLab
{
    public partial class MainWindow
    {
        private bool _pannelloAperto = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ClientiViewModel();

            if (DataContext is ClientiViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
            }
        }

        private void Vm_PropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ClientiViewModel.OverlayAperto)) return;

            if (DataContext is ClientiViewModel vm)
            {
                if (vm.OverlayAperto && !_pannelloAperto)
                    AprirePannello();
                else if (!vm.OverlayAperto && _pannelloAperto)
                    ChiuderePannello();
            }
        }

        private void AprirePannello()
        {
            _pannelloAperto = true;
            PannelloLaterale.IsHitTestVisible = true;
            PannelloLaterale.Visibility = Visibility.Visible;
            OverlayScuro.Visibility = Visibility.Visible;

            AnimaTranslate(400, 0, durationMs: 280, easeOut: true);
        }

        private async void ChiuderePannello()
        {
            _pannelloAperto = false;
            PannelloLaterale.IsHitTestVisible = false;

            AnimaTranslate(0, 400, 220, false);

            await Task.Delay(230);

            OverlayScuro.Visibility = Visibility.Collapsed;
            PannelloLaterale.Visibility = Visibility.Collapsed;
        }

        private DoubleAnimation AnimaTranslate(double from, double to,
            int durationMs, bool easeOut)
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
            PannelloTranslate.BeginAnimation(
                System.Windows.Media.TranslateTransform.XProperty, anim);
            return anim;
        }

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

        private void gridClienti_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ClientiViewModel vm &&
                gridClienti.SelectedItem is Cliente cliente)
                vm.ApriDettaglioCommand.Execute(cliente);
        }
    }
}