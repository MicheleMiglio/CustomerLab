using CLab.Data;
using CLab.Models;
using CLab.ViewModels.Dettaglio;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public class ClientiViewModel : ViewModelBase
    {
        public ObservableCollection<Referente> ReferentiAttivi { get; set; } = new();
        public ObservableCollection<Referente> ReferentiTutti { get; set; } = new();
        public ObservableCollection<Programma> Programmi { get; set; } = new();

        private bool _pannelloReferentiAperto;
        public bool PannelloReferentiAperto { get => _pannelloReferentiAperto; set { _pannelloReferentiAperto = value; OnPropertyChanged(); } }

        private bool _pannelloProgrammiAperto;
        public bool PannelloProgrammiAperto { get => _pannelloProgrammiAperto; set { _pannelloProgrammiAperto = value; OnPropertyChanged(); } }

        private string _nuovoReferenteTesto = string.Empty;
        public string NuovoReferenteTesto { get => _nuovoReferenteTesto; set { _nuovoReferenteTesto = value; OnPropertyChanged(); } }

        private string _nuovoProgrammaTesto = string.Empty;
        public string NuovoProgrammaTesto { get => _nuovoProgrammaTesto; set { _nuovoProgrammaTesto = value; OnPropertyChanged(); } }

        public ICommand ApriGestioneReferentiCommand { get; }
        public ICommand ChiudiGestioneReferentiCommand { get; }
        public ICommand AggiungiReferenteCommand { get; }
        public ICommand RinominaReferenteCommand { get; }
        public ICommand EliminaReferenteCommand { get; }
        public ICommand RiattivaReferenteCommand { get; }

        public ICommand ApriGestioneProgrammiCommand { get; }
        public ICommand ChiudiGestioneProgrammiCommand { get; }
        public ICommand AggiungiProgrammaCommand { get; }
        public ICommand RinominaProgrammaCommand { get; }
        public ICommand EliminaProgrammaCommand { get; }

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
                        c.ReferenteNome.ToLower().Contains(q)));

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

            ApriGestioneReferentiCommand = new RelayCommand(() => { CaricaReferenti(); PannelloReferentiAperto = true; });
            ChiudiGestioneReferentiCommand = new RelayCommand(() => PannelloReferentiAperto = false);
            AggiungiReferenteCommand = new RelayCommand(AggiungiReferente);
            RinominaReferenteCommand = new RelayCommand<Referente>(RinominaReferente);
            EliminaReferenteCommand = new RelayCommand<Referente>(EliminaReferente);
            RiattivaReferenteCommand = new RelayCommand<Referente>(RiattivaReferente);

            ApriGestioneProgrammiCommand = new RelayCommand(() => { CaricaProgrammi(); PannelloProgrammiAperto = true; });
            ChiudiGestioneProgrammiCommand = new RelayCommand(() => PannelloProgrammiAperto = false);
            AggiungiProgrammaCommand = new RelayCommand(AggiungiProgramma);
            RinominaProgrammaCommand = new RelayCommand<Programma>(RinominaProgramma);
            EliminaProgrammaCommand = new RelayCommand<Programma>(EliminaProgramma);

            CaricaReferenti();
            CaricaProgrammi();

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

            var nomiReferenti = db.Referenti.AsNoTracking().ToDictionary(r => r.Id, r => r.Nome);
            foreach (var c in lista)
                c.ReferenteNome = c.ReferenteId.HasValue && nomiReferenti.TryGetValue(c.ReferenteId.Value, out var nome)
                    ? nome
                    : "—";

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
                Dettaglio.FormProgramma = null;
                Dettaglio.FormIntermediario = null;
                Dettaglio.FormTipoContabilita = null;
                Dettaglio.FormStato = StatoCliente.Attivo;
            }
            else
            {
                _formId = cliente.Id;
                Dettaglio.FormRagioneSociale = cliente.RagioneSociale;
                Dettaglio.FormPartitaIva = cliente.PartitaIva;
                Dettaglio.FormReferente = ReferentiAttivi.FirstOrDefault(r => r.Id == cliente.ReferenteId);
                Dettaglio.FormProgramma = Programmi.FirstOrDefault(p => p.Id == cliente.ProgrammaId);
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

            if (Dettaglio.FormReferente == null)
            {
                MessageBox.Show("Il referente è obbligatorio.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    ReferenteId = Dettaglio.FormReferente.Id,
                    ProgrammaId = Dettaglio.FormProgramma?.Id,
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
                esistente.ReferenteId = Dettaglio.FormReferente.Id;
                esistente.ProgrammaId = Dettaglio.FormProgramma?.Id;
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

            using var db = new ClabDbContext();

            var todoCollegati = db.ToDo.Where(t => t.ClienteId == cliente.Id).ToList();
            var todoAperti = todoCollegati.Where(t => !t.Completato).ToList();

            string messaggio = $"Eliminare il cliente '{cliente.RagioneSociale}'?";
            if (todoAperti.Count > 0)
            {
                messaggio += $"\n\nSono collegati {todoAperti.Count} ToDo non completat{(todoAperti.Count == 1 ? "o" : "i")}: " +
                             "verranno eliminati insieme al cliente.";
            }

            var ris = MessageBox.Show(messaggio, "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ris != MessageBoxResult.Yes) return;

            if (todoAperti.Count > 0)
                db.ToDo.RemoveRange(todoAperti);

            // I ToDo già completati restano nello storico, ma slegati dal
            // cliente: lo snapshot del nome preserva il contesto.
            foreach (var t in todoCollegati.Where(t => t.Completato))
            {
                t.ClienteId = null;
                t.ClienteNomeStorico = cliente.RagioneSociale;
            }

            var da = db.Clienti.Find(cliente.Id);
            if (da != null) db.Clienti.Remove(da);

            db.SaveChanges();

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

        private void CaricaReferenti()
        {
            using var db = new ClabDbContext();
            var tutti = db.Referenti.AsNoTracking().OrderBy(r => r.Nome).ToList();

            ReferentiTutti.Clear();
            foreach (var r in tutti) ReferentiTutti.Add(r);

            ReferentiAttivi.Clear();
            foreach (var r in tutti.Where(r => r.Attivo)) ReferentiAttivi.Add(r);
        }

        private void CaricaProgrammi()
        {
            using var db = new ClabDbContext();
            Programmi.Clear();
            foreach (var p in db.Programmi.AsNoTracking().OrderBy(p => p.Nome).ToList())
                Programmi.Add(p);
        }

        private void AggiungiReferente()
        {
            if (string.IsNullOrWhiteSpace(NuovoReferenteTesto)) return;
            using var db = new ClabDbContext();
            db.Referenti.Add(new Referente { Nome = NuovoReferenteTesto.Trim(), Attivo = true });
            db.SaveChanges();
            NuovoReferenteTesto = string.Empty;
            CaricaReferenti();
        }

        public void RinominaReferente(Referente? r)
        {
            if (r == null || string.IsNullOrWhiteSpace(r.Nome)) return;
            using var db = new ClabDbContext();
            var entita = db.Referenti.First(x => x.Id == r.Id);
            entita.Nome = r.Nome.Trim();
            db.SaveChanges();
        }

        private void EliminaReferente(Referente? r)
        {
            if (r == null) return;

            using var db = new ClabDbContext();
            int numeroClienti = db.Clienti.Count(c => c.ReferenteId == r.Id);
            int numeroToDo = db.ToDo.Count(t => t.ReferenteId == r.Id);

            if (numeroClienti == 0 && numeroToDo == 0)
            {
                var entita = db.Referenti.First(x => x.Id == r.Id);
                db.Referenti.Remove(entita);
                db.SaveChanges();
                CaricaReferenti();
                return;
            }

            var esito = MessageBox.Show(
                $"\"{r.Nome}\" è collegato a {numeroClienti} client{(numeroClienti == 1 ? "e" : "i")}.\n" +
                "Non può essere eliminato: verrà disattivato (sparirà dalla tendina) e " +
                $"{(numeroClienti == 1 ? "il cliente collegato passerà" : "i clienti collegati passeranno")} in stato Cessato.\nContinuare?",
                "Conferma disattivazione", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (esito != MessageBoxResult.Yes) return;

            var entitaRef = db.Referenti.First(x => x.Id == r.Id);
            entitaRef.Attivo = false;

            foreach (var c in db.Clienti.Where(c => c.ReferenteId == r.Id).ToList())
                c.Stato = StatoCliente.Cessato;

            db.SaveChanges();
            CaricaReferenti();
            CaricaClienti();
        }

        private void RiattivaReferente(Referente? r)
        {
            if (r == null) return;

            using var db = new ClabDbContext();
            var entita = db.Referenti.First(x => x.Id == r.Id);
            entita.Attivo = true;
            db.SaveChanges();

            var clientiCessati = db.Clienti.Where(c => c.ReferenteId == r.Id && c.Stato == StatoCliente.Cessato).ToList();

            if (clientiCessati.Count > 0)
            {
                var esito = MessageBox.Show(
                    $"\"{r.Nome}\" è di nuovo attivo. Ci sono {clientiCessati.Count} client{(clientiCessati.Count == 1 ? "e" : "i")} " +
                    "attualmente Cessati (probabilmente a causa della precedente disattivazione).\nVuoi riportarli in stato Attivo?",
                    "Riattivare anche i clienti?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (esito == MessageBoxResult.Yes)
                {
                    foreach (var c in clientiCessati) c.Stato = StatoCliente.Attivo;
                    db.SaveChanges();
                }
            }

            CaricaReferenti();
            CaricaClienti();
        }

        private void AggiungiProgramma()
        {
            if (string.IsNullOrWhiteSpace(NuovoProgrammaTesto)) return;
            using var db = new ClabDbContext();
            db.Programmi.Add(new Programma { Nome = NuovoProgrammaTesto.Trim() });
            db.SaveChanges();
            NuovoProgrammaTesto = string.Empty;
            CaricaProgrammi();
        }

        public void RinominaProgramma(Programma? p)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.Nome)) return;
            using var db = new ClabDbContext();
            var entita = db.Programmi.First(x => x.Id == p.Id);
            entita.Nome = p.Nome.Trim();
            db.SaveChanges();
        }

        private void EliminaProgramma(Programma? p)
        {
            if (p == null) return;

            using var db = new ClabDbContext();
            int numeroClienti = db.Clienti.Count(c => c.ProgrammaId == p.Id);

            if (numeroClienti > 0)
            {
                var esito = MessageBox.Show(
                    $"\"{p.Nome}\" è assegnato a {numeroClienti} client{(numeroClienti == 1 ? "e" : "i")}. " +
                    "Eliminandolo, il campo Programma di quei clienti verrà svuotato.\nContinuare?",
                    "Conferma eliminazione", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (esito != MessageBoxResult.Yes) return;

                foreach (var c in db.Clienti.Where(c => c.ProgrammaId == p.Id).ToList())
                    c.ProgrammaId = null;
            }

            var entita = db.Programmi.First(x => x.Id == p.Id);
            db.Programmi.Remove(entita);
            db.SaveChanges();
            CaricaProgrammi();
        }
    }
}