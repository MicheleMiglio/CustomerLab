using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CLab.Views
{
    /// <summary>
    /// Torta "donut" di stato riutilizzabile (FASE 2: solo creata, l'integrazione
    /// in ScadenzarioView è rimandata alla FASE 3).
    /// VM-agnostic: geometrie delle fette, brush e testo centrale sono
    /// DependencyProperty, così le View possono bindare direttamente ai dati
    /// esistenti senza modifiche ai ViewModel.
    /// </summary>
    public partial class TortaStatoControl : UserControl
    {
        public static readonly DependencyProperty FettaCompletateProperty =
            DependencyProperty.Register(nameof(FettaCompletate), typeof(Geometry),
                typeof(TortaStatoControl), new PropertyMetadata(null));

        public static readonly DependencyProperty FettaInCorsoProperty =
            DependencyProperty.Register(nameof(FettaInCorso), typeof(Geometry),
                typeof(TortaStatoControl), new PropertyMetadata(null));

        public static readonly DependencyProperty FettaInRitardoProperty =
            DependencyProperty.Register(nameof(FettaInRitardo), typeof(Geometry),
                typeof(TortaStatoControl), new PropertyMetadata(null));

        public static readonly DependencyProperty BrushCompletateProperty =
            DependencyProperty.Register(nameof(BrushCompletate), typeof(Brush),
                typeof(TortaStatoControl), new PropertyMetadata(null));

        public static readonly DependencyProperty BrushInCorsoProperty =
            DependencyProperty.Register(nameof(BrushInCorso), typeof(Brush),
                typeof(TortaStatoControl), new PropertyMetadata(null));

        public static readonly DependencyProperty BrushInRitardoProperty =
            DependencyProperty.Register(nameof(BrushInRitardo), typeof(Brush),
                typeof(TortaStatoControl), new PropertyMetadata(null));

        public static readonly DependencyProperty TestoCentroProperty =
            DependencyProperty.Register(nameof(TestoCentro), typeof(string),
                typeof(TortaStatoControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty VuotoProperty =
            DependencyProperty.Register(nameof(Vuoto), typeof(bool),
                typeof(TortaStatoControl), new PropertyMetadata(false));

        public Geometry FettaCompletate
        {
            get => (Geometry)GetValue(FettaCompletateProperty);
            set => SetValue(FettaCompletateProperty, value);
        }

        public Geometry FettaInCorso
        {
            get => (Geometry)GetValue(FettaInCorsoProperty);
            set => SetValue(FettaInCorsoProperty, value);
        }

        public Geometry FettaInRitardo
        {
            get => (Geometry)GetValue(FettaInRitardoProperty);
            set => SetValue(FettaInRitardoProperty, value);
        }

        public Brush BrushCompletate
        {
            get => (Brush)GetValue(BrushCompletateProperty);
            set => SetValue(BrushCompletateProperty, value);
        }

        public Brush BrushInCorso
        {
            get => (Brush)GetValue(BrushInCorsoProperty);
            set => SetValue(BrushInCorsoProperty, value);
        }

        public Brush BrushInRitardo
        {
            get => (Brush)GetValue(BrushInRitardoProperty);
            set => SetValue(BrushInRitardoProperty, value);
        }

        public string TestoCentro
        {
            get => (string)GetValue(TestoCentroProperty);
            set => SetValue(TestoCentroProperty, value);
        }

        public bool Vuoto
        {
            get => (bool)GetValue(VuotoProperty);
            set => SetValue(VuotoProperty, value);
        }

        public TortaStatoControl()
        {
            InitializeComponent();
        }
    }
}