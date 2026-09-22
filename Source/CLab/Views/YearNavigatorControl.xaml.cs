using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.Views
{
    /// <summary>
    /// YearNavigatorControl CLab 2.0: [‹] anno [›]. Presentazione pura
    /// (DependencyProperties): <see cref="Anno"/> è il valore mostrato,
    /// <see cref="ComandoPrecedente"/>/<see cref="ComandoSuccessivo"/> sono i
    /// comandi del ViewModel. Riutilizzabile in qualsiasi modulo con
    /// navigazione temporale (Fatture oggi, altri domani). Uso:
    /// xmlns:views="clr-namespace:CLab.Views" →
    /// &lt;views:YearNavigatorControl Anno="{Binding AnnoSelezionato}"
    ///     ComandoPrecedente="{Binding AnnoPrecedenteCommand}"
    ///     ComandoSuccessivo="{Binding AnnoSuccessivoCommand}"/&gt;
    /// </summary>
    public partial class YearNavigatorControl : UserControl
    {
        public static readonly DependencyProperty AnnoProperty = DependencyProperty.Register(
            nameof(Anno), typeof(int), typeof(YearNavigatorControl), new PropertyMetadata(0));

        public static readonly DependencyProperty ComandoPrecedenteProperty = DependencyProperty.Register(
            nameof(ComandoPrecedente), typeof(ICommand), typeof(YearNavigatorControl), new PropertyMetadata(null));

        public static readonly DependencyProperty ComandoSuccessivoProperty = DependencyProperty.Register(
            nameof(ComandoSuccessivo), typeof(ICommand), typeof(YearNavigatorControl), new PropertyMetadata(null));

        public int Anno
        {
            get => (int)GetValue(AnnoProperty);
            set => SetValue(AnnoProperty, value);
        }

        public ICommand? ComandoPrecedente
        {
            get => (ICommand?)GetValue(ComandoPrecedenteProperty);
            set => SetValue(ComandoPrecedenteProperty, value);
        }

        public ICommand? ComandoSuccessivo
        {
            get => (ICommand?)GetValue(ComandoSuccessivoProperty);
            set => SetValue(ComandoSuccessivoProperty, value);
        }

        public YearNavigatorControl()
        {
            InitializeComponent();
        }
    }
}
