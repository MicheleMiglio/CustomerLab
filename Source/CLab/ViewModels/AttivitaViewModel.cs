using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public class AttivitaViewModel : ViewModelBase
    {
        public ObservableCollection<Attivita> Elenco { get; set; } = new();
        public ObservableCollection<Attivita> ElencoFiltrato { get; set; } = new();

        private string _filtroCatalogoTesto = string.Empty;
        public string FiltroCatalogoTesto
        {
            get => _filtroCatalogoTesto;
            set
            {
                _filtroCatalogoTesto = value;
                OnPropertyChanged();
                ApplicaFiltroCatalogo();
            }
        }

        private bool _pannelloAperto;
        public bool PannelloAperto
        {
            get => _pannelloAperto;
            set { _pannelloAperto = value; OnPropertyChanged(); }
        }

        private int _attivitaInModificaId;

        private string _formNome = string.Empty;
        public string FormNome
        {
            get => _formNome;
            set { _formNome = value; OnPropertyChanged(); }
        }

        private Periodicita _formPeriodicita = Periodicita.Mensile;
        public Periodicita FormPeriodicita
        {
            get => _formPeriodicita;
            set { _formPeriodicita = value; OnPropertyChanged(); }
        }

        private TipoCampoAttivita _formTipoCampo = TipoCampoAttivita.SiNo;
        public TipoCampoAttivita FormTipoCampo
        {
            get => _formTipoCampo;
            set { _formTipoCampo = value; OnPropertyChanged(); }
        }

        private int? _formTestoLunghezzaMassima = 200;
        public int? FormTestoLunghezzaMassima
        {
            get => _formTestoLunghezzaMassima;
            set { _formTestoLunghezzaMassima = value; OnPropertyChanged(); }
        }

        private bool _formNumeroEImporto;
        public bool FormNumeroEImporto
        {
            get => _formNumeroEImporto;
            set { _formNumeroEImporto = value; OnPropertyChanged(); }
        }

        private bool _formTendinaRichiedeImporto;
        public bool FormTendinaRichiedeImporto
        {
            get => _formTendinaRichiedeImporto;
            set { _formTendinaRichiedeImporto = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> FormOpzioni { get; set; } = new();

        private string _formNuovaOpzione = string.Empty;
        public string FormNuovaOpzione
        {
            get => _formNuovaOpzione;
            set { _formNuovaOpzione = value; OnPropertyChanged(); }
        }

        // --- Comandi catalogo ---

        public ICommand NuovoCommand { get; }
        public ICommand ModificaCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand EliminaCommand { get; }
        public ICommand AggiungiOpzioneCommand { get; }
        public ICommand RimuoviOpzioneCommand { get; }

        public AttivitaViewModel()
        {
            NuovoCommand = new RelayCommand(Nuovo);
            ModificaCommand = new RelayCommand<Attivita>(Modifica);
            SalvaCommand = new RelayCommand(Salva);
            AnnullaCommand = new RelayCommand(Annulla);
            EliminaCommand = new RelayCommand<Attivita>(Elimina);
            AggiungiOpzioneCommand = new RelayCommand(AggiungiOpzione);
            RimuoviOpzioneCommand = new RelayCommand<string>(RimuoviOpzione);

            MostraCatalogoCommand = new RelayCommand(MostraCatalogo);
            MostraConfigurazioneCommand = new RelayCommand(MostraConfigurazione);
            MostraConfigPerClienteCommand = new RelayCommand(() => ModalitaPerCliente = true);
            MostraConfigPerAttivitaCommand = new RelayCommand(() => ModalitaPerCliente = false);
            RimuoviAttivitaCommand = new RelayCommand<VoceDuplica>(RimuoviAttivita);
            AggiungiAttivitaCommand = new RelayCommand<VoceDuplica>(AggiungiAttivita);
            RimuoviClienteCommand = new RelayCommand<VoceClienteConfigurazione>(RimuoviCliente);
            AggiungiClienteCommand = new RelayCommand<VoceClienteConfigurazione>(AggiungiCliente);

            CaricaElenco();
            CaricaClientiPerConfigurazione();
        }

        private void CaricaElenco()
        {
            using var db = new ClabDbContext();

            Elenco.Clear();
            foreach (var a in db.Attivita.OrderBy(x => x.Nome).ToList())
                Elenco.Add(a);

            ApplicaFiltroCatalogo();
        }

        private void ApplicaFiltroCatalogo()
        {
            ElencoFiltrato.Clear();

            var filtrate = string.IsNullOrWhiteSpace(FiltroCatalogoTesto)
                ? Elenco
                : Elenco.Where(a => a.Nome.Contains(FiltroCatalogoTesto, StringComparison.OrdinalIgnoreCase));

            foreach (var a in filtrate)
                ElencoFiltrato.Add(a);
        }

        private void Nuovo()
        {
            _attivitaInModificaId = 0;
            FormNome = string.Empty;
            FormPeriodicita = Periodicita.Mensile;
            FormTipoCampo = TipoCampoAttivita.SiNo;
            FormTestoLunghezzaMassima = 200;
            FormNumeroEImporto = false;
            FormTendinaRichiedeImporto = false;
            FormOpzioni.Clear();
            FormNuovaOpzione = string.Empty;

            PannelloAperto = true;
        }

        private void Modifica(Attivita? a)
        {
            if (a == null) return;

            using var db = new ClabDbContext();
            var opzioni = db.OpzioniAttivita
                             .Where(o => o.AttivitaId == a.Id)
                             .OrderBy(o => o.Ordine)
                             .Select(o => o.Testo)
                             .ToList();

            _attivitaInModificaId = a.Id;
            FormNome = a.Nome;
            FormPeriodicita = a.Periodicita;
            FormTipoCampo = a.TipoCampo;
            FormTestoLunghezzaMassima = a.TestoLunghezzaMassima ?? 200;
            FormNumeroEImporto = a.NumeroEImporto;
            FormTendinaRichiedeImporto = a.TendinaRichiedeImporto;
            FormOpzioni.Clear();
            foreach (var o in opzioni) FormOpzioni.Add(o);
            FormNuovaOpzione = string.Empty;

            PannelloAperto = true;
        }

        private void AggiungiOpzione()
        {
            if (string.IsNullOrWhiteSpace(FormNuovaOpzione)) return;

            FormOpzioni.Add(FormNuovaOpzione.Trim());
            FormNuovaOpzione = string.Empty;
        }

        private void RimuoviOpzione(string? opzione)
        {
            if (opzione == null) return;
            FormOpzioni.Remove(opzione);
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(FormNome))
            {
                MessageBox.Show("Il nome dell'attività è obbligatorio.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FormTipoCampo == TipoCampoAttivita.Tendina && FormOpzioni.Count < 2)
            {
                MessageBox.Show("Una tendina richiede almeno due opzioni.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new ClabDbContext();

            Attivita entita;

            if (_attivitaInModificaId == 0)
            {
                entita = new Attivita();
                db.Attivita.Add(entita);
            }
            else
            {
                entita = db.Attivita.First(a => a.Id == _attivitaInModificaId);

                var vecchieOpzioni = db.OpzioniAttivita.Where(o => o.AttivitaId == entita.Id);
                db.OpzioniAttivita.RemoveRange(vecchieOpzioni);
            }

            entita.Nome = FormNome.Trim();
            entita.Periodicita = FormPeriodicita;
            entita.TipoCampo = FormTipoCampo;
            entita.TestoLunghezzaMassima = FormTipoCampo == TipoCampoAttivita.TestoLibero
                ? FormTestoLunghezzaMassima : null;
            entita.NumeroEImporto = FormTipoCampo == TipoCampoAttivita.Numero && FormNumeroEImporto;
            entita.TendinaRichiedeImporto = FormTipoCampo == TipoCampoAttivita.Tendina && FormTendinaRichiedeImporto;

            db.SaveChanges();

            if (FormTipoCampo == TipoCampoAttivita.Tendina)
            {
                int ordine = 0;
                foreach (var testo in FormOpzioni)
                {
                    db.OpzioniAttivita.Add(new AttivitaOpzione
                    {
                        AttivitaId = entita.Id,
                        Testo = testo,
                        Ordine = ordine++
                    });
                }
                db.SaveChanges();
            }

            PannelloAperto = false;
            CaricaElenco();

            if (ConfigurazioneAttiva)
                CaricaListeConfigurazione();
        }

        private void Annulla()
        {
            PannelloAperto = false;
        }

        private void Elimina(Attivita? a)
        {
            if (a == null) return;

            using var db = new ClabDbContext();

            int clientiCoinvolti = db.ClientiAttivita.Count(ca => ca.AttivitaId == a.Id);
            int compilazioniCoinvolte = db.Compilazioni.Count(c => c.AttivitaId == a.Id);

            string messaggio = $"Eliminando \"{a.Nome}\" dal catalogo:\n" +
                $"• Clienti interessati: {clientiCoinvolti}\n" +
                $"• Compilazioni che verranno cancellate: {compilazioniCoinvolte}\n" +
                "Continuare?";

            var esito = MessageBox.Show(messaggio, "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (esito != MessageBoxResult.Yes) return;

            var entita = db.Attivita.First(x => x.Id == a.Id);
            db.Attivita.Remove(entita);
            db.SaveChanges();

            CaricaElenco();

            if (AttivitaConfigurazione != null && !Elenco.Any(x => x.Id == AttivitaConfigurazione.Id))
                AttivitaConfigurazione = null;

            if (ConfigurazioneAttiva)
                CaricaListeConfigurazione();
        }

        // --- Configurazione: doppia lista, due modalità ---

        private bool _configurazioneAttiva;
        public bool ConfigurazioneAttiva
        {
            get => _configurazioneAttiva;
            set { _configurazioneAttiva = value; OnPropertyChanged(); }
        }

        public ICommand MostraCatalogoCommand { get; }
        public ICommand MostraConfigurazioneCommand { get; }
        public ICommand MostraConfigPerClienteCommand { get; }
        public ICommand MostraConfigPerAttivitaCommand { get; }
        public ICommand RimuoviAttivitaCommand { get; }
        public ICommand AggiungiAttivitaCommand { get; }
        public ICommand RimuoviClienteCommand { get; }
        public ICommand AggiungiClienteCommand { get; }

        private void MostraCatalogo()
        {
            ConfigurazioneAttiva = false;
        }

        private void MostraConfigurazione()
        {
            ConfigurazioneAttiva = true;
            CaricaListeConfigurazione();
        }

        private bool _modalitaPerCliente = true;
        public bool ModalitaPerCliente
        {
            get => _modalitaPerCliente;
            set
            {
                _modalitaPerCliente = value;
                OnPropertyChanged();
                CaricaListeConfigurazione();
            }
        }

        public ObservableCollection<Cliente> ClientiPerConfigurazione { get; set; } = new();

        private List<Cliente> _clientiConfigurazioneCompleti = new();
        public ObservableCollection<Referente> ReferentiFiltroConfigurazione { get; set; } = new();

        private Referente? _referenteFiltroConfigurazione;
        public Referente? ReferenteFiltroConfigurazione
        {
            get => _referenteFiltroConfigurazione;
            set { _referenteFiltroConfigurazione = value; OnPropertyChanged(); ApplicaFiltroClientiConfigurazione(); }
        }

        private Cliente? _clienteConfigurazione;
        public Cliente? ClienteConfigurazione
        {
            get => _clienteConfigurazione;
            set
            {
                _clienteConfigurazione = value;
                OnPropertyChanged();
                CaricaListeConfigurazione();
            }
        }

        private Attivita? _attivitaConfigurazione;
        public Attivita? AttivitaConfigurazione
        {
            get => _attivitaConfigurazione;
            set
            {
                _attivitaConfigurazione = value;
                OnPropertyChanged();
                CaricaListeConfigurazione();
            }
        }

        // Liste "per cliente" (contengono attività)
        public ObservableCollection<VoceDuplica> AttivitaAssegnateConfig { get; set; } = new();
        public ObservableCollection<VoceDuplica> AttivitaNonAssegnateConfig { get; set; } = new();
        private List<VoceDuplica> _attivitaAssegnateComplete = new();
        private List<VoceDuplica> _attivitaNonAssegnateComplete = new();

        // Liste "per attività" (contengono clienti)
        public ObservableCollection<VoceClienteConfigurazione> ClientiAssegnatiConfig { get; set; } = new();
        public ObservableCollection<VoceClienteConfigurazione> ClientiNonAssegnatiConfig { get; set; } = new();
        private List<VoceClienteConfigurazione> _clientiAssegnatiCompleti = new();
        private List<VoceClienteConfigurazione> _clientiNonAssegnatiCompleti = new();

        private string _filtroAssegnateTesto = string.Empty;
        public string FiltroAssegnateTesto
        {
            get => _filtroAssegnateTesto;
            set { _filtroAssegnateTesto = value; OnPropertyChanged(); ApplicaFiltriConfigurazione(); }
        }

        private string _filtroNonAssegnateTesto = string.Empty;
        public string FiltroNonAssegnateTesto
        {
            get => _filtroNonAssegnateTesto;
            set { _filtroNonAssegnateTesto = value; OnPropertyChanged(); ApplicaFiltriConfigurazione(); }
        }

        private void CaricaClientiPerConfigurazione()
        {
            using var db = new ClabDbContext();
            _clientiConfigurazioneCompleti = db.Clienti.AsNoTracking().OrderBy(x => x.RagioneSociale).ToList();

            ReferentiFiltroConfigurazione.Clear();
            foreach (var r in db.Referenti.AsNoTracking().Where(r => r.Attivo).OrderBy(r => r.Nome).ToList())
                ReferentiFiltroConfigurazione.Add(r);

            ApplicaFiltroClientiConfigurazione();
        }

        private void ApplicaFiltroClientiConfigurazione()
        {
            ClientiPerConfigurazione.Clear();

            var filtrati = ReferenteFiltroConfigurazione == null
                ? _clientiConfigurazioneCompleti
                : _clientiConfigurazioneCompleti.Where(c => c.ReferenteId == ReferenteFiltroConfigurazione.Id);

            foreach (var c in filtrati) ClientiPerConfigurazione.Add(c);

            if (ClienteConfigurazione != null && !ClientiPerConfigurazione.Contains(ClienteConfigurazione))
                ClienteConfigurazione = null;
        }

        private void CaricaListeConfigurazione()
        {
            if (ModalitaPerCliente)
                CaricaListePerCliente();
            else
                CaricaListePerAttivita();
        }

        private void CaricaListePerCliente()
        {
            _attivitaAssegnateComplete = new List<VoceDuplica>();
            _attivitaNonAssegnateComplete = new List<VoceDuplica>();

            if (ClienteConfigurazione != null)
            {
                using var db = new ClabDbContext();
                var idAssegnate = db.ClientiAttivita
                    .Where(ca => ca.ClienteId == ClienteConfigurazione.Id)
                    .Select(ca => ca.AttivitaId)
                    .ToHashSet();

                foreach (var a in Elenco)
                {
                    var voce = new VoceDuplica
                    {
                        AttivitaId = a.Id,
                        Nome = a.Nome,
                        Periodicita = a.Periodicita,
                        TipoCampo = a.TipoCampo
                    };

                    if (idAssegnate.Contains(a.Id))
                        _attivitaAssegnateComplete.Add(voce);
                    else
                        _attivitaNonAssegnateComplete.Add(voce);
                }
            }

            ApplicaFiltriConfigurazione();
        }

        private void CaricaListePerAttivita()
        {
            _clientiAssegnatiCompleti = new List<VoceClienteConfigurazione>();
            _clientiNonAssegnatiCompleti = new List<VoceClienteConfigurazione>();

            if (AttivitaConfigurazione != null)
            {
                using var db = new ClabDbContext();
                var idAssegnati = db.ClientiAttivita
                    .Where(ca => ca.AttivitaId == AttivitaConfigurazione.Id)
                    .Select(ca => ca.ClienteId)
                    .ToHashSet();

                foreach (var c in ClientiPerConfigurazione)
                {
                    var voce = new VoceClienteConfigurazione
                    {
                        ClienteId = c.Id,
                        RagioneSociale = c.RagioneSociale,
                        PartitaIva = c.PartitaIva
                    };

                    if (idAssegnati.Contains(c.Id))
                        _clientiAssegnatiCompleti.Add(voce);
                    else
                        _clientiNonAssegnatiCompleti.Add(voce);
                }
            }

            ApplicaFiltriConfigurazione();
        }

        private void ApplicaFiltriConfigurazione()
        {
            AttivitaAssegnateConfig.Clear();
            foreach (var v in Filtra(_attivitaAssegnateComplete, FiltroAssegnateTesto, x => x.Nome))
                AttivitaAssegnateConfig.Add(v);

            AttivitaNonAssegnateConfig.Clear();
            foreach (var v in Filtra(_attivitaNonAssegnateComplete, FiltroNonAssegnateTesto, x => x.Nome))
                AttivitaNonAssegnateConfig.Add(v);

            ClientiAssegnatiConfig.Clear();
            foreach (var v in Filtra(_clientiAssegnatiCompleti, FiltroAssegnateTesto, x => x.RagioneSociale))
                ClientiAssegnatiConfig.Add(v);

            ClientiNonAssegnatiConfig.Clear();
            foreach (var v in Filtra(_clientiNonAssegnatiCompleti, FiltroNonAssegnateTesto, x => x.RagioneSociale))
                ClientiNonAssegnatiConfig.Add(v);
        }

        private static IEnumerable<T> Filtra<T>(List<T> lista, string filtro, Func<T, string> testo)
        {
            return string.IsNullOrWhiteSpace(filtro)
                ? lista
                : lista.Where(x => testo(x).Contains(filtro, StringComparison.OrdinalIgnoreCase));
        }

        private void RimuoviAttivita(VoceDuplica? voce)
        {
            if (voce == null || ClienteConfigurazione == null) return;

            using var db = new ClabDbContext();
            int compilazioniCoinvolte = db.Compilazioni.Count(c =>
                c.ClienteId == ClienteConfigurazione.Id && c.AttivitaId == voce.AttivitaId);

            string messaggio = compilazioniCoinvolte > 0
                ? $"Ci sono {compilazioniCoinvolte} compilazion{(compilazioniCoinvolte == 1 ? "e" : "i")} registrat{(compilazioniCoinvolte == 1 ? "a" : "e")} " +
                  $"per \"{voce.Nome}\" su {ClienteConfigurazione.RagioneSociale}.\nRimuovendola, verranno eliminate definitivamente. Continuare?"
                : $"Rimuovere \"{voce.Nome}\" da {ClienteConfigurazione.RagioneSociale}?";

            var esito = MessageBox.Show(messaggio, "Conferma rimozione", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (esito != MessageBoxResult.Yes) return;

            if (compilazioniCoinvolte > 0)
                db.Compilazioni.RemoveRange(db.Compilazioni.Where(c =>
                    c.ClienteId == ClienteConfigurazione.Id && c.AttivitaId == voce.AttivitaId));

            var esistente = db.ClientiAttivita.FirstOrDefault(a =>
                a.ClienteId == ClienteConfigurazione.Id && a.AttivitaId == voce.AttivitaId);
            if (esistente != null) db.ClientiAttivita.Remove(esistente);
            db.SaveChanges();

            _attivitaAssegnateComplete.Remove(voce);
            _attivitaNonAssegnateComplete.Add(voce);
            ApplicaFiltriConfigurazione();
        }

        private void AggiungiAttivita(VoceDuplica? voce)
        {
            if (voce == null || ClienteConfigurazione == null) return;

            using var db = new ClabDbContext();
            bool esiste = db.ClientiAttivita.Any(a =>
                a.ClienteId == ClienteConfigurazione.Id && a.AttivitaId == voce.AttivitaId);

            if (!esiste)
            {
                db.ClientiAttivita.Add(new ClienteAttivita
                {
                    ClienteId = ClienteConfigurazione.Id,
                    AttivitaId = voce.AttivitaId
                });
                db.SaveChanges();
            }

            _attivitaNonAssegnateComplete.Remove(voce);
            _attivitaAssegnateComplete.Add(voce);
            ApplicaFiltriConfigurazione();
        }

        private void RimuoviCliente(VoceClienteConfigurazione? voce)
        {
            if (voce == null || AttivitaConfigurazione == null) return;

            using var db = new ClabDbContext();
            int compilazioniCoinvolte = db.Compilazioni.Count(c =>
                c.ClienteId == voce.ClienteId && c.AttivitaId == AttivitaConfigurazione.Id);

            string messaggio = compilazioniCoinvolte > 0
                ? $"Ci sono {compilazioniCoinvolte} compilazion{(compilazioniCoinvolte == 1 ? "e" : "i")} registrat{(compilazioniCoinvolte == 1 ? "a" : "e")} " +
                  $"per \"{AttivitaConfigurazione.Nome}\" su {voce.RagioneSociale}.\nRimuovendola, verranno eliminate definitivamente. Continuare?"
                : $"Rimuovere \"{AttivitaConfigurazione.Nome}\" da {voce.RagioneSociale}?";

            var esito = MessageBox.Show(messaggio, "Conferma rimozione", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (esito != MessageBoxResult.Yes) return;

            if (compilazioniCoinvolte > 0)
                db.Compilazioni.RemoveRange(db.Compilazioni.Where(c =>
                    c.ClienteId == voce.ClienteId && c.AttivitaId == AttivitaConfigurazione.Id));

            var esistente = db.ClientiAttivita.FirstOrDefault(a =>
                a.ClienteId == voce.ClienteId && a.AttivitaId == AttivitaConfigurazione.Id);
            if (esistente != null) db.ClientiAttivita.Remove(esistente);
            db.SaveChanges();

            _clientiAssegnatiCompleti.Remove(voce);
            _clientiNonAssegnatiCompleti.Add(voce);
            ApplicaFiltriConfigurazione();
        }

        private void AggiungiCliente(VoceClienteConfigurazione? voce)
        {
            if (voce == null || AttivitaConfigurazione == null) return;

            using var db = new ClabDbContext();
            bool esiste = db.ClientiAttivita.Any(a =>
                a.ClienteId == voce.ClienteId && a.AttivitaId == AttivitaConfigurazione.Id);

            if (!esiste)
            {
                db.ClientiAttivita.Add(new ClienteAttivita
                {
                    ClienteId = voce.ClienteId,
                    AttivitaId = AttivitaConfigurazione.Id
                });
                db.SaveChanges();
            }

            _clientiNonAssegnatiCompleti.Remove(voce);
            _clientiAssegnatiCompleti.Add(voce);
            ApplicaFiltriConfigurazione();
        }

        /// <summary>
        /// Chiamato da fuori (es. Scadenzario, tramite MainViewModel) per aprire
        /// la Configurazione già impostata in modalità "per cliente" con un
        /// cliente preselezionato. FASE 4: la selezione avviene per Id, non più
        /// per ragione sociale.
        /// </summary>
        public void ApriConfigurazionePerCliente(int clienteId)
        {
            ModalitaPerCliente = true;

            // Il filtro referente potrebbe nascondere il cliente richiesto:
            // lo azzero così la selezione contestuale è sempre possibile.
            if (ReferenteFiltroConfigurazione != null)
                ReferenteFiltroConfigurazione = null;

            var cliente = ClientiPerConfigurazione.FirstOrDefault(c => c.Id == clienteId)
                          ?? _clientiConfigurazioneCompleti.FirstOrDefault(c => c.Id == clienteId);
            if (cliente != null)
                ClienteConfigurazione = cliente;
        }
    }

    public class VoceScaduta
    {
        public string Nome { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
    }

    public class VoceDuplica
    {
        public int AttivitaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public Periodicita Periodicita { get; set; }
        public TipoCampoAttivita TipoCampo { get; set; }
    }

    public class VoceClienteConfigurazione
    {
        public int ClienteId { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public string? PartitaIva { get; set; }
    }
}