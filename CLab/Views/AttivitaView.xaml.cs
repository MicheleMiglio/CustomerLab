using CLab.Models;
using CLab.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CLab.Views
{
    public partial class AttivitaView : UserControl
    {
        public AttivitaView()
        {
            InitializeComponent();
            DataContextChanged += AttivitaView_DataContextChanged;
        }

        private void AttivitaView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is AttivitaViewModel vecchio)
                vecchio.PropertyChanged -= ViewModel_PropertyChanged;

            if (e.NewValue is AttivitaViewModel nuovo)
                nuovo.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AttivitaViewModel.ConfigurazioneAttiva) &&
                DataContext is AttivitaViewModel vm && vm.ConfigurazioneAttiva)
            {
                GeneraColonneMatrice(vm);
            }
        }

        private void GeneraColonneMatrice(AttivitaViewModel vm)
        {
            // La prima colonna (CLIENTE) è statica, definita nello XAML:
            // qui rigeneriamo solo le colonne delle attività, una per volta.
            while (gridMatrice.Columns.Count > 1)
                gridMatrice.Columns.RemoveAt(1);

            for (int i = 0; i < vm.Elenco.Count; i++)
            {
                var attivita = vm.Elenco[i];

                string periodo = attivita.Periodicita switch
                {
                    Periodicita.Mensile => "mensile",
                    Periodicita.Trimestrale => "trimestrale",
                    _ => "annuale"
                };

                gridMatrice.Columns.Add(new DataGridTemplateColumn
                {
                    Header = new TextBlock
                    {
                        Text = $"{attivita.Nome}\n({periodo})",
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 10
                    },
                    CellTemplate = CreaTemplateCheckbox(i),
                    SortMemberPath = $"Celle[{i}].Selezionata",
                    Width = new DataGridLength(100)
                });
            }
        }

        private DataTemplate CreaTemplateCheckbox(int indiceColonna)
        {
            string xaml = $@"
                <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                              xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
                    <CheckBox Style=""{{DynamicResource ModuleCheckBoxStyle}}""
                              HorizontalAlignment=""Center""
                              IsChecked=""{{Binding Celle[{indiceColonna}].Selezionata, Mode=TwoWay}}""/>
                </DataTemplate>";

            return (DataTemplate)XamlReader.Parse(xaml);
        }
    }
}