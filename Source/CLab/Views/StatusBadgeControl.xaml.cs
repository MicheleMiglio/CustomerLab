using System.Windows;
using System.Windows.Controls;

namespace CLab.Views
{
    /// <summary>
    /// Badge di stato unificato CLab 2.0 (FASE 2): pillola con pallino e testo.
    /// Solo presentazione (DependencyProperty): la semantica testo/colori è
    /// centralizzata in Converters.StatoSemantico. Uso:
    /// xmlns:views="clr-namespace:CLab.Views" →
    /// &lt;views:StatusBadgeControl Stato="{Binding Stato}"/&gt;
    /// </summary>
    public partial class StatusBadgeControl : UserControl
    {
        public static readonly DependencyProperty StatoProperty = DependencyProperty.Register(
            nameof(Stato), typeof(object), typeof(StatusBadgeControl), new PropertyMetadata(null));

        public object? Stato
        {
            get => GetValue(StatoProperty);
            set => SetValue(StatoProperty, value);
        }

        public StatusBadgeControl()
        {
            InitializeComponent();
        }
    }
}
