using CLab.ViewModels;

namespace CLab.Models
{
    public enum TipoContatto
    {
        Telefono,
        Email
    }

    public class Contatti : ViewModelBase
    {
        public int Id { get; set; }
        public TipoContatto Tipo { get; set; }
        public string Valore { get; set; } = string.Empty;
        public string? Etichetta { get; set; }

        private bool _principale;
        public bool Principale
        {
            get => _principale;
            set { _principale = value; OnPropertyChanged(); }
        }

        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}