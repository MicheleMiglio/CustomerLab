using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;

namespace CLab.ViewModels
{
    public class ClientiViewModel : ViewModelBase
    {
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

        public ObservableCollection<Contatti> TelefoniCliente { get; set; } = new();
        public ObservableCollection<Contatti> EmailCliente { get; set; } = new();

        private int _tempIdCounter = -1;

        private int _telefonoInModificaId = 0;
        private string _nuovoTelefonoValore = string.Empty;
        public string NuovoTelefonoValore
        {
            get => _nuovoTelefonoValore;
            set { _nuovoTelefonoValore = value; OnPropertyChanged(); }
        }
        private string? _nuovoTelefonoEtichetta;
        public string? NuovoTelefonoEtichetta
        {
            get => _nuovoTelefonoEtichetta;
            set { _nuovoTelefonoEtichetta = value; OnPropertyChanged(); }
        }

        private int _emailInModificaId = 0;
        private string _nuovaEmailValore = string.Empty;
        public string NuovaEmailValore
        {
            get => _nuovaEmailValore;
            set { _nuovaEmailValore = value; OnPropertyChanged(); }
        }
        private string? _nuovaEmailEtichetta;
        public string? NuovaEmailEtichetta
        {
            get => _nuovaEmailEtichetta;
            set { _nuovaEmailEtichetta = value; OnPropertyChanged(); }
        }

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

        private string? _formIntermediario;
        public string? FormIntermediario
        {
            get => _formIntermediario;
            set { _formIntermediario = value; OnPropertyChanged(); }
        }

        private string? _formTipoContabilita;
        public string? FormTipoContabilita
        {
            get => _formTipoContabilita;
            set { _formTipoContabilita = value; OnPropertyChanged(); }
        }

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
                                                   () => !string.IsNullOrWhiteSpace(NuovoTelefonoValore));
            SelezionaTelefonoCommand = new RelayCommand<Contatti>(SelezionaTelefono);
            RimuoviTelefonoCommand = new RelayCommand<Contatti>(RimuoviTelefono);
            ImpostaTelefonoPrincipaleCommand = new RelayCommand<Contatti>(ImpostaTelefonoPrincipale);
            SalvaEmailCommand = new RelayCommand(SalvaEmail,
                                                   () => !string.IsNullOrWhiteSpace(NuovaEmailValore));
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
            TelefoniCliente.Clear();
            EmailCliente.Clear();
            AnnullaModificaTelefono();
            AnnullaModificaEmail();

            if (cliente == null)
            {
                _formId = 0;
                FormRagioneSociale = string.Empty;
                FormPartitaIva = null;
                FormReferente = null;
                FormIntermediario = null;
                FormTipoContabilita = null;
                FormStato = StatoCliente.Attivo;
            }
            else
            {
                _formId = cliente.Id;
                FormRagioneSociale = cliente.RagioneSociale;
                FormPartitaIva = cliente.PartitaIva;
                FormReferente = cliente.Referente;
                FormIntermediario = cliente.Intermediario;
                FormTipoContabilita = cliente.TipoContabilita;
                FormStato = cliente.Stato;

                using var db = new ClabDbContext();
                var contatti = db.Contatti.Where(c => c.ClienteId == cliente.Id).ToList();
                foreach (var contatto in contatti)
                {
                    if (contatto.Tipo == TipoContatto.Telefono)
                        TelefoniCliente.Add(contatto);
                    else
                        EmailCliente.Add(contatto);
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
            if (string.IsNullOrWhiteSpace(FormRagioneSociale))
            {
                MessageBox.Show("La Ragione Sociale è obbligatoria.",
                    "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidaPartitaIvaCodiceFiscale(FormPartitaIva, out string erroreIva))
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
                    RagioneSociale = FormRagioneSociale,
                    PartitaIva = FormPartitaIva,
                    Referente = FormReferente,
                    Intermediario = FormIntermediario,
                    TipoContabilita = FormTipoContabilita,
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
                esistente.Intermediario = FormIntermediario;
                esistente.TipoContabilita = FormTipoContabilita;
                esistente.Stato = FormStato;
                clienteSalvato = esistente;

                var vecchi = db.Contatti.Where(c => c.ClienteId == clienteSalvato.Id).ToList();
                db.Contatti.RemoveRange(vecchi);
            }

            foreach (var c in TelefoniCliente.Concat(EmailCliente))
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
            if (!ValidaTelefono(NuovoTelefonoValore, out string errore))
            {
                MessageBox.Show(errore, "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_telefonoInModificaId == 0)
            {
                TelefoniCliente.Add(new Contatti
                {
                    Id = _tempIdCounter--,
                    Tipo = TipoContatto.Telefono,
                    Valore = NuovoTelefonoValore,
                    Etichetta = NuovoTelefonoEtichetta,
                    Principale = !TelefoniCliente.Any()
                });
            }
            else
            {
                var es = TelefoniCliente.FirstOrDefault(t => t.Id == _telefonoInModificaId);
                if (es != null) { es.Valore = NuovoTelefonoValore; es.Etichetta = NuovoTelefonoEtichetta; }
            }
            AnnullaModificaTelefono();
        }

        private void SelezionaTelefono(Contatti? t)
        {
            if (t == null) return;
            _telefonoInModificaId = t.Id;
            NuovoTelefonoValore = t.Valore;
            NuovoTelefonoEtichetta = t.Etichetta;
        }

        private void AnnullaModificaTelefono()
        {
            _telefonoInModificaId = 0;
            NuovoTelefonoValore = string.Empty;
            NuovoTelefonoEtichetta = null;
        }

        private void RimuoviTelefono(Contatti? t)
        {
            if (t == null) return;
            bool era = t.Principale;
            TelefoniCliente.Remove(t);
            if (era && TelefoniCliente.Any()) TelefoniCliente.First().Principale = true;
            if (_telefonoInModificaId == t.Id) AnnullaModificaTelefono();
        }

        private void ImpostaTelefonoPrincipale(Contatti? t)
        {
            if (t == null) return;
            foreach (var x in TelefoniCliente) x.Principale = (x == t);
        }

        private void SalvaEmail()
        {
            if (!ValidaEmail(NuovaEmailValore, out string errore))
            {
                MessageBox.Show(errore, "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_emailInModificaId == 0)
            {
                EmailCliente.Add(new Contatti
                {
                    Id = _tempIdCounter--,
                    Tipo = TipoContatto.Email,
                    Valore = NuovaEmailValore,
                    Etichetta = NuovaEmailEtichetta,
                    Principale = !EmailCliente.Any()
                });
            }
            else
            {
                var es = EmailCliente.FirstOrDefault(e => e.Id == _emailInModificaId);
                if (es != null) { es.Valore = NuovaEmailValore; es.Etichetta = NuovaEmailEtichetta; }
            }
            AnnullaModificaEmail();
        }

        private void SelezionaEmail(Contatti? em)
        {
            if (em == null) return;
            _emailInModificaId = em.Id;
            NuovaEmailValore = em.Valore;
            NuovaEmailEtichetta = em.Etichetta;
        }

        private void AnnullaModificaEmail()
        {
            _emailInModificaId = 0;
            NuovaEmailValore = string.Empty;
            NuovaEmailEtichetta = null;
        }

        private void RimuoviEmail(Contatti? em)
        {
            if (em == null) return;
            bool era = em.Principale;
            EmailCliente.Remove(em);
            if (era && EmailCliente.Any()) EmailCliente.First().Principale = true;
            if (_emailInModificaId == em.Id) AnnullaModificaEmail();
        }

        private void ImpostaEmailPrincipale(Contatti? em)
        {
            if (em == null) return;
            foreach (var x in EmailCliente) x.Principale = (x == em);
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