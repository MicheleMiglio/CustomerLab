using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CLab.ViewModels
{
    public class ScadenzarioViewModel : ViewModelBase
    {
        public ObservableCollection<Cliente> ClientiDisponibili { get; set; } = new();

        private List<Cliente> _clientiCompleti = new();

        public ObservableCollection<Referente> ReferentiFiltro { get; set; } = new();

        private Referente? _referenteFiltro;
        public Referente? ReferenteFiltro
        {
            get => _referenteFiltro;
            set { _referenteFiltro = value; OnPropertyChanged(); ApplicaFiltroCliente(); }
        }

        private Cliente? _clienteSelezionato;
        public Cliente? ClienteSelezionato
        {
            get => _clienteSelezionato;
            set
            {
                _clienteSelezionato = value;
                OnPropertyChanged();
                NessunClienteSelezionato = value == null;
                CaricaTuttoPerCliente();
                OnPropertyChanged(nameof(MostraAvvisoClienteNonAttivo));
                OnPropertyChanged(nameof(TestoAvvisoClienteNonAttivo));
            }
        }

        public bool MostraAvvisoClienteNonAttivo => ClienteSelezionato != null && ClienteSelezionato.Stato != StatoCliente.Attivo;

        public string TestoAvvisoClienteNonAttivo => ClienteSelezionato == null ? "" :
            $"Questo cliente risulta \"{ClienteSelezionato.Stato}\". Vai su Clienti per riattivarlo, se necessario.";

        private bool _nessunClienteSelezionato = true;
        public bool NessunClienteSelezionato
        {
            get => _nessunClienteSelezionato;
            set { _nessunClienteSelezionato = value; OnPropertyChanged(); }
        }

        private int _annoSelezionato = DateTime.Now.Year;
        public int AnnoSelezionato
        {
            get => _annoSelezionato;
            set { _annoSelezionato = value; OnPropertyChanged(); CaricaTuttoPerCliente(); }
        }

        private bool _haAttivitaConfigurate;
        public bool HaAttivitaConfigurate
        {
            get => _haAttivitaConfigurate;
            set { _haAttivitaConfigurate = value; OnPropertyChanged(); }
        }

        private bool _mostraEmptyState;
        public bool MostraEmptyState
        {
            get => _mostraEmptyState;
            set { _mostraEmptyState = value; OnPropertyChanged(); }
        }

        private readonly Action<string> _apriConfigurazioneAttivita;

        public ICommand ConfiguraAttivitaCommand { get; }
        public ICommand AnnoPrecedenteCommand { get; }
        public ICommand AnnoSuccessivoCommand { get; }

        // --- Schede: Dashboard / Adempimenti / Ritenute d'acconto ---

        private enum Scheda { Dashboard, Adempimenti, Ritenute }
        private Scheda _scheda = Scheda.Dashboard;

        public bool MostraDashboard => _scheda == Scheda.Dashboard;
        public bool MostraAdempimenti => _scheda == Scheda.Adempimenti;
        public bool MostraRitenute => _scheda == Scheda.Ritenute;

        public ICommand MostraDashboardCommand { get; }
        public ICommand MostraAdempimentiCommand { get; }
        public ICommand MostraRitenuteCommand { get; }

        private void CambiaScheda(Scheda nuova)
        {
            _scheda = nuova;
            OnPropertyChanged(nameof(MostraDashboard));
            OnPropertyChanged(nameof(MostraAdempimenti));
            OnPropertyChanged(nameof(MostraRitenute));
        }

        // --- Nota cliente ---

        private string _notaCliente = string.Empty;
        public string NotaCliente
        {
            get => _notaCliente;
            set
            {
                if (_notaCliente == value) return;
                _notaCliente = value;
                OnPropertyChanged();
                SalvaNota();
            }
        }

        // --- Duplica da un altro cliente ---

        private bool _pannelloDuplicaAperto;
        public bool PannelloDuplicaAperto
        {
            get => _pannelloDuplicaAperto;
            set { _pannelloDuplicaAperto = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Cliente> ClientiDuplicabili { get; set; } = new();

        private Cliente? _clienteSorgenteSelezionato;
        public Cliente? ClienteSorgenteSelezionato
        {
            get => _clienteSorgenteSelezionato;
            set { _clienteSorgenteSelezionato = value; OnPropertyChanged(); CaricaAnteprimaDuplica(); }
        }

        public ObservableCollection<VoceDuplica> AttivitaAssegnate { get; set; } = new();
        public ObservableCollection<VoceDuplica> AttivitaNonAssegnate { get; set; } = new();

        public ICommand RimuoviDaAssegnateCommand { get; }
        public ICommand AggiungiAdAssegnateCommand { get; }
        public ICommand ApriPannelloDuplicaCommand { get; }
        public ICommand SalvaDuplicaCommand { get; }
        public ICommand AnnullaDuplicaCommand { get; }

        // --- Ricerca condivisa tra le tre sezioni ---

        private string _filtroAttivitaTesto = string.Empty;
        public string FiltroAttivitaTesto
        {
            get => _filtroAttivitaTesto;
            set { _filtroAttivitaTesto = value; OnPropertyChanged(); ApplicaFiltroAttivita(); }
        }

        // --- Sezioni: chiuse di default, con stato aggregato ---

        private bool _sezioneMensiliEspansa;
        public bool SezioneMensiliEspansa { get => _sezioneMensiliEspansa; set { _sezioneMensiliEspansa = value; OnPropertyChanged(); } }

        private bool _sezioneTrimestraliEspansa;
        public bool SezioneTrimestraliEspansa { get => _sezioneTrimestraliEspansa; set { _sezioneTrimestraliEspansa = value; OnPropertyChanged(); } }

        private bool _sezioneAnnualiEspansa;
        public bool SezioneAnnualiEspansa { get => _sezioneAnnualiEspansa; set { _sezioneAnnualiEspansa = value; OnPropertyChanged(); } }

        public ICommand ToggleSezioneMensiliCommand { get; }
        public ICommand ToggleSezioneTrimestraliCommand { get; }
        public ICommand ToggleSezioneAnnualiCommand { get; }

        private string _statoSezioneMensili = "Futuro";
        public string StatoSezioneMensili { get => _statoSezioneMensili; private set { _statoSezioneMensili = value; OnPropertyChanged(); } }

        private string _statoSezioneTrimestrali = "Futuro";
        public string StatoSezioneTrimestrali { get => _statoSezioneTrimestrali; private set { _statoSezioneTrimestrali = value; OnPropertyChanged(); } }

        private string _statoSezioneAnnuali = "Futuro";
        public string StatoSezioneAnnuali { get => _statoSezioneAnnuali; private set { _statoSezioneAnnuali = value; OnPropertyChanged(); } }

        // --- Contenuto: le tre sezioni compilabili ---

        public ObservableCollection<RigaAttivitaCompilazione> RigheMensili { get; set; } = new();
        public ObservableCollection<RigaAttivitaCompilazione> RigheTrimestrali { get; set; } = new();
        public ObservableCollection<RigaAttivitaCompilazione> RigheAnnuali { get; set; } = new();

        public ObservableCollection<RigaAttivitaCompilazione> RigheMensiliFiltrate { get; set; } = new();
        public ObservableCollection<RigaAttivitaCompilazione> RigheTrimestraliFiltrate { get; set; } = new();
        public ObservableCollection<RigaAttivitaCompilazione> RigheAnnualiFiltrate { get; set; } = new();

        private bool _haRigheMensili;
        public bool HaRigheMensili { get => _haRigheMensili; set { _haRigheMensili = value; OnPropertyChanged(); } }

        private bool _haRigheTrimestrali;
        public bool HaRigheTrimestrali { get => _haRigheTrimestrali; set { _haRigheTrimestrali = value; OnPropertyChanged(); } }

        private bool _haRigheAnnuali;
        public bool HaRigheAnnuali { get => _haRigheAnnuali; set { _haRigheAnnuali = value; OnPropertyChanged(); } }

        // --- Dashboard ---

        private string _completamentoMensiliTesto = "—";
        public string CompletamentoMensiliTesto { get => _completamentoMensiliTesto; private set { _completamentoMensiliTesto = value; OnPropertyChanged(); } }

        private string _completamentoTrimestraliTesto = "—";
        public string CompletamentoTrimestraliTesto { get => _completamentoTrimestraliTesto; private set { _completamentoTrimestraliTesto = value; OnPropertyChanged(); } }

        private string _completamentoAnnualiTesto = "—";
        public string CompletamentoAnnualiTesto { get => _completamentoAnnualiTesto; private set { _completamentoAnnualiTesto = value; OnPropertyChanged(); } }

        private int _totaleInRitardo;
        public int TotaleInRitardo { get => _totaleInRitardo; private set { _totaleInRitardo = value; OnPropertyChanged(); } }

        private int _totaleInCorso;
        public int TotaleInCorso { get => _totaleInCorso; private set { _totaleInCorso = value; OnPropertyChanged(); } }

        private int _totaleCompletateAdOggi;
        public int TotaleCompletateAdOggi { get => _totaleCompletateAdOggi; private set { _totaleCompletateAdOggi = value; OnPropertyChanged(); } }

        private GraficoTorta _tortaMensili = new();
        public GraficoTorta TortaMensili { get => _tortaMensili; private set { _tortaMensili = value; OnPropertyChanged(); } }

        private GraficoTorta _tortaTrimestrali = new();
        public GraficoTorta TortaTrimestrali { get => _tortaTrimestrali; private set { _tortaTrimestrali = value; OnPropertyChanged(); } }

        private GraficoTorta _tortaAnnuali = new();
        public GraficoTorta TortaAnnuali { get => _tortaAnnuali; private set { _tortaAnnuali = value; OnPropertyChanged(); } }

        // --- Ritenute d'acconto ---

        private List<RitenutaAcconto> _ritenuteComplete = new();
        public ObservableCollection<RitenutaAcconto> RitenuteFiltrate { get; set; } = new();

        private string _filtroRitenuteTesto = string.Empty;
        public string FiltroRitenuteTesto
        {
            get => _filtroRitenuteTesto;
            set { _filtroRitenuteTesto = value; OnPropertyChanged(); ApplicaFiltroRitenute(); }
        }

        private string _totaleRitenuteTesto = "€ 0";
        public string TotaleRitenuteTesto { get => _totaleRitenuteTesto; private set { _totaleRitenuteTesto = value; OnPropertyChanged(); } }

        private string _totaleVersatoTesto = "€ 0";
        public string TotaleVersatoTesto { get => _totaleVersatoTesto; private set { _totaleVersatoTesto = value; OnPropertyChanged(); } }

        private string _residuoRitenuteTesto = "€ 0";
        public string ResiduoRitenuteTesto { get => _residuoRitenuteTesto; private set { _residuoRitenuteTesto = value; OnPropertyChanged(); } }

        private GraficoTorta _tortaRitenute = new();
        public GraficoTorta TortaRitenute { get => _tortaRitenute; private set { _tortaRitenute = value; OnPropertyChanged(); } }

        private int _ritenuteVersate;
        public int RitenuteVersate { get => _ritenuteVersate; private set { _ritenuteVersate = value; OnPropertyChanged(); } }

        private int _ritenuteDaVersare;
        public int RitenuteDaVersare { get => _ritenuteDaVersare; private set { _ritenuteDaVersare = value; OnPropertyChanged(); } }

        private int _ritenuteAnomalie;
        public int RitenuteAnomalie { get => _ritenuteAnomalie; private set { _ritenuteAnomalie = value; OnPropertyChanged(); } }

        private bool _pannelloRitenutaAperto;
        public bool PannelloRitenutaAperto { get => _pannelloRitenutaAperto; set { _pannelloRitenutaAperto = value; OnPropertyChanged(); } }

        private int _ritenutaInModificaId;

        private string _formIntestazione = string.Empty;
        public string FormIntestazione { get => _formIntestazione; set { _formIntestazione = value; OnPropertyChanged(); } }

        private string _formNumeroFattura = string.Empty;
        public string FormNumeroFattura { get => _formNumeroFattura; set { _formNumeroFattura = value; OnPropertyChanged(); } }

        private DateTime? _formDataFattura = DateTime.Now;
        public DateTime? FormDataFattura { get => _formDataFattura; set { _formDataFattura = value; OnPropertyChanged(); } }

        private DateTime? _formDataPagamentoFattura;
        public DateTime? FormDataPagamentoFattura { get => _formDataPagamentoFattura; set { _formDataPagamentoFattura = value; OnPropertyChanged(); } }

        private decimal? _formImportoRitenuta;
        public decimal? FormImportoRitenuta { get => _formImportoRitenuta; set { _formImportoRitenuta = value; OnPropertyChanged(); } }

        private DateTime? _formScadenzaVersamento;
        public DateTime? FormScadenzaVersamento { get => _formScadenzaVersamento; set { _formScadenzaVersamento = value; OnPropertyChanged(); } }

        private decimal? _formImportoVersato;
        public decimal? FormImportoVersato { get => _formImportoVersato; set { _formImportoVersato = value; OnPropertyChanged(); } }

        public ICommand NuovaRitenutaCommand { get; }
        public ICommand ModificaRitenutaCommand { get; }
        public ICommand SalvaRitenutaCommand { get; }
        public ICommand AnnullaRitenutaCommand { get; }
        public ICommand EliminaRitenutaCommand { get; }
        public ICommand SegnaVersatoInteroCommand { get; }
        public ICommand PulisciDataPagamentoFatturaCommand { get; }
        public ICommand PulisciDataPagamentoRitenutaCommand { get; }
        public ICommand PulisciImportoVersatoCommand { get; }

        public ScadenzarioViewModel(Action<string> apriConfigurazioneAttivita)
        {
            _apriConfigurazioneAttivita = apriConfigurazioneAttivita;

            ConfiguraAttivitaCommand = new RelayCommand(ConfiguraAttivita);
            AnnoPrecedenteCommand = new RelayCommand(() => AnnoSelezionato--);
            AnnoSuccessivoCommand = new RelayCommand(() => AnnoSelezionato++);

            MostraDashboardCommand = new RelayCommand(() => CambiaScheda(Scheda.Dashboard));
            MostraAdempimentiCommand = new RelayCommand(() => CambiaScheda(Scheda.Adempimenti));
            MostraRitenuteCommand = new RelayCommand(() => CambiaScheda(Scheda.Ritenute));

            ToggleSezioneMensiliCommand = new RelayCommand(() => SezioneMensiliEspansa = !SezioneMensiliEspansa);
            ToggleSezioneTrimestraliCommand = new RelayCommand(() => SezioneTrimestraliEspansa = !SezioneTrimestraliEspansa);
            ToggleSezioneAnnualiCommand = new RelayCommand(() => SezioneAnnualiEspansa = !SezioneAnnualiEspansa);

            ApriPannelloDuplicaCommand = new RelayCommand(ApriPannelloDuplica);
            SalvaDuplicaCommand = new RelayCommand(SalvaDuplica);
            RimuoviDaAssegnateCommand = new RelayCommand<VoceDuplica>(RimuoviDaAssegnate);
            AggiungiAdAssegnateCommand = new RelayCommand<VoceDuplica>(AggiungiAdAssegnate);
            AnnullaDuplicaCommand = new RelayCommand(() => PannelloDuplicaAperto = false);

            NuovaRitenutaCommand = new RelayCommand(NuovaRitenuta);
            ModificaRitenutaCommand = new RelayCommand<RitenutaAcconto>(ModificaRitenuta);
            SalvaRitenutaCommand = new RelayCommand(SalvaRitenuta);
            AnnullaRitenutaCommand = new RelayCommand(() => PannelloRitenutaAperto = false);
            EliminaRitenutaCommand = new RelayCommand<RitenutaAcconto>(EliminaRitenuta);
            SegnaVersatoInteroCommand = new RelayCommand(SegnaVersatoIntero);
            PulisciDataPagamentoFatturaCommand = new RelayCommand(() => FormDataPagamentoFattura = null);
            PulisciDataPagamentoRitenutaCommand = new RelayCommand(() => FormScadenzaVersamento = null);
            PulisciImportoVersatoCommand = new RelayCommand(() => FormImportoVersato = null);

            CaricaClienti();
        }

        private void CaricaClienti()
        {
            using var db = new ClabDbContext();
            _clientiCompleti = db.Clienti.AsNoTracking().OrderBy(x => x.RagioneSociale).ToList();

            ReferentiFiltro.Clear();
            foreach (var r in db.Referenti.AsNoTracking().Where(r => r.Attivo).OrderBy(r => r.Nome).ToList())
                ReferentiFiltro.Add(r);

            ApplicaFiltroCliente();
        }

        private void ApplicaFiltroCliente()
        {
            ClientiDisponibili.Clear();

            var filtrati = ReferenteFiltro == null
                ? _clientiCompleti
                : _clientiCompleti.Where(c => c.ReferenteId == ReferenteFiltro.Id);

            foreach (var c in filtrati) ClientiDisponibili.Add(c);

            if (ClienteSelezionato != null && !ClientiDisponibili.Contains(ClienteSelezionato))
                ClienteSelezionato = null;
        }

        private void ConfiguraAttivita()
        {
            if (ClienteSelezionato == null) return;
            _apriConfigurazioneAttivita(ClienteSelezionato.RagioneSociale);
        }

        // --- Caricamento unico per cliente+anno ---

        private void CaricaTuttoPerCliente()
        {
            RigheMensili.Clear();
            RigheTrimestrali.Clear();
            RigheAnnuali.Clear();

            if (ClienteSelezionato == null)
            {
                HaAttivitaConfigurate = false;
                MostraEmptyState = false;
                HaRigheMensili = HaRigheTrimestrali = HaRigheAnnuali = false;
                _notaCliente = string.Empty;
                OnPropertyChanged(nameof(NotaCliente));
                ApplicaFiltroAttivita();
                AzzeraDashboard();
                CaricaRitenute();
                return;
            }

            using var db = new ClabDbContext();

            var cliente = db.Clienti.AsNoTracking().First(c => c.Id == ClienteSelezionato.Id);
            _notaCliente = cliente.Note ?? string.Empty;
            OnPropertyChanged(nameof(NotaCliente));

            var idAttivitaAssegnate = db.ClientiAttivita.AsNoTracking()
                .Where(ca => ca.ClienteId == ClienteSelezionato.Id)
                .Select(ca => ca.AttivitaId)
                .ToList();

            var attivitaAssegnate = db.Attivita.AsNoTracking()
                .Where(a => idAttivitaAssegnate.Contains(a.Id))
                .OrderBy(a => a.Nome)
                .ToList();

            HaAttivitaConfigurate = attivitaAssegnate.Count > 0;
            MostraEmptyState = !HaAttivitaConfigurate;

            if (!HaAttivitaConfigurate)
            {
                ApplicaFiltroAttivita();
                AzzeraDashboard();
                CaricaRitenute();
                return;
            }

            var idAttivitaTendina = attivitaAssegnate.Where(a => a.TipoCampo == TipoCampoAttivita.Tendina).Select(a => a.Id).ToList();

            var opzioniPerAttivita = db.OpzioniAttivita.AsNoTracking()
                .Where(o => idAttivitaTendina.Contains(o.AttivitaId))
                .OrderBy(o => o.Ordine)
                .GroupBy(o => o.AttivitaId)
                .ToDictionary(g => g.Key, g => g.Select(o => o.Testo).ToList());

            var compilazioniEsistenti = db.Compilazioni.AsNoTracking()
                .Where(c => c.ClienteId == ClienteSelezionato.Id && c.Anno == AnnoSelezionato)
                .ToList();

            foreach (var attivita in attivitaAssegnate)
            {
                int numeroPeriodi = attivita.Periodicita switch
                {
                    Periodicita.Mensile => 12,
                    Periodicita.Trimestrale => 4,
                    _ => 1
                };

                var opzioni = opzioniPerAttivita.TryGetValue(attivita.Id, out var lista) ? lista : new List<string>();

                var riga = new RigaAttivitaCompilazione { AttivitaId = attivita.Id, Nome = attivita.Nome, Periodicita = attivita.Periodicita };

                for (int periodo = 1; periodo <= numeroPeriodi; periodo++)
                {
                    var esistente = compilazioniEsistenti.FirstOrDefault(c => c.AttivitaId == attivita.Id && c.Periodo == periodo);

                    var cella = new CellaCompilazione
                    {
                        AttivitaId = attivita.Id,
                        Periodo = periodo,
                        TipoCampo = attivita.TipoCampo,
                        TendinaRichiedeImporto = attivita.TendinaRichiedeImporto,
                        EImporto = attivita.NumeroEImporto,
                        TestoLunghezzaMassimaEffettiva = attivita.TestoLunghezzaMassima ?? 200,
                        Opzioni = opzioni
                    };

                    if (esistente != null)
                    {
                        cella.ValoreBooleano = esistente.ValoreBooleano;
                        cella.ValoreNumero = esistente.ValoreNumero;
                        cella.Commento = esistente.Commento;
                        if (attivita.TipoCampo == TipoCampoAttivita.Tendina) cella.ValoreTendina = esistente.ValoreTesto;
                        else cella.ValoreTesto = esistente.ValoreTesto;
                    }

                    cella.Stato = CalcolaStato(attivita.Periodicita, periodo, cella.Compilato);
                    cella.OnCambiata = c => SalvaCompilazione(riga, c);
                    riga.Celle.Add(cella);
                }

                if (numeroPeriodi > 1)
                {
                    for (int i = 0; i < riga.Celle.Count; i++)
                    {
                        var c = riga.Celle[i];
                        riga.Pillole.Add(new PeriodoPill
                        {
                            Indice = i,
                            Etichetta = EtichettaPeriodo(attivita.Periodicita, c.Periodo),
                            StatoColore = c.Stato,
                            SelezionaCommand = riga.SelezionaPeriodoCommand
                        });
                    }
                }

                int indiceDefault = 0;
                if (AnnoSelezionato == DateTime.Now.Year)
                {
                    if (attivita.Periodicita == Periodicita.Mensile) indiceDefault = DateTime.Now.Month - 1;
                    else if (attivita.Periodicita == Periodicita.Trimestrale) indiceDefault = (DateTime.Now.Month - 1) / 3;
                }
                riga.IndiceSelezionato = Math.Min(indiceDefault, riga.Celle.Count - 1);

                switch (attivita.Periodicita)
                {
                    case Periodicita.Mensile: RigheMensili.Add(riga); break;
                    case Periodicita.Trimestrale: RigheTrimestrali.Add(riga); break;
                    default: RigheAnnuali.Add(riga); break;
                }
            }

            HaRigheMensili = RigheMensili.Count > 0;
            HaRigheTrimestrali = RigheTrimestrali.Count > 0;
            HaRigheAnnuali = RigheAnnuali.Count > 0;

            ApplicaFiltroAttivita();
            CaricaDashboard();
            CaricaRitenute();
        }

        private void ApplicaFiltroAttivita()
        {
            void Filtra(ObservableCollection<RigaAttivitaCompilazione> origine, ObservableCollection<RigaAttivitaCompilazione> destinazione)
            {
                destinazione.Clear();
                var filtrate = string.IsNullOrWhiteSpace(FiltroAttivitaTesto)
                    ? origine
                    : origine.Where(r => r.Nome.Contains(FiltroAttivitaTesto, StringComparison.OrdinalIgnoreCase));
                foreach (var r in filtrate) destinazione.Add(r);
            }

            Filtra(RigheMensili, RigheMensiliFiltrate);
            Filtra(RigheTrimestrali, RigheTrimestraliFiltrate);
            Filtra(RigheAnnuali, RigheAnnualiFiltrate);
        }

        private void SalvaCompilazione(RigaAttivitaCompilazione riga, CellaCompilazione cella)
        {
            if (ClienteSelezionato == null) return;

            using var db = new ClabDbContext();

            var esistente = db.Compilazioni.FirstOrDefault(c =>
                c.ClienteId == ClienteSelezionato.Id && c.AttivitaId == cella.AttivitaId &&
                c.Anno == AnnoSelezionato && c.Periodo == cella.Periodo);

            if (esistente == null)
            {
                esistente = new Compilazione
                {
                    ClienteId = ClienteSelezionato.Id,
                    AttivitaId = cella.AttivitaId,
                    Anno = AnnoSelezionato,
                    Periodo = cella.Periodo
                };
                db.Compilazioni.Add(esistente);
            }

            esistente.ValoreBooleano = cella.ValoreBooleano;
            esistente.ValoreNumero = cella.ValoreNumero;
            esistente.Commento = cella.Commento;
            esistente.ValoreTesto = cella.TipoCampo == TipoCampoAttivita.Tendina ? cella.ValoreTendina : cella.ValoreTesto;

            db.SaveChanges();

            cella.Stato = CalcolaStato(riga.Periodicita, cella.Periodo, cella.Compilato);
            var pillola = riga.Pillole.FirstOrDefault(p => p.Indice == cella.Periodo - 1);
            if (pillola != null) pillola.StatoColore = cella.Stato;
            riga.NotificaRiepilogoCambiato();

            CaricaDashboard();
        }

        private void SalvaNota()
        {
            if (ClienteSelezionato == null) return;
            using var db = new ClabDbContext();
            var cliente = db.Clienti.Find(ClienteSelezionato.Id);
            if (cliente == null) return;
            cliente.Note = NotaCliente;
            db.SaveChanges();
        }

        // --- Calcolo stato periodo: 4 stati, "Futuro" escluso da ogni conteggio ---

        private string CalcolaStato(Periodicita periodicita, int periodo, bool compilato)
        {
            if (compilato) return "Compilato";

            var oggi = DateTime.Now;

            if (periodicita == Periodicita.Annuale)
                return AnnoSelezionato < oggi.Year ? "Ritardo" : "InCorso";

            if (AnnoSelezionato < oggi.Year) return "Ritardo";
            if (AnnoSelezionato > oggi.Year) return "Futuro";

            int correnteIndice = periodicita == Periodicita.Mensile ? oggi.Month : ((oggi.Month - 1) / 3) + 1;
            if (periodo < correnteIndice) return "Ritardo";
            if (periodo == correnteIndice) return "InCorso";
            return "Futuro";
        }

        private static string EtichettaPeriodo(Periodicita p, int periodo) => p switch
        {
            Periodicita.Mensile => NomeMese(periodo),
            Periodicita.Trimestrale => $"Q{periodo} · {NomeMese((periodo - 1) * 3 + 1)}-{NomeMese((periodo - 1) * 3 + 3)}",
            _ => ""
        };

        private static string NomeMese(int numero) => numero switch
        {
            1 => "GEN",
            2 => "FEB",
            3 => "MAR",
            4 => "APR",
            5 => "MAG",
            6 => "GIU",
            7 => "LUG",
            8 => "AGO",
            9 => "SET",
            10 => "OTT",
            11 => "NOV",
            12 => "DIC",
            _ => ""
        };

        // --- Dashboard ---

        private void CaricaDashboard()
        {
            var (compM, corsoM, ritM) = ContaStati(RigheMensili);
            var (compT, corsoT, ritT) = ContaStati(RigheTrimestrali);
            var (compA, corsoA, ritA) = ContaStati(RigheAnnuali);

            CompletamentoMensiliTesto = TestoPercentuale(compM, corsoM, ritM);
            CompletamentoTrimestraliTesto = TestoPercentuale(compT, corsoT, ritT);
            CompletamentoAnnualiTesto = TestoPercentuale(compA, corsoA, ritA);

            TortaMensili = CostruisciTorta(compM, corsoM, ritM);
            TortaTrimestrali = CostruisciTorta(compT, corsoT, ritT);
            TortaAnnuali = CostruisciTorta(compA, corsoA, ritA);

            TotaleInRitardo = ritM + ritT + ritA;
            TotaleInCorso = corsoM + corsoT + corsoA;
            TotaleCompletateAdOggi = compM + compT + compA;

            AggiornaStatiSezioni();
        }

        private void AggiornaStatiSezioni()
        {
            StatoSezioneMensili = StatoAggregato(RigheMensili);
            StatoSezioneTrimestrali = StatoAggregato(RigheTrimestrali);
            StatoSezioneAnnuali = StatoAggregato(RigheAnnuali);
        }

        private static string StatoAggregato(IEnumerable<RigaAttivitaCompilazione> righe)
        {
            var stati = righe.Select(r => r.StatoRiepilogo).ToList();
            if (stati.Count == 0) return "Futuro";
            if (stati.Contains("Ritardo")) return "Ritardo";
            if (stati.Contains("InCorso")) return "InCorso";
            return "Compilato";
        }

        // "Futuro" resta intenzionalmente escluso: non entra né nella torta né nella percentuale.
        private static (int compilate, int inCorso, int inRitardo) ContaStati(IEnumerable<RigaAttivitaCompilazione> righe)
        {
            int c = 0, i = 0, r = 0;
            foreach (var riga in righe)
                foreach (var cella in riga.Celle)
                {
                    switch (cella.Stato)
                    {
                        case "Compilato": c++; break;
                        case "InCorso": i++; break;
                        case "Ritardo": r++; break;
                    }
                }
            return (c, i, r);
        }

        private static string TestoPercentuale(int compilate, int inCorso, int inRitardo)
        {
            int totale = compilate + inCorso + inRitardo;
            return totale == 0 ? "—" : $"{Math.Round(compilate * 100.0 / totale)}%";
        }

        private static GraficoTorta CostruisciTorta(int completate, int inCorso, int inRitardo)
        {
            var torta = new GraficoTorta { Completate = completate, InCorso = inCorso, InRitardo = inRitardo };
            int totale = completate + inCorso + inRitardo;

            if (totale == 0)
            {
                torta.Vuoto = true;
                return torta;
            }

            double p1 = completate / (double)totale;
            double p2 = p1 + inCorso / (double)totale;

            torta.FettaCompletate = CreaSettore(0, p1);
            torta.FettaInCorso = CreaSettore(p1, p2);
            torta.FettaInRitardo = CreaSettore(p2, 1.0);
            return torta;
        }

        private static Geometry CreaSettore(double da, double a)
        {
            if (a - da <= 0.0005) return Geometry.Empty;

            const double cx = 50, cy = 50, raggio = 46;
            double angoloDa = da * 360 - 90;
            double angoloA = a * 360 - 90;
            var p0 = PuntoSuCirconferenza(cx, cy, raggio, angoloDa);
            var p1 = PuntoSuCirconferenza(cx, cy, raggio, angoloA);
            bool grandeArco = (angoloA - angoloDa) > 180;

            var figura = new PathFigure { StartPoint = new Point(cx, cy), IsClosed = true };
            figura.Segments.Add(new LineSegment(p0, true));
            figura.Segments.Add(new ArcSegment(p1, new Size(raggio, raggio), 0, grandeArco, SweepDirection.Clockwise, true));

            var geometria = new PathGeometry();
            geometria.Figures.Add(figura);
            return geometria;
        }

        private static Point PuntoSuCirconferenza(double cx, double cy, double r, double angoloGradi)
        {
            double rad = angoloGradi * Math.PI / 180.0;
            return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        private void AzzeraDashboard()
        {
            CompletamentoMensiliTesto = "—";
            CompletamentoTrimestraliTesto = "—";
            CompletamentoAnnualiTesto = "—";
            TotaleInRitardo = 0;
            TotaleInCorso = 0;
            TotaleCompletateAdOggi = 0;
            TortaMensili = new GraficoTorta { Vuoto = true };
            TortaTrimestrali = new GraficoTorta { Vuoto = true };
            TortaAnnuali = new GraficoTorta { Vuoto = true };
            StatoSezioneMensili = StatoSezioneTrimestrali = StatoSezioneAnnuali = "Futuro";
        }

        // --- Duplica da un altro cliente ---

        private void ApriPannelloDuplica()
        {
            if (ClienteSelezionato == null) return;

            using var db = new ClabDbContext();
            var idClientiConAttivita = db.ClientiAttivita.AsNoTracking().Select(ca => ca.ClienteId).Distinct().ToList();

            ClientiDuplicabili.Clear();
            foreach (var c in ClientiDisponibili.Where(c => c.Id != ClienteSelezionato.Id && idClientiConAttivita.Contains(c.Id)))
                ClientiDuplicabili.Add(c);

            ClienteSorgenteSelezionato = null;
            PannelloDuplicaAperto = true;
        }

        private void CaricaAnteprimaDuplica()
        {
            AttivitaAssegnate.Clear();
            AttivitaNonAssegnate.Clear();
            if (ClienteSorgenteSelezionato == null) return;

            using var db = new ClabDbContext();
            var idAssegnate = db.ClientiAttivita.AsNoTracking()
                .Where(ca => ca.ClienteId == ClienteSorgenteSelezionato.Id).Select(ca => ca.AttivitaId).ToList();

            foreach (var a in db.Attivita.AsNoTracking().OrderBy(a => a.Nome).ToList())
            {
                var voce = new VoceDuplica { AttivitaId = a.Id, Nome = a.Nome, Periodicita = a.Periodicita, TipoCampo = a.TipoCampo };
                if (idAssegnate.Contains(a.Id)) AttivitaAssegnate.Add(voce);
                else AttivitaNonAssegnate.Add(voce);
            }
        }

        private void RimuoviDaAssegnate(VoceDuplica? voce)
        {
            if (voce == null) return;
            AttivitaAssegnate.Remove(voce);
            AttivitaNonAssegnate.Add(voce);
        }

        private void AggiungiAdAssegnate(VoceDuplica? voce)
        {
            if (voce == null) return;
            AttivitaNonAssegnate.Remove(voce);
            AttivitaAssegnate.Add(voce);
        }

        private void SalvaDuplica()
        {
            if (ClienteSelezionato == null) return;

            using var db = new ClabDbContext();
            foreach (var voce in AttivitaAssegnate)
                db.ClientiAttivita.Add(new ClienteAttivita { ClienteId = ClienteSelezionato.Id, AttivitaId = voce.AttivitaId });

            db.SaveChanges();

            PannelloDuplicaAperto = false;
            CaricaTuttoPerCliente();
        }

        // --- Ritenute d'acconto ---

        private void CaricaRitenute()
        {
            _ritenuteComplete = new List<RitenutaAcconto>();

            if (ClienteSelezionato != null)
            {
                using var db = new ClabDbContext();
                _ritenuteComplete = db.RitenuteAcconto.AsNoTracking()
                    .Where(r => r.ClienteId == ClienteSelezionato.Id && r.DataFattura.Year == AnnoSelezionato)
                    .OrderByDescending(r => r.DataFattura)
                    .ToList();
            }

            ApplicaFiltroRitenute();
            AggiornaTotaliRitenute();
        }

        private void ApplicaFiltroRitenute()
        {
            RitenuteFiltrate.Clear();
            var filtrate = string.IsNullOrWhiteSpace(FiltroRitenuteTesto)
                ? _ritenuteComplete
                : _ritenuteComplete.Where(r => r.NumeroFattura.Contains(FiltroRitenuteTesto, StringComparison.OrdinalIgnoreCase));
            foreach (var r in filtrate) RitenuteFiltrate.Add(r);
        }

        private void AggiornaTotaliRitenute()
        {
            decimal totale = _ritenuteComplete.Sum(r => r.ImportoRitenuta);
            decimal versato = _ritenuteComplete.Sum(r => r.ImportoVersato ?? 0);

            TotaleRitenuteTesto = $"€ {totale:N0}";
            TotaleVersatoTesto = $"€ {versato:N0}";
            ResiduoRitenuteTesto = $"€ {(totale - versato):N0}";

            RitenuteVersate = _ritenuteComplete.Count(r => r.StatoVersamento == "Versato");
            RitenuteDaVersare = _ritenuteComplete.Count(r => r.StatoVersamento == "DaVersare");
            RitenuteAnomalie = _ritenuteComplete.Count(r => r.HaAnomalie);

            TortaRitenute = CostruisciTorta(RitenuteVersate, RitenuteDaVersare, RitenuteAnomalie);
        }

        private void NuovaRitenuta()
        {
            if (ClienteSelezionato == null) return;

            _ritenutaInModificaId = 0;
            FormIntestazione = string.Empty;
            FormNumeroFattura = string.Empty;
            FormDataFattura = DateTime.Now;
            FormDataPagamentoFattura = null;
            FormImportoRitenuta = null;
            FormScadenzaVersamento = null;
            FormImportoVersato = null;

            PannelloRitenutaAperto = true;
        }

        private void ModificaRitenuta(RitenutaAcconto? r)
        {
            if (r == null) return;

            _ritenutaInModificaId = r.Id;
            FormIntestazione = r.Intestazione;
            FormNumeroFattura = r.NumeroFattura;
            FormDataFattura = r.DataFattura;
            FormDataPagamentoFattura = r.DataPagamentoFattura;
            FormImportoRitenuta = r.ImportoRitenuta;
            FormScadenzaVersamento = r.ScadenzaVersamento;
            FormImportoVersato = r.ImportoVersato;

            PannelloRitenutaAperto = true;
        }

        private void SalvaRitenuta()
        {
            if (ClienteSelezionato == null) return;

            if (string.IsNullOrWhiteSpace(FormIntestazione) || string.IsNullOrWhiteSpace(FormNumeroFattura) ||
                !FormDataFattura.HasValue || !FormImportoRitenuta.HasValue)
            {
                MessageBox.Show("Intestazione, numero fattura, data fattura e importo ritenuta sono obbligatori.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new ClabDbContext();

            RitenutaAcconto entita;
            if (_ritenutaInModificaId == 0)
            {
                entita = new RitenutaAcconto { ClienteId = ClienteSelezionato.Id };
                db.RitenuteAcconto.Add(entita);
            }
            else
            {
                entita = db.RitenuteAcconto.First(r => r.Id == _ritenutaInModificaId);
            }

            entita.Intestazione = FormIntestazione.Trim();
            entita.NumeroFattura = FormNumeroFattura.Trim();
            entita.DataFattura = FormDataFattura.Value;
            entita.DataPagamentoFattura = FormDataPagamentoFattura;
            entita.ImportoRitenuta = FormImportoRitenuta.Value;
            entita.ScadenzaVersamento = FormScadenzaVersamento;
            entita.ImportoVersato = FormImportoVersato;

            db.SaveChanges();

            PannelloRitenutaAperto = false;
            CaricaRitenute();
        }

        private void EliminaRitenuta(RitenutaAcconto? r)
        {
            if (r == null) return;

            var esito = MessageBox.Show($"Eliminare la ritenuta della fattura \"{r.NumeroFattura}\"?", "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (esito != MessageBoxResult.Yes) return;

            using var db = new ClabDbContext();
            var entita = db.RitenuteAcconto.First(x => x.Id == r.Id);
            db.RitenuteAcconto.Remove(entita);
            db.SaveChanges();

            CaricaRitenute();
        }

        private void SegnaVersatoIntero()
        {
            FormImportoVersato = FormImportoRitenuta;
            FormScadenzaVersamento = DateTime.Now;
        }

        public void ApriConfigurazionePerCliente(string ragioneSociale)
        {
            // Punto di aggancio per MainViewModel; ConfiguraAttivita usa sempre il cliente già selezionato.
        }
    }

    public class RigaAttivitaCompilazione : ViewModelBase
    {
        public int AttivitaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public Periodicita Periodicita { get; set; }
        public ObservableCollection<CellaCompilazione> Celle { get; set; } = new();
        public ObservableCollection<PeriodoPill> Pillole { get; set; } = new();

        private bool _espansa;
        public bool Espansa { get => _espansa; set { _espansa = value; OnPropertyChanged(); } }

        private int _indiceSelezionato;
        public int IndiceSelezionato
        {
            get => _indiceSelezionato;
            set
            {
                _indiceSelezionato = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CellaSelezionata));
                foreach (var p in Pillole) p.Selezionato = p.Indice == value;
            }
        }

        public CellaCompilazione? CellaSelezionata => Celle.Count > IndiceSelezionato ? Celle[IndiceSelezionato] : null;

        public ICommand ToggleEspansaCommand { get; }
        public ICommand SelezionaPeriodoCommand { get; }

        public RigaAttivitaCompilazione()
        {
            ToggleEspansaCommand = new RelayCommand(() => Espansa = !Espansa);
            SelezionaPeriodoCommand = new RelayCommand<int>(i => IndiceSelezionato = i);
        }

        // "Futuro" escluso: una riga con tutto il passato/presente compilato è verde
        // anche se i mesi a venire non sono ancora stati raggiunti.
        public string StatoRiepilogo
        {
            get
            {
                var rilevanti = Celle.Where(c => c.Stato != "Futuro").ToList();
                if (rilevanti.Count == 0) return "Futuro";
                if (rilevanti.Any(c => c.Stato == "Ritardo")) return "Ritardo";
                if (rilevanti.Any(c => c.Stato == "InCorso")) return "InCorso";
                return "Compilato";
            }
        }

        public string TestoRiepilogo => Celle.Count == 0 ? "" : $"{Celle.Count(c => c.Stato == "Compilato")}/{Celle.Count}";

        public void NotificaRiepilogoCambiato()
        {
            OnPropertyChanged(nameof(StatoRiepilogo));
            OnPropertyChanged(nameof(TestoRiepilogo));
        }
    }

    public class PeriodoPill : ViewModelBase
    {
        public int Indice { get; set; }
        public string Etichetta { get; set; } = string.Empty;

        private string _statoColore = "Futuro";
        public string StatoColore { get => _statoColore; set { _statoColore = value; OnPropertyChanged(); } }

        private bool _selezionato;
        public bool Selezionato { get => _selezionato; set { _selezionato = value; OnPropertyChanged(); } }

        public ICommand? SelezionaCommand { get; set; }
    }

    public class CellaCompilazione : ViewModelBase
    {
        public int AttivitaId { get; set; }
        public int Periodo { get; set; }
        public TipoCampoAttivita TipoCampo { get; set; }
        public bool TendinaRichiedeImporto { get; set; }
        public bool EImporto { get; set; }
        public int TestoLunghezzaMassimaEffettiva { get; set; } = 200;
        public List<string> Opzioni { get; set; } = new();

        private string _stato = "Futuro";
        public string Stato { get => _stato; set { if (_stato == value) return; _stato = value; OnPropertyChanged(); } }

        public bool Compilato => TipoCampo switch
        {
            TipoCampoAttivita.SiNo => ValoreBooleano == true,
            TipoCampoAttivita.Numero => ValoreNumero.HasValue,
            TipoCampoAttivita.Tendina => !string.IsNullOrWhiteSpace(ValoreTendina),
            TipoCampoAttivita.TestoLibero => !string.IsNullOrWhiteSpace(ValoreTesto),
            _ => false
        };

        private bool? _valoreBooleano;
        public bool? ValoreBooleano
        {
            get => _valoreBooleano;
            set { if (_valoreBooleano == value) return; _valoreBooleano = value; OnPropertyChanged(); Salva(); }
        }

        private string? _valoreTesto;
        public string? ValoreTesto
        {
            get => _valoreTesto;
            set { if (_valoreTesto == value) return; _valoreTesto = value; OnPropertyChanged(); Salva(); }
        }

        private decimal? _valoreNumero;
        public decimal? ValoreNumero
        {
            get => _valoreNumero;
            set { if (_valoreNumero == value) return; _valoreNumero = value; OnPropertyChanged(); Salva(); }
        }

        private string? _valoreTendina;
        public string? ValoreTendina
        {
            get => _valoreTendina;
            set { if (_valoreTendina == value) return; _valoreTendina = value; OnPropertyChanged(); Salva(); }
        }

        private string? _commento;
        public string? Commento
        {
            get => _commento;
            set { if (_commento == value) return; _commento = value; OnPropertyChanged(); Salva(); }
        }

        public Action<CellaCompilazione>? OnCambiata;
        private void Salva() => OnCambiata?.Invoke(this);
    }

    public class GraficoTorta
    {
        public Geometry FettaCompletate { get; set; } = Geometry.Empty;
        public Geometry FettaInCorso { get; set; } = Geometry.Empty;
        public Geometry FettaInRitardo { get; set; } = Geometry.Empty;
        public bool Vuoto { get; set; }
        public int Completate { get; set; }
        public int InCorso { get; set; }
        public int InRitardo { get; set; }
    }
}