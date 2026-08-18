using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CLab.Models
{
    public class MenuItemModel : INotifyPropertyChanged
    {
        private bool _isSelezionato;
        private int _contatore;


        public string Titolo { get; set; } = string.Empty;


        public string Icona { get; set; } = string.Empty;


        public ICommand? Comando { get; set; }


        public bool Separatore { get; set; }


        public bool IsSelezionato
        {
            get => _isSelezionato;
            set
            {
                if (_isSelezionato == value)
                    return;

                _isSelezionato = value;
                OnPropertyChanged();
            }
        }

        public int Contatore
        {
            get => _contatore;
            set
            {
                if (_contatore == value)
                    return;

                _contatore = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;


        protected void OnPropertyChanged(
            [CallerMemberName] string? nome = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nome));
        }
    }
}