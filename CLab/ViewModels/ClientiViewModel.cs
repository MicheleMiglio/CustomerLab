using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using CLab.ViewModels.Dettaglio;

namespace CLab.ViewModels
{
    public class ClientiViewModel : ViewModelBase
    {
        public ClienteDettaglioViewModel Dettaglio { get; } = new();

        private ObservableCollection<Cliente> _tuttiClienti = new();

        private ObservableCollection<Cliente> _clientiFiltrati = new();
        public ObservableCollection<Cliente> ClientiFiltrati
        {
            get => _clientiFiltrati;
            set { _clientiFiltrati = value; OnPropertyChanged(); }
        }

        private string _filtroTesto = string.Empty;
        public string FiltroTesto
        {
            get => _filtroTesto;
            set
            {
                _filtroTesto = value;
                OnPropertyChanged();
                AggiornaCerca();
            }
        }

        private string _contatoreTesto = string.Empty;
        public string ContatoreTesto
        {
            get => _contatoreTesto;
            set { _contatoreTesto = value; OnPropertyChanged(); }
        }

        private void AggiornaCerca()
        {
            var q = _filtroTesto.Trim().ToLower();
            var lista = string.IsNullOrEmpty(q)
                ? _tuttiClienti
                : new ObservableCollection<Cliente>(
                    _tuttiClienti.Where(c =>
                        c.RagioneSociale.ToLower().Contains(q) ||
                        (c.PartitaIva ?? "").ToLower().Contains(q) ||
                        (c.Referente ?? "").ToLower().Contains(q)));

            ClientiFiltrati = lista;
            int n = ClientiFiltrati.Count;
            ContatoreTesto = $"{n} CLIENT{(n == 1 ? "E" : "I")}";
        }

        private bool _overlayAperto;
        public bool OverlayAperto
        {
            get => _overlayAperto;
            set { _overlayAperto = value; OnPropertyChanged(); }
        }

        private bool _modalitaModifica;
        public bool ModalitaModifica
        {
            get => _modalitaModifica;
            set { _modalitaModifica = value; OnPropertyChanged(); }
        }

        private string _pannelloSottoTitolo = string.Empty;
        public string PannelloSottoTitolo
        {
            get => _pannelloSottoTitolo;
            set { _pannelloSottoTitolo = value; OnPropertyChanged(); }
        }

        // Stato del form telefoni/email/anagrafica: unica fonte di verità in Dettaglio
        // (letta e scritta dalla UI). Qui restano solo i due contatori tecnici,
        // che non sono bindati da nessuna view e servono solo a questi comandi.
        private int _tempIdCounter = -1;
        private int _telefonoInModificaId = 0;
        private int _emailInModificaId = 0;
        private int _formId;

        public RelayCommand NuovoCommand { get; }
        public RelayCommand SalvaCommand { get; }
        public RelayCommand SalvaTelefonoCommand { get; }
        public RelayCommand<Contatti> SelezionaTelefonoCommand { get; }
        public RelayCommand<Contatti> RimuoviTelefonoCommand { get; }
        public RelayCommand<Contatti> ImpostaTelefonoPrincipaleCommand { get; }
        public RelayCommand SalvaEmailCommand { get; }
        public RelayCommand<Contatti> SelezionaEmailCommand { get; }
        public RelayCommand<Contatti> RimuoviEmailCommand { get; }
        public RelayCommand<Contatti> ImpostaEmailPrincipaleCommand { get; }
        public RelayCommand<Cliente> ApriDettaglioCommand { get; }
        public RelayCommand<Cliente> EliminaClienteCommand { get; }
        public RelayCommand ChiudiOverlayCommand { get; }
        public RelayCommand AttivaModificaCommand { get; }

        public ClientiViewModel()
        {
            CaricaClienti();

            NuovoCommand = new RelayCommand(Nuovo);
            SalvaCommand = new RelayCommand(Salva);
            SalvaTelefonoCommand = new RelayCommand(SalvaTelefono,
                                                   () => !string.IsNullOrWhiteSpace(Dettaglio.NuovoTelefonoValore));
            SelezionaTelefonoCommand = new RelayCommand<Contatti>(SelezionaTelefono);
            RimuoviTelefonoCommand = new RelayCommand<Contatti>(RimuoviTelefono);
            ImpostaTelefonoPrincipaleCommand = new RelayCommand<Contatti>(ImpostaTelefonoPrincipale);
            SalvaEmailCommand = new RelayCommand(SalvaEmail,
                                                   () => !string.IsNullOrWhiteSpace(Dettaglio.NuovaEmailValore));
            SelezionaEmailCommand = new RelayCommand<Contatti>(SelezionaEmail);
            RimuoviEmailCommand = new RelayCommand<Contatti>(RimuoviEmail);
            ImpostaEmailPrincipaleCommand = new RelayCommand<Contatti>(ImpostaEmailPrincipale);
            ApriDettaglioCommand = new RelayCommand<Cliente>(ApriDettaglio);
            EliminaClienteCommand = new RelayCommand<Cliente>(EliminaCliente);
            ChiudiOverlayCommand = new RelayCommand(ChiudiOverlay);
            AttivaModificaCommand = new RelayCommand(() =>
            {
                ModalitaModifica = true;
                PannelloSottoTitolo = "MODIFICA CLIENTE";
            });
        }

        private void CaricaClienti()
        {
            using var db = new ClabDbContext();
            var lista = db.Clienti.Include(c => c.Contatti).ToList();
            _tuttiClienti = new ObservableCollection<Cliente>(lista);
            AggiornaCerca();
        }

        private void CaricaNelForm(Cliente? cliente)
        {
            Dettaglio.TelefoniCliente.Clear();
            Dettaglio.EmailCliente.Clear();
            AnnullaModificaTelefono();
            AnnullaModificaEmail();

            if (cliente == null)
            {
                _formId = 0;
                Dettaglio.FormRagioneSociale = string.Empty;
                Dettaglio.FormPartitaIva = null;
                Dettaglio.FormReferente = null;
                Dettaglio.FormIntermediario = null;
                Dettaglio.FormTipoContabilita = null;
                Dettaglio.FormStato = StatoCliente.Attivo;
            }
            else
            {
                _formId = cliente.Id;
                Dettaglio.FormRagioneSociale = cliente.RagioneSociale;
                Dettaglio.FormPartitaIva = cliente.PartitaIva;
                Dettaglio.FormReferente = cliente.Referente;
                Dettaglio.FormIntermediario = cliente.Intermediario;
                Dettaglio.FormTipoContabilita = cliente.TipoContabilita;
                Dettaglio.FormStato = cliente.Stato;

                using var db = new ClabDbContext();
                var contatti = db.Contatti.Where(c => c.ClienteId == cliente.Id).ToList();
                foreach (var contatto in contatti)
                {
                    if (contatto.Tipo == TipoContatto.Telefono)
                        Dettaglio.TelefoniCliente.Add(contatto);
                    else
                        Dettaglio.EmailCliente.Add(contatto);
                }
            }
        }

        private void Nuovo()
        {
            CaricaNelForm(null);
            ModalitaModifica = true;
            PannelloSottoTitolo = "NUOVO CLIENTE";
            OverlayAperto = true;
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(Dettaglio.FormRagioneSociale))
            {
                MessageBox.Show("La Ragione Sociale è obbligatoria.",
                    "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidaPartitaIvaCodiceFiscale(Dettaglio.FormPartitaIva, out string erroreIva))
            {
                MessageBox.Show(erroreIva, "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new ClabDbContext();
            Cliente clienteSalvato;

            if (_formId == 0)
            {
                var nuovo = new Cliente
                {
                    RagioneSociale = Dettaglio.FormRagioneSociale,
                    PartitaIva = Dettaglio.FormPartitaIva,
                    Referente = Dettaglio.FormReferente,
                    Intermediario = Dettaglio.FormIntermediario,
                    TipoContabilita = Dettaglio.FormTipoContabilita,
                    Stato = Dettaglio.FormStato
                };
                db.Clienti.Add(nuovo);
                db.SaveChanges();
                clienteSalvato = nuovo;
            }
            else
            {
                var esistente = db.Clienti.Find(_formId);
                if (esistente == null) return;

                esistente.RagioneSociale = Dettaglio.FormRagioneSociale;
                esistente.PartitaIva = Dettaglio.FormPartitaIva;
                esistente.Referente = Dettaglio.FormReferente;
                esistente.Intermediario = Dettaglio.FormIntermediario;
                esistente.TipoContabilita = Dettaglio.FormTipoContabilita;
                esistente.Stato = Dettaglio.FormStato;
                clienteSalvato = esistente;

                var vecchi = db.Contatti.Where(c => c.ClienteId == clienteSalvato.Id).ToList();
                db.Contatti.RemoveRange(vecchi);
            }

            foreach (var c in Dettaglio.TelefoniCliente.Concat(Dettaglio.EmailCliente))
            {
                db.Contatti.Add(new Contatti
                {
                    Tipo = c.Tipo,
                    Valore = c.Valore,
                    Etichetta = c.Etichetta,
                    Principale = c.Principale,
                    ClienteId = clienteSalvato.Id
                });
            }

            db.SaveChanges();
            CaricaClienti();
            CaricaNelForm(null);
            ChiudiOverlay();
        }

        private void ApriDettaglio(Cliente? cliente)
        {
            if (cliente == null) return;
            CaricaNelForm(cliente);
            ModalitaModifica = false;
            PannelloSottoTitolo = $"{cliente.TipoContabilita ?? "Senza contabilità"} · {cliente.Stato}";
            OverlayAperto = true;
        }

        private void EliminaCliente(Cliente? cliente)
        {
            if (cliente == null) return;

            var ris = MessageBox.Show(
                $"Eliminare il cliente '{cliente.RagioneSociale}'?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ris != MessageBoxResult.Yes) return;

            using var db = new ClabDbContext();
            var da = db.Clienti.Find(cliente.Id);
            if (da != null) { db.Clienti.Remove(da); db.SaveChanges(); }

            CaricaClienti();
        }

        private void ChiudiOverlay()
        {
            OverlayAperto = false;
            ModalitaModifica = false;
        }

        private void SalvaTelefono()
        {
            if (!ValidaTelefono(Dettaglio.NuovoTelefonoValore, out string errore))
            {
                MessageBox.Show(errore, "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_telefonoInModificaId == 0)
            {
                Dettaglio.TelefoniCliente.Add(new Contatti
                {
                    Id = _tempIdCounter--,
                    Tipo = TipoContatto.Telefono,
                    Valore = Dettaglio.NuovoTelefonoValore,
                    Etichetta = Dettaglio.NuovoTelefonoEtichetta,
                    Principale = !Dettaglio.TelefoniCliente.Any()
                });
            }
            else
            {
                var es = Dettaglio.TelefoniCliente.FirstOrDefault(t => t.Id == _telefonoInModificaId);
                if (es != null) { es.Valore = Dettaglio.NuovoTelefonoValore; es.Etichetta = Dettaglio.NuovoTelefonoEtichetta; }
            }
            AnnullaModificaTelefono();
        }

        private void SelezionaTelefono(Contatti? t)
        {
            if (t == null) return;
            _telefonoInModificaId = t.Id;
            Dettaglio.NuovoTelefonoValore = t.Valore;
            Dettaglio.NuovoTelefonoEtichetta = t.Etichetta;
        }

        private void AnnullaModificaTelefono()
        {
            _telefonoInModificaId = 0;
            Dettaglio.NuovoTelefonoValore = string.Empty;
            Dettaglio.NuovoTelefonoEtichetta = null;
        }

        private void RimuoviTelefono(Contatti? t)
        {
            if (t == null) return;
            bool era = t.Principale;
            Dettaglio.TelefoniCliente.Remove(t);
            if (era && Dettaglio.TelefoniCliente.Any()) Dettaglio.TelefoniCliente.First().Principale = true;
            if (_telefonoInModificaId == t.Id) AnnullaModificaTelefono();
        }

        private void ImpostaTelefonoPrincipale(Contatti? t)
        {
            if (t == null) return;
            foreach (var x in Dettaglio.TelefoniCliente) x.Principale = (x == t);
        }

        private void SalvaEmail()
        {
            if (!ValidaEmail(Dettaglio.NuovaEmailValore, out string errore))
            {
                MessageBox.Show(errore, "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_emailInModificaId == 0)
            {
                Dettaglio.EmailCliente.Add(new Contatti
                {
                    Id = _tempIdCounter--,
                    Tipo = TipoContatto.Email,
                    Valore = Dettaglio.NuovaEmailValore,
                    Etichetta = Dettaglio.NuovaEmailEtichetta,
                    Principale = !Dettaglio.EmailCliente.Any()
                });
            }
            else
            {
                var es = Dettaglio.EmailCliente.FirstOrDefault(e => e.Id == _emailInModificaId);
                if (es != null) { es.Valore = Dettaglio.NuovaEmailValore; es.Etichetta = Dettaglio.NuovaEmailEtichetta; }
            }
            AnnullaModificaEmail();
        }

        private void SelezionaEmail(Contatti? em)
        {
            if (em == null) return;
            _emailInModificaId = em.Id;
            Dettaglio.NuovaEmailValore = em.Valore;
            Dettaglio.NuovaEmailEtichetta = em.Etichetta;
        }

        private void AnnullaModificaEmail()
        {
            _emailInModificaId = 0;
            Dettaglio.NuovaEmailValore = string.Empty;
            Dettaglio.NuovaEmailEtichetta = null;
        }

        private void RimuoviEmail(Contatti? em)
        {
            if (em == null) return;
            bool era = em.Principale;
            Dettaglio.EmailCliente.Remove(em);
            if (era && Dettaglio.EmailCliente.Any()) Dettaglio.EmailCliente.First().Principale = true;
            if (_emailInModificaId == em.Id) AnnullaModificaEmail();
        }

        private void ImpostaEmailPrincipale(Contatti? em)
        {
            if (em == null) return;
            foreach (var x in Dettaglio.EmailCliente) x.Principale = (x == em);
        }

        private static bool ValidaPartitaIvaCodiceFiscale(string? valore, out string errore)
        {
            errore = string.Empty;
            if (string.IsNullOrWhiteSpace(valore)) return true;

            string v = valore.Trim().ToUpper();
            if (v.All(char.IsDigit))
            {
                if (v.Length != 11)
                { errore = "La Partita IVA deve essere composta da 11 cifre numeriche."; return false; }
            }
            else
            {
                if (v.Length != 16)
                { errore = "Il Codice Fiscale deve essere composto da 16 caratteri."; return false; }

                const string pat = @"^[A-Z]{6}[0-9]{2}[A-EHLMPR-T][0-9]{2}[A-Z][0-9]{3}[A-Z]$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(v, pat))
                { errore = "Il Codice Fiscale inserito non rispetta il formato previsto (es. RSSMRA80A01F205X)."; return false; }
            }
            return true;
        }

        private static bool ValidaEmail(string? valore, out string errore)
        {
            errore = string.Empty;
            if (string.IsNullOrWhiteSpace(valore)) return true;
            const string pat = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(valore, pat))
            { errore = $"L'indirizzo email '{valore}' non sembra valido."; return false; }
            return true;
        }

        private static bool ValidaTelefono(string? valore, out string errore)
        {
            errore = string.Empty;
            if (string.IsNullOrWhiteSpace(valore)) return true;
            string pulito = valore.Replace(" ", "").Replace("+", "");
            if (!pulito.All(char.IsDigit))
            { errore = $"Il numero '{valore}' contiene caratteri non validi (ammessi: numeri, spazi, '+')."; return false; }
            return true;
        }
    }
}