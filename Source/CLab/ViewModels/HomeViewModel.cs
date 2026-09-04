using CLab.Data;
using CLab.Models;
using CLab.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CLab.ViewModels
{
    /// <summary>
    /// Home operativa CLab 2.0 (FASE 4B): dashboard con priorità
    /// urgenze → attività → situazione generale → KPI secondari.
    /// Tutta la navigazione contestuale passa da INavigatore (implementato da
    /// MainViewModel); nessun accesso diretto ad altri ViewModel.
    /// I dati sono quelli esistenti: nessuna modifica a Models/DB.
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigatore? _navigatore;

        public string Saluto { get; }
        public string DataOggiTesto { get; }
        public string ContestoTesto { get; private set; } = string.Empty;

        // --- 1. Richiede attenzione ---

        public ObservableCollection<VoceAdempimentiRitardo> AdempimentiRitardoPerCliente { get; } = new();
        public bool HaAdempimentiRitardo => AdempimentiRitardoPerCliente.Count > 0;
        public int TotaleAdempimentiRitardo { get; private set; }
        public string AdempimentiRitardoTitolo => TotaleAdempimentiRitardo == 1
            ? "1 adempimento in ritardo"
            : $"{TotaleAdempimentiRitardo} adempimenti in ritardo";

        public int ToDoScaduti { get; private set; }
        public bool HaToDoScaduti => ToDoScaduti > 0;
        public string ToDoScadutiTesto => ToDoScaduti == 1 ? "1 ToDo scaduto" : $"{ToDoScaduti} ToDo scaduti";

        public int FattureScaduteNumero { get; private set; }
        public bool HaFattureScadute => FattureScaduteNumero > 0;
        public string FattureScaduteTesto => FattureScaduteNumero == 1
            ? "1 fattura scaduta non incassata"
            : $"{FattureScaduteNumero} fatture scadute non incassate";

        public int PromemoriaAltaPriorita { get; private set; }
        public bool HaPromemoriaAlta => PromemoriaAltaPriorita > 0;
        public string PromemoriaAltaTesto => PromemoriaAltaPriorita == 1
            ? "1 promemoria ad alta priorità"
            : $"{PromemoriaAltaPriorita} promemoria ad alta priorità";

        public bool TuttoOk => !HaAdempimentiRitardo && !HaToDoScaduti && !HaFattureScadute && !HaPromemoriaAlta;

        // --- 2. Prossime scadenze (prossimi 7 giorni) ---

        public ObservableCollection<VoceHomeToDo> ProssimeScadenze { get; } = new();
        public bool HaProssimeScadenze => ProssimeScadenze.Count > 0;

        // --- 3. ToDo urgenti (scaduti + alta priorità) ---

        public ObservableCollection<VoceHomeToDo> ToDoUrgenti { get; } = new();
        public bool HaToDoUrgenti => ToDoUrgenti.Count > 0;

        // --- 4. Promemoria in evidenza (solo alta priorità) ---

        public ObservableCollection<VoceHomePromemoria> PromemoriaInEvidenza { get; } = new();
        public bool HaPromemoriaInEvidenza => PromemoriaInEvidenza.Count > 0;

        // --- 5. Fatture (anno corrente) ---

        public string AnnoCorrente => DateTime.Now.Year.ToString();
        public int FattureDaIncassareNumero { get; private set; }
        public string FattureDaIncassareTesto { get; private set; } = "€ 0";
        public bool HaFattureDaIncassare => FattureDaIncassareNumero > 0;

        // --- 6. KPI secondari ---

        public int ClientiAttivi { get; private set; }
        public int FattureAnnoCorrente { get; private set; }
        public int ToDoAperti { get; private set; }
        public int PromemoriaTotali { get; private set; }

        // --- 7. Adempimenti anno corrente (barra segmentata, standard WPF) ---

        public bool HaAdempimentiAnno => AdempimentiTotaliAnno > 0;
        public int AdempimentiTotaliAnno => AdempimentiCompletati + AdempimentiInCorso + AdempimentiInRitardo;
        public int AdempimentiCompletati { get; private set; }
        public int AdempimentiInCorso { get; private set; }
        public int AdempimentiInRitardo { get; private set; }
        public double PctCompletati { get; private set; }
        public double PctInCorso { get; private set; }
        public double PctInRitardo { get; private set; }

        // --- Comandi (navigazione contestuale via INavigatore) ---

        public ICommand ApriClientiCommand { get; }
        public ICommand ApriToDoCommand { get; }
        public ICommand ApriPromemoriaCommand { get; }
        public ICommand ApriFattureAnnoCommand { get; }
        public ICommand ApriTuttiRitardiCommand { get; }
        public ICommand ApriRitardoClienteCommand { get; }
        public ICommand ApriToDoFiltratoCommand { get; }
        public ICommand ApriToDoScadutiCommand { get; }

        public HomeViewModel(INavigatore? navigatore)
        {
            _navigatore = navigatore;

            var ora = DateTime.Now;
            Saluto = ora.Hour switch
            {
                < 12 => "Buongiorno",
                < 18 => "Buon pomeriggio",
                _ => "Buonasera"
            };

            var cultura = new CultureInfo("it-IT");
            var testoData = ora.ToString("dddd d MMMM yyyy", cultura);
            DataOggiTesto = char.ToUpper(testoData[0]) + testoData[1..];

            ApriClientiCommand = new RelayCommand(() => _navigatore?.ApriClienti());
            ApriToDoCommand = new RelayCommand(() => _navigatore?.ApriToDo());
            ApriPromemoriaCommand = new RelayCommand(() => _navigatore?.ApriPromemoria());
            ApriFattureAnnoCommand = new RelayCommand(() => _navigatore?.ApriFatture(DateTime.Now.Year));
            ApriTuttiRitardiCommand = new RelayCommand(() => _navigatore?.ApriScadenzario());
            ApriRitardoClienteCommand = new RelayCommand<VoceAdempimentiRitardo>(v =>
            {
                if (v != null)
                    _navigatore?.ApriScadenzario(v.ClienteId, "adempimenti", true);
            });
            ApriToDoFiltratoCommand = new RelayCommand<VoceHomeToDo>(v =>
            {
                if (v != null)
                    _navigatore?.ApriToDo(v.ClienteId, v.IsScaduto, v.Priorita == PrioritaToDo.Alta);
            });
            ApriToDoScadutiCommand = new RelayCommand(() => _navigatore?.ApriToDo(soloScaduti: true));

            var (ritardoPerCliente, completati, inCorso, inRitardo) = CalcolaAdempimenti();
            AdempimentiCompletati = completati;
            AdempimentiInCorso = inCorso;
            AdempimentiInRitardo = inRitardo;
            TotaleAdempimentiRitardo = inRitardo;

            if (AdempimentiTotaliAnno > 0)
            {
                double totale = AdempimentiTotaliAnno;
                PctCompletati = completati / totale;
                PctInCorso = inCorso / totale;
                PctInRitardo = inRitardo / totale;
            }

            var nomiClienti = CaricaNomiClienti();
            foreach (var coppia in ritardoPerCliente
                         .OrderByDescending(c => c.Value)
                         .ThenBy(c => nomiClienti.TryGetValue(c.Key, out var nome) ? nome : string.Empty, StringComparer.CurrentCulture)
                         .Take(5))
            {
                AdempimentiRitardoPerCliente.Add(new VoceAdempimentiRitardo
                {
                    ClienteId = coppia.Key,
                    RagioneSociale = nomiClienti.TryGetValue(coppia.Key, out var nome) ? nome : $"Cliente #{coppia.Key}",
                    NumeroInRitardo = coppia.Value
                });
            }

            CaricaToDo(nomiClienti);
            CaricaPromemoria();
            CaricaFatture();
            ClientiAttivi = ContaClientiAttivi();

            var parti = new List<string>();
            if (ToDoScaduti > 0) parti.Add($"{ToDoScaduti} ToDo scaduti");
            if (TotaleAdempimentiRitardo > 0) parti.Add($"{TotaleAdempimentiRitardo} adempimenti in ritardo");
            if (FattureScaduteNumero > 0) parti.Add($"{FattureScaduteNumero} fatture scadute");
            if (PromemoriaAltaPriorita > 0) parti.Add($"{PromemoriaAltaPriorita} promemoria ad alta priorità");

            ContestoTesto = parti.Count == 0
                ? "Tutto in ordine: nessuna urgenza aperta."
                : string.Join(" · ", parti) + ".";
        }

        private void CaricaToDo(Dictionary<int, string> nomiClienti)
        {
            using var db = new ClabDbContext();
            var elenco = db.ToDo.AsNoTracking().ToList();

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

            var aperti = elenco.Where(t => !t.Completato).ToList();
            ToDoAperti = aperti.Count;
            ToDoScaduti = aperti.Count(t => t.IsScaduto);

            // Urgenti: prima gli scaduti (i più vecchi), poi l'alta priorità imminente.
            foreach (var t in aperti.Where(t => t.IsScaduto)
                         .OrderBy(t => t.DataScadenza ?? DateTime.MaxValue)
                         .ThenByDescending(t => t.Priorita)
                         .Take(3))
                ToDoUrgenti.Add(CreaVoce(t));

            foreach (var t in aperti.Where(t => !t.IsScaduto && t.Priorita == PrioritaToDo.Alta)
                         .OrderBy(t => t.DataScadenza ?? DateTime.MaxValue)
                         .ThenByDescending(t => t.DataCreazione)
                         .Take(Math.Max(0, 5 - ToDoUrgenti.Count)))
                ToDoUrgenti.Add(CreaVoce(t));

            // Prossime scadenze: entro 7 giorni, escluse le voci già in "urgenti".
            var giaMostrati = ToDoUrgenti.Select(v => v.Id).ToHashSet();
            var oggi = DateTime.Today;
            var limite = oggi.AddDays(7);

            foreach (var t in aperti
                         .Where(t => t.DataScadenza.HasValue
                             && t.DataScadenza.Value.Date >= oggi
                             && t.DataScadenza.Value.Date <= limite
                             && !giaMostrati.Contains(t.Id))
                         .OrderBy(t => t.DataScadenza)
                         .ThenByDescending(t => t.Priorita)
                         .Take(6))
                ProssimeScadenze.Add(CreaVoce(t));
        }

        private VoceHomeToDo CreaVoce(ToDo t) => new()
        {
            Id = t.Id,
            Titolo = t.Titolo,
            ScadenzaTesto = EtichettaScadenza(t.DataScadenza, t.IsScaduto),
            Cliente = !string.IsNullOrEmpty(t.ClienteNome)
                ? t.ClienteNome
                : (!string.IsNullOrEmpty(t.ReferenteNome) ? t.ReferenteNome : "—"),
            ClienteId = t.ClienteId,
            Priorita = t.Priorita,
            IsScaduto = t.IsScaduto
        };

        private static string EtichettaScadenza(DateTime? data, bool scaduto)
        {
            if (!data.HasValue) return "Senza scadenza";

            var d = data.Value.Date;
            if (d == DateTime.Today) return "Scade oggi";
            if (d == DateTime.Today.AddDays(1)) return "Scade domani";
            return scaduto ? $"Scaduto il {d:dd/MM/yyyy}" : $"Per il {d:dd/MM/yyyy}";
        }

        private void CaricaPromemoria()
        {
            using var db = new ClabDbContext();
            var elenco = db.Promemoria.AsNoTracking().ToList();

            PromemoriaTotali = elenco.Count;
            PromemoriaAltaPriorita = elenco.Count(p => p.Priorita == PrioritaPromemoria.Alta);

            foreach (var p in elenco
                         .Where(p => p.Priorita == PrioritaPromemoria.Alta)
                         .OrderByDescending(p => p.DataCreazione)
                         .Take(3))
            {
                string? desc = string.IsNullOrWhiteSpace(p.Descrizione) ? null : p.Descrizione.Trim();
                if (desc != null && desc.Length > 90)
                    desc = desc[..87] + "…";

                PromemoriaInEvidenza.Add(new VoceHomePromemoria
                {
                    Titolo = p.Titolo,
                    Descrizione = desc ?? string.Empty,
                    Priorita = p.Priorita,
                    HaDescrizione = !string.IsNullOrEmpty(desc)
                });
            }
        }

        /// <summary>
        /// Situazione adempimenti dell'anno corrente: conteggi globali
        /// (completati/in corso/in ritardo) e dettaglio dei ritardi per cliente.
        /// Semantica invariata (TestoLibero escluso, "Futuro" fuori dai conteggi,
        /// stato calcolato con il servizio condiviso CLab.Services).
        /// </summary>
        private static (Dictionary<int, int> ritardoPerCliente, int completati, int inCorso, int inRitardo) CalcolaAdempimenti()
        {
            using var db = new ClabDbContext();
            int anno = DateTime.Now.Year;

            var attivitaCatalogo = db.Attivita.AsNoTracking().ToDictionary(a => a.Id, a => a);
            var assegnazioni = db.ClientiAttivita.AsNoTracking().ToList();
            var compilazioni = db.Compilazioni.AsNoTracking().Where(c => c.Anno == anno).ToList();

            var ritardoPerCliente = new Dictionary<int, int>();
            int comp = 0, corso = 0, rit = 0;

            foreach (var assegnazione in assegnazioni)
            {
                if (!attivitaCatalogo.TryGetValue(assegnazione.AttivitaId, out var attivita)) continue;
                if (attivita.TipoCampo == TipoCampoAttivita.TestoLibero) continue;

                int numeroPeriodi = attivita.Periodicita switch
                {
                    Periodicita.Mensile => 12,
                    Periodicita.Trimestrale => 4,
                    _ => 1
                };

                for (int periodo = 1; periodo <= numeroPeriodi; periodo++)
                {
                    var singola = compilazioni.FirstOrDefault(c =>
                        c.ClienteId == assegnazione.ClienteId && c.AttivitaId == assegnazione.AttivitaId && c.Periodo == periodo);

                    bool compilato = singola != null && attivita.TipoCampo switch
                    {
                        TipoCampoAttivita.SiNo => singola.ValoreBooleano == true,
                        TipoCampoAttivita.Numero => singola.ValoreNumero.HasValue,
                        TipoCampoAttivita.Tendina => !string.IsNullOrWhiteSpace(singola.ValoreTesto),
                        _ => false
                    };

                    switch (CalcoloStatoAdempimenti.Calcola(attivita.Periodicita, anno, periodo, compilato))
                    {
                        case CalcoloStatoAdempimenti.Compilato: comp++; break;
                        case CalcoloStatoAdempimenti.InCorso: corso++; break;
                        case CalcoloStatoAdempimenti.Ritardo:
                            rit++;
                            ritardoPerCliente[assegnazione.ClienteId] = ritardoPerCliente.GetValueOrDefault(assegnazione.ClienteId) + 1;
                            break;
                    }
                }
            }

            return (ritardoPerCliente, comp, corso, rit);
        }

        private static Dictionary<int, string> CaricaNomiClienti()
        {
            using var db = new ClabDbContext();
            return db.Clienti.AsNoTracking().ToDictionary(c => c.Id, c => c.RagioneSociale);
        }

        private void CaricaFatture()
        {
            using var db = new ClabDbContext();
            int anno = DateTime.Now.Year;

            var fattureAnno = db.Fatture.AsNoTracking().Where(f => f.DataEmissione.Year == anno).ToList();
            FattureAnnoCorrente = fattureAnno.Count;

            var valide = fattureAnno.Where(f => !f.Annullata).ToList();
            var daIncassare = valide.Where(f => !f.DataPagamento.HasValue).ToList();

            FattureDaIncassareNumero = daIncassare.Count;
            FattureDaIncassareTesto = $"€ {daIncassare.Sum(f => f.Importo):N0}";

            // Stessa semantica del conteggio "scadute" già esistente in Home.
            FattureScaduteNumero = valide.Count(f =>
                f.DataScadenza.HasValue
                && f.DataScadenza.Value.Date < DateTime.Now.Date
                && !f.DataPagamento.HasValue);
        }

        private static int ContaClientiAttivi()
        {
            using var db = new ClabDbContext();
            return db.Clienti.AsNoTracking().Count(c => c.Stato == StatoCliente.Attivo);
        }
    }

    /// <summary>Cliente con adempimenti in ritardo (riga della sezione "Richiede attenzione").</summary>
    public class VoceAdempimentiRitardo
    {
        public int ClienteId { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public int NumeroInRitardo { get; set; }
        public string Testo => NumeroInRitardo == 1
            ? $"{RagioneSociale} — 1 in ritardo"
            : $"{RagioneSociale} — {NumeroInRitardo} in ritardo";
    }

    public class VoceHomeToDo
    {
        /// <summary>Id del ToDo: esposto per future aperture puntuali (nessun uso DB).</summary>
        public int Id { get; set; }
        public string Titolo { get; set; } = string.Empty;
        public string ScadenzaTesto { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public bool HaCliente => ClienteId.HasValue;
        public PrioritaToDo Priorita { get; set; }
        public bool IsScaduto { get; set; }
        public bool IsAltaPriorita => Priorita == PrioritaToDo.Alta;
    }

    public class VoceHomePromemoria
    {
        public string Titolo { get; set; } = string.Empty;
        public string Descrizione { get; set; } = string.Empty;
        public PrioritaPromemoria Priorita { get; set; }
        public bool HaDescrizione { get; set; }
    }
}
