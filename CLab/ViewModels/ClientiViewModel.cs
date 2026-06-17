using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CLab.Data;
using CLab.Models;

namespace CLab.ViewModels
{
    public class ClientiViewModel : ViewModelBase
    {
        public ObservableCollection<Cliente> Clienti { get; set; }

        private Cliente? _clienteSelezionato;
        public Cliente? ClienteSelezionato
        {
            get => _clienteSelezionato;
            set
            {
                _clienteSelezionato = value;
                OnPropertyChanged();
                CaricaNelForm(value);
            }
        }

        // Campi del form
        private int _formId;
        private string _formRagioneSociale = string.Empty;
        public string FormRagioneSociale
        {
            get => _formRagioneSociale;
            set { _formRagioneSociale = value; OnPropertyChanged(); }
        }

        private string? _formPartitaIva;
        public string? FormPartitaIva
        {
            get => _formPartitaIva;
            set { _formPartitaIva = value; OnPropertyChanged(); }
        }

        private bool _formAttivo = true;
        public bool FormAttivo
        {
            get => _formAttivo;
            set { _formAttivo = value; OnPropertyChanged(); }
        }

        // Comandi
        public RelayCommand NuovoCommand { get; }
        public RelayCommand SalvaCommand { get; }
        public RelayCommand EliminaCommand { get; }

        public ClientiViewModel()
        {
            Clienti = new ObservableCollection<Cliente>();
            CaricaClienti();

            NuovoCommand = new RelayCommand(Nuovo);
            SalvaCommand = new RelayCommand(Salva);
            EliminaCommand = new RelayCommand(Elimina, () => ClienteSelezionato != null);
        }

        private void CaricaClienti()
        {
            using (var db = new ClabDbContext())
            {
                var lista = db.Clienti.ToList();
                Clienti.Clear();
                foreach (var c in lista)
                {
                    Clienti.Add(c);
                }
            }
        }

        private void CaricaNelForm(Cliente? cliente)
        {
            if (cliente == null)
            {
                _formId = 0;
                FormRagioneSociale = string.Empty;
                FormPartitaIva = null;
                FormAttivo = true;
            }
            else
            {
                _formId = cliente.Id;
                FormRagioneSociale = cliente.RagioneSociale;
                FormPartitaIva = cliente.PartitaIva;
                FormAttivo = cliente.Attivo;
            }
        }

        private void Nuovo()
        {
            ClienteSelezionato = null;
            CaricaNelForm(null);
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(FormRagioneSociale))
            {
                MessageBox.Show("La Ragione Sociale è obbligatoria.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new ClabDbContext())
            {
                if (_formId == 0)
                {
                    var nuovo = new Cliente
                    {
                        RagioneSociale = FormRagioneSociale,
                        PartitaIva = FormPartitaIva,
                        Attivo = FormAttivo
                    };
                    db.Clienti.Add(nuovo);
                }
                else
                {
                    var esistente = db.Clienti.Find(_formId);
                    if (esistente != null)
                    {
                        esistente.RagioneSociale = FormRagioneSociale;
                        esistente.PartitaIva = FormPartitaIva;
                        esistente.Attivo = FormAttivo;
                    }
                }

                db.SaveChanges();
            }

            CaricaClienti();
            Nuovo();
        }

        private void Elimina()
        {
            if (ClienteSelezionato == null) return;

            var risultato = MessageBox.Show(
                $"Eliminare il cliente '{ClienteSelezionato.RagioneSociale}'?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (risultato != MessageBoxResult.Yes) return;

            using (var db = new ClabDbContext())
            {
                var daEliminare = db.Clienti.Find(ClienteSelezionato.Id);
                if (daEliminare != null)
                {
                    db.Clienti.Remove(daEliminare);
                    db.SaveChanges();
                }
            }

            CaricaClienti();
            Nuovo();
        }
    }
}