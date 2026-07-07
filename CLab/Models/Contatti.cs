using CLab.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CLab.Models
{
    public enum TipoContatto
    {
        Telefono,
        Email
    }

    public class Contatti : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public TipoContatto Tipo { get; set; }
        public int ClienteId { get; set; }

        private string _valore = string.Empty;
        public string Valore
        {
            get => _valore;
            set { _valore = value; OnPropertyChanged(); }
        }

        private string? _etichetta;
        public string? Etichetta
        {
            get => _etichetta;
            set { _etichetta = value; OnPropertyChanged(); }
        }

        private bool _principale;
        public bool Principale
        {
            get => _principale;
            set { _principale = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}