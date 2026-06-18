using CLab.Data;
using CLab.Migrations;
using CLab.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

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

        // Contatti del cliente in form
        public ObservableCollection<Contatti> ContattiCliente { get; set; } = new ObservableCollection<Contatti>();

        private TipoContatto _nuovoContattoTipo = TipoContatto.Telefono;
        public TipoContatto NuovoContattoTipo
        {
            get => _nuovoContattoTipo;
            set { _nuovoContattoTipo = value; OnPropertyChanged(); }
        }

        private string _nuovoContattoValore = string.Empty;
        public string NuovoContattoValore
        {
            get => _nuovoContattoValore;
            set { _nuovoContattoValore = value; OnPropertyChanged(); }
        }

        private string? _nuovoContattoEtichetta;
        public string? NuovoContattoEtichetta
        {
            get => _nuovoContattoEtichetta;
            set { _nuovoContattoEtichetta = value; OnPropertyChanged(); }
        }

        private Contatti? _contattoSelezionato;
        public Contatti? ContattoSelezionato
        {
            get => _contattoSelezionato;
            set { _contattoSelezionato = value; OnPropertyChanged(); }
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

        private string? _formReferente;
        public string? FormReferente
        {
            get => _formReferente;
            set { _formReferente = value; OnPropertyChanged(); }
        }

        private StatoCliente _formStato = StatoCliente.Attivo;
        public StatoCliente FormStato
        {
            get => _formStato;
            set { _formStato = value; OnPropertyChanged(); }
        }

        // Comandi
        public RelayCommand NuovoCommand { get; }
        public RelayCommand SalvaCommand { get; }
        public RelayCommand EliminaCommand { get; }
        public RelayCommand AggiungiContattoCommand { get; }
        public RelayCommand RimuoviContattoCommand { get; }

        public ClientiViewModel()
        {
            Clienti = new ObservableCollection<Cliente>();
            CaricaClienti();

            NuovoCommand = new RelayCommand(Nuovo);
            SalvaCommand = new RelayCommand(Salva);
            EliminaCommand = new RelayCommand(Elimina, () => ClienteSelezionato != null);
            AggiungiContattoCommand = new RelayCommand(AggiungiContatto, () => !string.IsNullOrWhiteSpace(NuovoContattoValore));
            RimuoviContattoCommand = new RelayCommand(RimuoviContatto, () => ContattoSelezionato != null);
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
            ContattiCliente.Clear();

            if (cliente == null)
            {
                _formId = 0;
                FormRagioneSociale = string.Empty;
                FormPartitaIva = null;
                FormReferente = null;
                FormStato = StatoCliente.Attivo;
            }
            else
            {
                _formId = cliente.Id;
                FormRagioneSociale = cliente.RagioneSociale;
                FormPartitaIva = cliente.PartitaIva;
                FormReferente = cliente.Referente;
                FormStato = cliente.Stato;

                using (var db = new ClabDbContext())
                {
                    var contatti = db.Contatti.Where(c => c.ClienteId == cliente.Id).ToList();
                    foreach (var contatto in contatti)
                    {
                        ContattiCliente.Add(contatto);
                    }
                }
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
                Cliente clienteSalvato;

                if (_formId == 0)
                {
                    var nuovo = new Cliente
                    {
                        RagioneSociale = FormRagioneSociale,
                        PartitaIva = FormPartitaIva,
                        Referente = FormReferente,
                        Stato = FormStato
                    };
                    db.Clienti.Add(nuovo);
                    db.SaveChanges();

                    clienteSalvato = nuovo;
                }
                else
                {
                    var esistente = db.Clienti.Find(_formId);
                    if (esistente == null) return;

                    esistente.RagioneSociale = FormRagioneSociale;
                    esistente.PartitaIva = FormPartitaIva;
                    esistente.Referente = FormReferente;
                    esistente.Stato = FormStato;

                    clienteSalvato = esistente;

                    var vecchiContatti = db.Contatti.Where(c => c.ClienteId == clienteSalvato.Id).ToList();
                    db.Contatti.RemoveRange(vecchiContatti);
                }

                foreach (var contatto in ContattiCliente)
                {
                    db.Contatti.Add(new Contatti
                    {
                        Tipo = contatto.Tipo,
                        Valore = contatto.Valore,
                        Etichetta = contatto.Etichetta,
                        Principale = contatto.Principale,
                        ClienteId = clienteSalvato.Id
                    });
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

        private void AggiungiContatto()
        {
            var contatto = new Contatti
            {
                Tipo = NuovoContattoTipo,
                Valore = NuovoContattoValore,
                Etichetta = NuovoContattoEtichetta
            };

            ContattiCliente.Add(contatto);

            NuovoContattoValore = string.Empty;
            NuovoContattoEtichetta = null;
        }

        private void RimuoviContatto()
        {
            if (ContattoSelezionato != null)
            {
                ContattiCliente.Remove(ContattoSelezionato);
            }
        }
    }
}