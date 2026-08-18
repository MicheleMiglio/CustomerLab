using CLab.Models;
using CLab.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CLab.Views
{
    public partial class PromemoriaView : UserControl
    {
        public PromemoriaView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Fa sparire il post-it con un fade-out breve, poi lo elimina
        /// davvero (dati compresi). Nessuna conferma: è un post-it, si butta.
        /// </summary>
        private void BtnElimina_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement elemento) return;
            if (elemento.DataContext is not Promemoria promemoria) return;
            if (DataContext is not PromemoriaViewModel vm) return;

            var cartoncino = TrovaAntenato<Border>(elemento);
            if (cartoncino == null)
            {
                vm.RimuoviDefinitivamente(promemoria);
                return;
            }

            var animazione = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            animazione.Completed += (s, args) => vm.RimuoviDefinitivamente(promemoria);
            cartoncino.BeginAnimation(UIElement.OpacityProperty, animazione);
        }

        private static T? TrovaAntenato<T>(DependencyObject partenza) where T : DependencyObject
        {
            var corrente = VisualTreeHelper.GetParent(partenza);
            while (corrente != null)
            {
                if (corrente is T match) return match;
                corrente = VisualTreeHelper.GetParent(corrente);
            }
            return null;
        }
    }
}