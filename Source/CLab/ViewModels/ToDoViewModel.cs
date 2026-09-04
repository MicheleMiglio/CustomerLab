using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public enum OrdinamentoToDo
    {
        Scadenza,
        Creazione
    }

    public class ChipFiltroToDo
    {
        public string Chiave { get; init; } = string.Empty;
        public string Testo { get; init; } = string.Empty;
    }

    public class SezioneToDoLista : ViewModelBase
    {
        public string Chiave { get; init; } = string.Empty;
        public string Titolo { get; init; } = string.Empty;
        public bool SempreVisibile { get; init; }
        public bool MostraPlaceholderVuoto { get; init; }

        private bool _espansa = true;
        public bool Espansa
        {
            get => _espansa;
            set
            {
                if (_espansa == value) return;
                _espansa = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MostraRighe));
                OnPropertyChanged(nameof(MostraVuoto));
            }
        }

        public ObservableCollection<ToDo> Righe { get; } = new();

        public int Conteggio => Righe.Count;
        public bool MostraSezione => SempreVisibile || Righe.Count > 0;
        public string Intestazione => string.IsNullOrEmpty(Nota)
            ? $"{Titolo}  {Conteggio}"
            : $"{Titolo}  {Conteggio}  ·  {Nota}";

        private string? _nota;
        public string? Nota
        {
            get => _nota;
            set { if (_nota == value) return; _nota = value; OnPropertyChanged(); OnPropertyChanged(nameof(Intestazione)); }
        }
        public bool MostraRighe => Espansa && Righe.Count > 0;
        public bool MostraVuoto => Espansa && Righe.Count == 0 && MostraPlaceholderVuoto;

        public void NotificaConteggio()
        {
            OnPropertyChanged(nameof(Conteggio));
            OnPropertyChanged(nameof(MostraSezione));
            OnPropertyChanged(nameof(Intestazione));
            OnPropertyChanged(nameof(MostraRighe));
            OnPropertyChanged(nameof(MostraVuoto));
        }
    }

    public class ToDoViewModel : ViewModelBase
    {
        private const int GiorniCompletatiDefault = 90;

        private int _todoInModificaId;
        private List<ToDo> _tuttiToDo = new();
        private int _sottoAttivitaTempId = -1;
        private readonly HashSet<int> _espansi = new();

        public SezioneToDoLista SezioneScaduti { get; }
        public SezioneToDoLista SezioneInProgramma { get; }
        public SezioneToDoLista SezioneSenzaScadenza { get; }
        public SezioneToDoLista SezioneCompletati { get; }

        public ObservableCollection<SezioneToDoLista> Sezioni { get; }
        public ObservableCollection<ChipFiltroToDo> ChipFiltri { get; } = new();

        public ObservableCollection<Cliente> ClientiPerCombo { get; } = new();
        public ObservableCollection<Cliente> ClientiPerFiltro { get; } = new();
        private List<Cliente> _clientiTutti = new();

        public ObservableCollection<Referente> ReferentiTutti { get; } = new();

        public List<KeyValuePair<string, string>> OpzioniPrioritaFiltro { get; } = new()
        {
            new("tutte", "Tutte"),
            new("alta", "Alta"),
            new("media", "Media"),
            new("bassa", "Bassa")
        };

        public List<KeyValuePair<string, string>> OpzioniCollegamentoFiltro { get; } = new()
        {
            new("tutti", "Tutti"),
            new("cliente", "Con cliente"),
            new("interno", "Senza cliente"),
            new("referente", "Con referente")
        };

        public List<KeyValuePair<string, string>> OpzioniPassiFiltro { get; } = new()
        {
            new("tutti", "Tutti"),
            new("con", "Con sotto-attività"),
            new("senza", "Senza sotto-attività"),
            new("aperti", "Passi ancora aperti")
        };

        public List<KeyValuePair<string, string>> OpzioniCampoDataFiltro { get; } = new()
        {
            new("scadenza", "Scadenza"),
            new("completamento", "Completamento"),
            new("creazione", "Creazione")
        };

        // --- Ordinamento e ricerca (istantanei) ---

        private OrdinamentoToDo _ordinamento = OrdinamentoToDo.Scadenza;
        public OrdinamentoToDo Ordinamento
        {
            get => _ordinamento;
            set
            {
                if (_ordinamento == value) return;
                _ordinamento = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OrdinamentoEtichetta));
                AggiornaLista();
            }
        }

        public string OrdinamentoEtichetta => Ordinamento == OrdinamentoToDo.Creazione
            ? "Più recenti"
            : "Per scadenza";

        private string _filtroTesto = string.Empty;
        public string FiltroTesto
        {
            get => _filtroTesto;
            set { _filtroTesto = value; OnPropertyChanged(); AggiornaLista(); }
        }

        // --- Filtri applicati ---

        private int? _filtroClienteId;
        private string _filtroClienteNome = string.Empty;
        private int? _filtroReferenteId;
        private string _filtroReferenteNome = string.Empty;
        private string _filtroPriorita = "tutte";
        private string _filtroCollegamento = "tutti";
        private string _filtroPassi = "tutti";
        private string _filtroCampoData = "scadenza";
        private DateTime? _filtroDataDa;
        private DateTime? _filtroDataA;
        private bool _filtroMostraTuttiCompletati;
        private bool _filtroSoloScaduti;

        // --- Bozza pannello filtri ---

        private Cliente? _bozzaCliente;
        public Cliente? BozzaCliente { get => _bozzaCliente; set { _bozzaCliente = value; OnPropertyChanged(); } }

        private Referente? _bozzaReferente;
        public Referente? BozzaReferente { get => _bozzaReferente; set { _bozzaReferente = value; OnPropertyChanged(); } }

        private string _bozzaPriorita = "tutte";
        public string BozzaPriorita { get => _bozzaPriorita; set { _bozzaPriorita = value; OnPropertyChanged(); } }

        private string _bozzaCollegamento = "tutti";
        public string BozzaCollegamento { get => _bozzaCollegamento; set { _bozzaCollegamento = value; OnPropertyChanged(); } }

        private string _bozzaPassi = "tutti";
        public string BozzaPassi { get => _bozzaPassi; set { _bozzaPassi = value; OnPropertyChanged(); } }

        private string _bozzaCampoData = "scadenza";
        public string BozzaCampoData { get => _bozzaCampoData; set { _bozzaCampoData = value; OnPropertyChanged(); } }

        private DateTime? _bozzaDataDa;
        public DateTime? BozzaDataDa { get => _bozzaDataDa; set { _bozzaDataDa = value; OnPropertyChanged(); } }

        private DateTime? _bozzaDataA;
        public DateTime? BozzaDataA { get => _bozzaDataA; set { _bozzaDataA = value; OnPropertyChanged(); } }

        private bool _bozzaMostraTuttiCompletati;
        public bool BozzaMostraTuttiCompletati { get => _bozzaMostraTuttiCompletati; set { _bozzaMostraTuttiCompletati = value; OnPropertyChanged(); } }

        private bool _bozzaSoloScaduti;
        public bool BozzaSoloScaduti { get => _bozzaSoloScaduti; set { _bozzaSoloScaduti = value; OnPropertyChanged(); } }

        public int NumeroFiltriAttivi { get; private set; }
        public bool HaChipFiltri => ChipFiltri.Count > 0;
        public bool ListaVuota { get; private set; }

        /// <summary>FASE 8: messaggio empty state distinguente filtri attivi / tutto completato.</summary>
        public string EmptyStateTesto
        {
            get
            {
                if (HaChipFiltri)
                    return "Nessun ToDo corrisponde ai filtri attivi.";

                bool soloCompletati = SezioneScaduti.Conteggio == 0
                    && SezioneInProgramma.Conteggio == 0
                    && SezioneSenzaScadenza.Conteggio == 0
                    && SezioneCompletati.Conteggio > 0;

                return soloCompletati ? "Tutto completato!" : "Nessun ToDo da mostrare.";
            }
        }

        public string TestoBottoneFiltri => NumeroFiltriAttivi > 0
            ? $"Filtri ({NumeroFiltriAttivi})"
            : "Filtri";

        public string NotaCompletati => _filtroMostraTuttiCompletati
            ? "tutti"
            : (_filtroCampoData == "completamento" && (_filtroDataDa.HasValue || _filtroDataA.HasValue)
                ? "periodo filtrato"
                : $"ultimi {GiorniCompletatiDefault} giorni");

        // --- Pannelli ---

        private bool _overlayAperto;
        public bool OverlayAperto
        {
            get => _overlayAperto;
            set
            {
                if (_overlayAperto == value) return;
                _overlayAperto = value;
                OnPropertyChanged();
                if (value) FiltriAperti = false;
            }
        }

        private bool _filtriAperti;
        public bool FiltriAperti
        {
            get => _filtriAperti;
            set
            {
                if (_filtriAperti == value) return;
                _filtriAperti = value;
                OnPropertyChanged();
                if (value)
                {
                    OverlayAperto = false;
                    CopiaApplicatiInBozza();
                }
            }
        }

        private bool _mostraEliminaPanel;
        public bool MostraEliminaPanel { get => _mostraEliminaPanel; set { _mostraEliminaPanel = value; OnPropertyChanged(); } }

        private string _pannelloTitolo = string.Empty;
        public string PannelloTitolo { get => _pannelloTitolo; set { _pannelloTitolo = value; OnPropertyChanged(); } }

        private string _pannelloSottoTitolo = string.Empty;
        public string PannelloSottoTitolo { get => _pannelloSottoTitolo; set { _pannelloSottoTitolo = value; OnPropertyChanged(); } }

        private string _formCompletatoInfo = string.Empty;
        public string FormCompletatoInfo { get => _formCompletatoInfo; set { _formCompletatoInfo = value; OnPropertyChanged(); } }

        // --- Campi form ---

        private string _formTitolo = string.Empty;
        public string FormTitolo { get => _formTitolo; set { _formTitolo = value; OnPropertyChanged(); } }

        private string? _formDescrizione;
        public string? FormDescrizione { get => _formDescrizione; set { _formDescrizione = value; OnPropertyChanged(); } }

        private DateTime? _formDataScadenza;
        public DateTime? FormDataScadenza { get => _formDataScadenza; set { _formDataScadenza = value; OnPropertyChanged(); } }

        private PrioritaToDo _formPriorita = PrioritaToDo.Media;
        public PrioritaToDo FormPriorita { get => _formPriorita; set { _formPriorita = value; OnPropertyChanged(); } }

        private Cliente? _formCliente;
        public Cliente? FormCliente
        {
            get => _formCliente;
            set
            {
                if (_formCliente == value) return;
                _formCliente = value;
                OnPropertyChanged();

                if (value != null)
                    FormReferente = ReferentiTutti.FirstOrDefault(r => r.Id == value.ReferenteId);
            }
        }

        private Referente? _formReferente;
        public Referente? FormReferente
        {
            get => _formReferente;
            set
            {
                if (_formReferente == value) return;
                _formReferente = value;
                OnPropertyChanged();
                AggiornaClientiPerCombo();
            }
        }

        public ObservableCollection<ToDoSottoAttivita> FormSottoAttivita { get; } = new();

        private string _nuovaSottoAttivitaTesto = string.Empty;
        public string NuovaSottoAttivitaTesto { get => _nuovaSottoAttivitaTesto; set { _nuovaSottoAttivitaTesto = value; OnPropertyChanged(); } }

        // --- Comandi ---

        public ICommand ApriFiltriCommand { get; }
        public ICommand ApplicaFiltriCommand { get; }
        public ICommand AzzeraBozzaFiltriCommand { get; }
        public ICommand ChiudiFiltriCommand { get; }
        public ICommand RimuoviChipCommand { get; }

        public ICommand OrdinaPerScadenzaCommand { get; }
        public ICommand OrdinaPerCreazioneCommand { get; }

        public ICommand ToggleSezioneCommand { get; }

        public ICommand NuovoCommand { get; }
        public ICommand ApriModificaCommand { get; }
        public ICommand ToggleCompletatoCommand { get; }
        public ICommand ToggleEspansoCommand { get; }
        public ICommand ToggleSottoAttivitaInlineCommand { get; }

        public ICommand SalvaCommand { get; }
        public ICommand EliminaCommand { get; }
        public ICommand ChiudiOverlayCommand { get; }
        public ICommand PulisciClienteCommand { get; }
        public ICommand PulisciReferenteCommand { get; }
        public ICommand PulisciBozzaClienteCommand { get; }
        public ICommand PulisciBozzaReferenteCommand { get; }

        public ICommand AggiungiSottoAttivitaCommand { get; }
        public ICommand ToggleSottoAttivitaCommand { get; }
        public ICommand RimuoviSottoAttivitaCommand { get; }

        public ToDoViewModel()
        {
            SezioneScaduti = new SezioneToDoLista
            {
                Chiave = "scaduti",
                Titolo = "Scaduti",
                SempreVisibile = true,
                MostraPlaceholderVuoto = true,
                Espansa = true
            };
            SezioneInProgramma = new SezioneToDoLista
            {
                Chiave = "programma",
                Titolo = "In programma",
                SempreVisibile = false,
                Espansa = true
            };
            SezioneSenzaScadenza = new SezioneToDoLista
            {
                Chiave = "senza",
                Titolo = "Senza scadenza",
                SempreVisibile = false,
                Espansa = true
            };
            SezioneCompletati = new SezioneToDoLista
            {
                Chiave = "completati",
                Titolo = "Completati",
                SempreVisibile = true,
                Espansa = false
            };

            Sezioni = new ObservableCollection<SezioneToDoLista>
            {
                SezioneScaduti,
                SezioneInProgramma,
                SezioneSenzaScadenza,
                SezioneCompletati
            };

            ApriFiltriCommand = new RelayCommand(() => FiltriAperti = true);
            ApplicaFiltriCommand = new RelayCommand(ApplicaFiltri);
            AzzeraBozzaFiltriCommand = new RelayCommand(() =>
            {
                AzzeraBozzaFiltri();
                ApplicaFiltri();
            });
            ChiudiFiltriCommand = new RelayCommand(() => FiltriAperti = false);
            RimuoviChipCommand = new RelayCommand<string>(RimuoviChip);

            OrdinaPerScadenzaCommand = new RelayCommand(() => Ordinamento = OrdinamentoToDo.Scadenza);
            OrdinaPerCreazioneCommand = new RelayCommand(() => Ordinamento = OrdinamentoToDo.Creazione);

            ToggleSezioneCommand = new RelayCommand<SezioneToDoLista>(s =>
            {
                if (s != null) s.Espansa = !s.Espansa;
            });

            NuovoCommand = new RelayCommand(Nuovo);
            ApriModificaCommand = new RelayCommand<ToDo>(ApriModifica);
            ToggleCompletatoCommand = new RelayCommand<ToDo>(ToggleCompletato);
            ToggleEspansoCommand = new RelayCommand<ToDo>(ToggleEspanso);
            ToggleSottoAttivitaInlineCommand = new RelayCommand<ToDoSottoAttivita>(ToggleSottoAttivitaInline);

            SalvaCommand = new RelayCommand(Salva, () => !string.IsNullOrWhiteSpace(FormTitolo));
            EliminaCommand = new RelayCommand(Elimina);
            ChiudiOverlayCommand = new RelayCommand(ChiudiOverlay);
            PulisciClienteCommand = new RelayCommand(() => FormCliente = null);
            PulisciReferenteCommand = new RelayCommand(() => FormReferente = null);
            PulisciBozzaClienteCommand = new RelayCommand(() => BozzaCliente = null);
            PulisciBozzaReferenteCommand = new RelayCommand(() => BozzaReferente = null);

            AggiungiSottoAttivitaCommand = new RelayCommand(AggiungiSottoAttivita, () => !string.IsNullOrWhiteSpace(NuovaSottoAttivitaTesto));
            ToggleSottoAttivitaCommand = new RelayCommand<ToDoSottoAttivita>(s => { if (s != null) s.Completato = !s.Completato; });
            RimuoviSottoAttivitaCommand = new RelayCommand<ToDoSottoAttivita>(s => { if (s != null) FormSottoAttivita.Remove(s); });

            CaricaClientiEReferenti();
            Carica();
        }

        private void CaricaClientiEReferenti()
        {
            using var db = new ClabDbContext();

            _clientiTutti = db.Clienti.AsNoTracking()
                .Where(c => c.Stato == StatoCliente.Attivo)
                .OrderBy(c => c.RagioneSociale)
                .ToList();

            ClientiPerFiltro.Clear();
            foreach (var c in _clientiTutti)
                ClientiPerFiltro.Add(c);

            ReferentiTutti.Clear();
            foreach (var r in db.Referenti.AsNoTracking().Where(r => r.Attivo).OrderBy(r => r.Nome).ToList())
                ReferentiTutti.Add(r);

            AggiornaClientiPerCombo();
        }

        private void AggiornaClientiPerCombo()
        {
            ClientiPerCombo.Clear();

            IEnumerable<Cliente> lista = _clientiTutti;
            if (FormReferente != null && FormCliente == null)
                lista = _clientiTutti.Where(c => c.ReferenteId == FormReferente.Id);

            foreach (var c in lista)
                ClientiPerCombo.Add(c);

            if (FormCliente != null && !ClientiPerCombo.Any(c => c.Id == FormCliente.Id))
                ClientiPerCombo.Insert(0, FormCliente);
        }

        private void Carica()
        {
            using var db = new ClabDbContext();
            var elenco = db.ToDo.Include(t => t.SottoAttivita).AsNoTracking().ToList();

            var nomiClienti = db.Clienti.AsNoTracking().ToDictionary(c => c.Id, c => c.RagioneSociale);
            var nomiReferenti = db.Referenti.AsNoTracking().ToDictionary(r => r.Id, r => r.Nome);

            foreach (var t in elenco)
            {
                t.ClienteNome = t.ClienteId.HasValue && nomiClienti.TryGetValue(t.ClienteId.Value, out var cn)
                    ? cn
                    : (t.ClienteNomeStorico ?? string.Empty);

                t.ReferenteNome = t.ReferenteId.HasValue && nomiReferenti.TryGetValue(t.ReferenteId.Value, out var rn)
                    ? rn
                    : string.Empty;
            }

            _tuttiToDo = elenco;
            AggiornaLista();
        }

        private void CopiaApplicatiInBozza()
        {
            BozzaCliente = _filtroClienteId.HasValue
                ? ClientiPerFiltro.FirstOrDefault(c => c.Id == _filtroClienteId.Value)
                : null;
            BozzaReferente = _filtroReferenteId.HasValue
                ? ReferentiTutti.FirstOrDefault(r => r.Id == _filtroReferenteId.Value)
                : null;
            BozzaPriorita = _filtroPriorita;
            BozzaCollegamento = _filtroCollegamento;
            BozzaPassi = _filtroPassi;
            BozzaCampoData = _filtroCampoData;
            BozzaDataDa = _filtroDataDa;
            BozzaDataA = _filtroDataA;
            BozzaMostraTuttiCompletati = _filtroMostraTuttiCompletati;
            BozzaSoloScaduti = _filtroSoloScaduti;
        }

        private void AzzeraBozzaFiltri()
        {
            BozzaCliente = null;
            BozzaReferente = null;
            BozzaPriorita = "tutte";
            BozzaCollegamento = "tutti";
            BozzaPassi = "tutti";
            BozzaCampoData = "scadenza";
            BozzaDataDa = null;
            BozzaDataA = null;
            BozzaMostraTuttiCompletati = false;
            BozzaSoloScaduti = false;
        }

        private void ApplicaFiltri()
        {
            _filtroClienteId = BozzaCliente?.Id;
            _filtroClienteNome = BozzaCliente?.RagioneSociale ?? string.Empty;
            _filtroReferenteId = BozzaReferente?.Id;
            _filtroReferenteNome = BozzaReferente?.Nome ?? string.Empty;
            _filtroPriorita = BozzaPriorita ?? "tutte";
            _filtroCollegamento = BozzaCollegamento ?? "tutti";
            _filtroPassi = BozzaPassi ?? "tutti";
            _filtroCampoData = BozzaCampoData ?? "scadenza";
            _filtroDataDa = BozzaDataDa;
            _filtroDataA = BozzaDataA;
            _filtroMostraTuttiCompletati = BozzaMostraTuttiCompletati;
            _filtroSoloScaduti = BozzaSoloScaduti;

            FiltriAperti = false;
            AggiornaLista();
        }

        /// <summary>
        /// Navigazione contestuale (FASE 4): apre il modulo con i filtri già
        /// applicati. Passa dal sistema filtri esistente (bozza → applicati):
        /// nessun sistema di filtraggio parallelo.
        /// </summary>
        public void ApriConFiltri(int? clienteId = null, bool soloScaduti = false, bool prioritaAlta = false)
        {
            if (clienteId.HasValue)
            {
                var cliente = ClientiPerFiltro.FirstOrDefault(c => c.Id == clienteId.Value);
                if (cliente == null)
                {
                    // Il cliente potrebbe non essere più "Attivo": lo carico
                    // comunque, così il filtro contestuale resta applicabile.
                    using var db = new ClabDbContext();
                    cliente = db.Clienti.AsNoTracking().FirstOrDefault(c => c.Id == clienteId.Value);
                    if (cliente != null)
                        ClientiPerFiltro.Add(cliente);
                }

                if (cliente != null)
                    BozzaCliente = cliente;
            }

            if (prioritaAlta)
                BozzaPriorita = "alta";

            BozzaSoloScaduti = soloScaduti;

            ApplicaFiltri();
        }

        private void RimuoviChip(string? chiave)
        {
            switch (chiave)
            {
                case "cliente":
                    _filtroClienteId = null;
                    _filtroClienteNome = string.Empty;
                    break;
                case "referente":
                    _filtroReferenteId = null;
                    _filtroReferenteNome = string.Empty;
                    break;
                case "priorita":
                    _filtroPriorita = "tutte";
                    break;
                case "collegamento":
                    _filtroCollegamento = "tutti";
                    break;
                case "passi":
                    _filtroPassi = "tutti";
                    break;
                case "date":
                    _filtroDataDa = null;
                    _filtroDataA = null;
                    _filtroCampoData = "scadenza";
                    break;
                case "completati":
                    _filtroMostraTuttiCompletati = false;
                    break;
                case "soloScaduti":
                    _filtroSoloScaduti = false;
                    break;
            }

            AggiornaLista();
        }

        private void AggiornaChipFiltri()
        {
            ChipFiltri.Clear();

            if (_filtroClienteId.HasValue)
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "cliente", Testo = $"Cliente · {_filtroClienteNome} ×" });
            if (_filtroReferenteId.HasValue)
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "referente", Testo = $"Referente · {_filtroReferenteNome} ×" });
            if (_filtroPriorita != "tutte")
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "priorita", Testo = $"Priorità · {EtichettaPriorita(_filtroPriorita)} ×" });
            if (_filtroCollegamento != "tutti")
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "collegamento", Testo = $"Collegamento · {EtichettaCollegamento(_filtroCollegamento)} ×" });
            if (_filtroPassi != "tutti")
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "passi", Testo = $"Passi · {EtichettaPassi(_filtroPassi)} ×" });
            if (_filtroDataDa.HasValue || _filtroDataA.HasValue)
            {
                string da = _filtroDataDa?.ToString("dd/MM/yyyy") ?? "…";
                string a = _filtroDataA?.ToString("dd/MM/yyyy") ?? "…";
                ChipFiltri.Add(new ChipFiltroToDo
                {
                    Chiave = "date",
                    Testo = $"{EtichettaCampoData(_filtroCampoData)} · {da} – {a} ×"
                });
            }
            if (_filtroMostraTuttiCompletati)
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "completati", Testo = "Completati · tutti ×" });
            if (_filtroSoloScaduti)
                ChipFiltri.Add(new ChipFiltroToDo { Chiave = "soloScaduti", Testo = "Solo scaduti ×" });

            NumeroFiltriAttivi = ChipFiltri.Count;
            OnPropertyChanged(nameof(NumeroFiltriAttivi));
            OnPropertyChanged(nameof(HaChipFiltri));
            OnPropertyChanged(nameof(TestoBottoneFiltri));
            OnPropertyChanged(nameof(NotaCompletati));
        }

        private static string EtichettaPriorita(string key) => key switch
        {
            "alta" => "Alta",
            "media" => "Media",
            "bassa" => "Bassa",
            _ => "Tutte"
        };

        private static string EtichettaCollegamento(string key) => key switch
        {
            "cliente" => "Con cliente",
            "interno" => "Senza cliente",
            "referente" => "Con referente",
            _ => "Tutti"
        };

        private static string EtichettaPassi(string key) => key switch
        {
            "con" => "Con sotto-attività",
            "senza" => "Senza sotto-attività",
            "aperti" => "Passi aperti",
            _ => "Tutti"
        };

        private static string EtichettaCampoData(string key) => key switch
        {
            "completamento" => "Completamento",
            "creazione" => "Creazione",
            _ => "Scadenza"
        };

        private void AggiornaLista()
        {
            var q = (FiltroTesto ?? string.Empty).Trim().ToLower();
            var oggi = DateTime.Today;
            var limiteCompletati = oggi.AddDays(-GiorniCompletatiDefault);
            bool archivioPerDate = _filtroCampoData == "completamento"
                && (_filtroDataDa.HasValue || _filtroDataA.HasValue);

            IEnumerable<ToDo> baseLista = _tuttiToDo;

            if (!string.IsNullOrEmpty(q))
            {
                baseLista = baseLista.Where(t =>
                    t.Titolo.ToLower().Contains(q) ||
                    (t.Descrizione ?? string.Empty).ToLower().Contains(q) ||
                    t.ClienteNome.ToLower().Contains(q) ||
                    t.ReferenteNome.ToLower().Contains(q));
            }

            if (_filtroClienteId.HasValue)
                baseLista = baseLista.Where(t => t.ClienteId == _filtroClienteId.Value);

            if (_filtroReferenteId.HasValue)
                baseLista = baseLista.Where(t => t.ReferenteId == _filtroReferenteId.Value);

            if (_filtroPriorita != "tutte")
            {
                var p = _filtroPriorita switch
                {
                    "alta" => PrioritaToDo.Alta,
                    "bassa" => PrioritaToDo.Bassa,
                    _ => PrioritaToDo.Media
                };
                baseLista = baseLista.Where(t => t.Priorita == p);
            }

            baseLista = _filtroCollegamento switch
            {
                "cliente" => baseLista.Where(t => t.ClienteId.HasValue),
                "interno" => baseLista.Where(t => !t.ClienteId.HasValue),
                "referente" => baseLista.Where(t => t.ReferenteId.HasValue),
                _ => baseLista
            };

            baseLista = _filtroPassi switch
            {
                "con" => baseLista.Where(t => t.HaSottoAttivita),
                "senza" => baseLista.Where(t => !t.HaSottoAttivita),
                "aperti" => baseLista.Where(t => t.HaSottoAttivita && t.SottoAttivitaCompletate < t.SottoAttivitaTotali),
                _ => baseLista
            };

            if (_filtroSoloScaduti)
                baseLista = baseLista.Where(t => t.IsScaduto);

            if (_filtroDataDa.HasValue || _filtroDataA.HasValue)
            {
                baseLista = baseLista.Where(t =>
                {
                    DateTime? data = _filtroCampoData switch
                    {
                        "completamento" => t.DataCompletamento,
                        "creazione" => t.DataCreazione,
                        _ => t.DataScadenza
                    };
                    if (!data.HasValue) return false;
                    if (_filtroDataDa.HasValue && data.Value.Date < _filtroDataDa.Value.Date) return false;
                    if (_filtroDataA.HasValue && data.Value.Date > _filtroDataA.Value.Date) return false;
                    return true;
                });
            }

            if (!_filtroMostraTuttiCompletati && !archivioPerDate)
            {
                baseLista = baseLista.Where(t =>
                    !t.Completato
                    || (t.DataCompletamento ?? t.DataCreazione).Date >= limiteCompletati);
            }

            var filtrati = baseLista.ToList();

            foreach (var t in filtrati)
                t.IsEspanso = _espansi.Contains(t.Id);

            PopolaSezione(SezioneScaduti, OrdinaSezione(
                filtrati.Where(t => !t.Completato && t.IsScaduto),
                perScadenza: true));

            PopolaSezione(SezioneInProgramma, OrdinaSezione(
                filtrati.Where(t => !t.Completato && t.DataScadenza.HasValue && !t.IsScaduto),
                perScadenza: true));

            PopolaSezione(SezioneSenzaScadenza, OrdinaSezione(
                filtrati.Where(t => !t.Completato && !t.DataScadenza.HasValue),
                perScadenza: false));

            PopolaSezione(SezioneCompletati, OrdinaSezione(
                filtrati.Where(t => t.Completato),
                perScadenza: false, completati: true));

            ListaVuota = filtrati.Count == 0;
            OnPropertyChanged(nameof(ListaVuota));
            OnPropertyChanged(nameof(EmptyStateTesto));
            SezioneCompletati.Nota = NotaCompletati;
            AggiornaChipFiltri();
        }

        private IEnumerable<ToDo> OrdinaSezione(IEnumerable<ToDo> elenco, bool perScadenza, bool completati = false)
        {
            if (Ordinamento == OrdinamentoToDo.Creazione)
                return elenco.OrderByDescending(t => t.DataCreazione).ThenByDescending(t => t.Priorita);

            if (completati)
                return elenco.OrderByDescending(t => t.DataCompletamento ?? t.DataCreazione).ThenByDescending(t => t.Priorita);

            if (perScadenza)
                return elenco.OrderBy(t => t.DataScadenza ?? DateTime.MaxValue).ThenByDescending(t => t.Priorita);

            return elenco.OrderByDescending(t => t.Priorita).ThenByDescending(t => t.DataCreazione);
        }

        private static void PopolaSezione(SezioneToDoLista sezione, IEnumerable<ToDo> elenco)
        {
            sezione.Righe.Clear();
            foreach (var t in elenco)
                sezione.Righe.Add(t);
            sezione.NotificaConteggio();
        }

        private void Nuovo()
        {
            _todoInModificaId = 0;

            FormTitolo = string.Empty;
            FormDescrizione = null;
            FormDataScadenza = null;
            FormPriorita = PrioritaToDo.Media;
            FormCliente = null;
            FormReferente = null;
            FormSottoAttivita.Clear();
            NuovaSottoAttivitaTesto = string.Empty;
            AggiornaClientiPerCombo();

            PannelloTitolo = "Nuovo ToDo";
            PannelloSottoTitolo = "NUOVO TODO";
            FormCompletatoInfo = string.Empty;
            MostraEliminaPanel = false;
            OverlayAperto = true;
        }

        private void ApriModifica(ToDo? t)
        {
            if (t == null) return;

            _todoInModificaId = t.Id;

            FormTitolo = t.Titolo;
            FormDescrizione = t.Descrizione;
            FormDataScadenza = t.DataScadenza;
            FormPriorita = t.Priorita;
            FormCliente = _clientiTutti.FirstOrDefault(c => c.Id == t.ClienteId);
            FormReferente = ReferentiTutti.FirstOrDefault(r => r.Id == t.ReferenteId);

            FormSottoAttivita.Clear();
            foreach (var s in t.SottoAttivita.OrderBy(s => s.Ordine))
                FormSottoAttivita.Add(new ToDoSottoAttivita
                {
                    Id = s.Id,
                    ToDoId = s.ToDoId,
                    Testo = s.Testo,
                    Completato = s.Completato,
                    Ordine = s.Ordine
                });
            NuovaSottoAttivitaTesto = string.Empty;
            AggiornaClientiPerCombo();

            PannelloTitolo = t.Titolo;
            PannelloSottoTitolo = "MODIFICA TODO";
            FormCompletatoInfo = t.Completato && t.DataCompletamento.HasValue
                ? $"Completato il {t.DataCompletamento:dd/MM/yyyy}"
                : string.Empty;
            MostraEliminaPanel = true;
            OverlayAperto = true;
        }

        private void ToggleEspanso(ToDo? t)
        {
            if (t == null) return;

            t.IsEspanso = !t.IsEspanso;
            if (t.IsEspanso) _espansi.Add(t.Id);
            else _espansi.Remove(t.Id);
        }

        private void ToggleCompletato(ToDo? t)
        {
            if (t == null) return;

            using var db = new ClabDbContext();
            var entita = db.ToDo.Include(x => x.SottoAttivita).First(x => x.Id == t.Id);
            entita.Completato = !entita.Completato;
            entita.DataCompletamento = entita.Completato ? DateTime.Now : null;

            if (entita.Completato)
            {
                foreach (var s in entita.SottoAttivita)
                    s.Completato = true;
            }

            db.SaveChanges();
            Carica();
        }

        private void ToggleSottoAttivitaInline(ToDoSottoAttivita? s)
        {
            if (s == null) return;

            using var db = new ClabDbContext();
            var entita = db.ToDoSottoAttivita.First(x => x.Id == s.Id);
            entita.Completato = !entita.Completato;

            var tutte = db.ToDoSottoAttivita.Where(x => x.ToDoId == entita.ToDoId).ToList();
            bool tutteComplete = tutte.Count > 0 && tutte.All(x => x.Completato);

            var todo = db.ToDo.First(x => x.Id == entita.ToDoId);
            if (tutteComplete && !todo.Completato)
            {
                todo.Completato = true;
                todo.DataCompletamento = DateTime.Now;
            }
            else if (!tutteComplete && todo.Completato)
            {
                todo.Completato = false;
                todo.DataCompletamento = null;
            }

            db.SaveChanges();
            Carica();
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(FormTitolo))
                return;

            using var db = new ClabDbContext();

            ToDo entita;
            if (_todoInModificaId == 0)
            {
                entita = new ToDo { DataCreazione = DateTime.Now };
                db.ToDo.Add(entita);
                db.SaveChanges();
            }
            else
            {
                entita = db.ToDo.First(x => x.Id == _todoInModificaId);

                var vecchie = db.ToDoSottoAttivita.Where(x => x.ToDoId == entita.Id).ToList();
                db.ToDoSottoAttivita.RemoveRange(vecchie);
            }

            entita.Titolo = FormTitolo.Trim();
            entita.Descrizione = string.IsNullOrWhiteSpace(FormDescrizione) ? null : FormDescrizione.Trim();
            entita.DataScadenza = FormDataScadenza;
            entita.Priorita = FormPriorita;
            entita.ClienteId = FormCliente?.Id;
            entita.ReferenteId = FormReferente?.Id;

            if (FormCliente != null)
                entita.ClienteNomeStorico = null;

            int ordine = 0;
            foreach (var s in FormSottoAttivita)
            {
                db.ToDoSottoAttivita.Add(new ToDoSottoAttivita
                {
                    ToDoId = entita.Id,
                    Testo = s.Testo,
                    Completato = s.Completato,
                    Ordine = ordine++
                });
            }

            db.SaveChanges();

            ChiudiOverlay();
            Carica();
        }

        private void Elimina()
        {
            if (_todoInModificaId == 0) return;

            var ris = MessageBox.Show(
                $"Eliminare il ToDo \"{FormTitolo}\"?",
                "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ris != MessageBoxResult.Yes) return;

            using var db = new ClabDbContext();
            var entita = db.ToDo.FirstOrDefault(x => x.Id == _todoInModificaId);
            if (entita != null)
            {
                db.ToDo.Remove(entita);
                db.SaveChanges();
            }

            ChiudiOverlay();
            Carica();
        }

        private void ChiudiOverlay()
        {
            OverlayAperto = false;
            FiltriAperti = false;
        }

        private void AggiungiSottoAttivita()
        {
            if (string.IsNullOrWhiteSpace(NuovaSottoAttivitaTesto))
                return;

            FormSottoAttivita.Add(new ToDoSottoAttivita
            {
                Id = _sottoAttivitaTempId--,
                Testo = NuovaSottoAttivitaTesto.Trim(),
                Completato = false,
                Ordine = FormSottoAttivita.Count
            });

            NuovaSottoAttivitaTesto = string.Empty;
        }
    }
}
