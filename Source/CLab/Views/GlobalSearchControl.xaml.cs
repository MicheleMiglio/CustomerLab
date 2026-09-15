using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace CLab.Views
{
    /// <summary>
    /// FASE 12: presentazione della ricerca globale. Solo logica UI: focus sul
    /// campo all'apertura, Esc per chiudere, click sull'overlay per chiudere.
    /// </summary>
    public partial class GlobalSearchControl : UserControl
    {
        public GlobalSearchControl()
        {
            InitializeComponent();

            // Focus sul campo ogni volta che la ricerca si apre.
            DependencyPropertyDescriptor.FromProperty(VisibilityProperty, typeof(UserControl))
                .AddValueChanged(this, (_, _) =>
                {
                    if (Visibility == Visibility.Visible)
                        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => campoRicerca.Focus()));
                });
        }

        private void SuClickSfondo(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CLab.ViewModels.GlobalSearchViewModel vm)
                vm.Chiudi();
        }

        private void SuTasto(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is CLab.ViewModels.GlobalSearchViewModel vm)
            {
                vm.Chiudi();
                e.Handled = true;
            }
        }
    }
}
