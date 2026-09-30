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

        // --- 2/3. ToDo in evidenza: box unico (prima "ToDo urgenti" e
        // "Prossime scadenze" raccontavano lo stesso modulo separatamente).
        // Lista: scaduti prima, poi alta priorità/in scadenza entro 7 giorni.
        // I ToDo senza scadenza non sono urgenti quindi non entrano in lista,
        // ma restano visibili come nota di conteggio (ToDoSenzaScadenzaTesto).

        public ObservableCollection<VoceHomeToDo> ToDoInEvidenza { get; } = new();
        public bool HaToDoInEvidenza => ToDoInEvidenza.Count > 0;

        public int ToDoInScadenza { get; private set; }
        public int ToDoSenzaScadenza { get; private set; }
        public bool HaToDoSenzaScadenza => ToDoSenzaScadenza > 0;
        public string ToDoSenzaScadenzaTesto => ToDoSenzaScadenza == 1
            ? "+1 ToDo senza scadenza da gestire"
            : $"+{ToDoSenzaScadenza} ToDo senza scadenza da gestire";

        public bool HaToDoCaption => ToDoScaduti > 0 || ToDoInScadenza > 0;
        public string ToDoCaptionTesto
        {
            get
            {
                var parti = new List<string>();
                if (ToDoScaduti > 0) parti.Add(ToDoScaduti == 1 ? "1 scaduto" : $"{ToDoScaduti} scaduti");
                if (ToDoInScadenza > 0) parti.Add(ToDoInScadenza == 1 ? "1 in scadenza" : $"{ToDoInScadenza} in scadenza");
                return parti.Count == 0 ? string.Empty : "· " + string.Join(" · ", parti);
            }
        }

        // --- 4. Promemoria in evidenza (solo alta priorità) ---

        public ObservableCollection<VoceHomePromemoria> PromemoriaInEvidenza { get; } = new();
        public bool HaPromemoriaInEvidenza => PromemoriaInEvidenza.Count > 0;

        // --- 4B. Studi da incassare (FASE 6): studi con maggiore importo scaduto ---

        public ObservableCollection<VoceHomeStudio> StudiDaIncassare { get; } = new();
        public bool HaStudiDaIncassare => StudiDaIncassare.Count > 0;

        // --- 5. Fatture (anno corrente) ---
        // Il KPI in alto mostra il totale ancora da incassare (invariato).
        // La card sotto NON ripete quel numero: mostra la composizione del
        // fatturato dell'anno in tre categorie mutuamente esclusive, con lo
        // stesso pattern visivo della card Adempimenti (barra segmentata +
        // tre contatori). Le tre categorie sommano sempre al totale anno.

        public string AnnoCorrente => DateTime.Now.Year.ToString();
        public int FattureDaIncassareNumero { get; private set; }
        public string FattureDaIncassareTesto { get; private set; } = "€ 0";
        public bool HaFattureDaIncassare => FattureDaIncassareNumero > 0;

        public bool HaFattureAnno => FattureAnnoCorrente > 0;
        private decimal FattureTotaleAnnoImporto { get; set; }

        public int FattureIncassatoNumero { get; private set; }
        private decimal FattureIncassatoImporto { get; set; }
        public string FattureIncassatoTesto => $"€ {FattureIncassatoImporto:N0}";

        public int FattureNonScaduteNumero { get; private set; }
        private decimal FattureNonScaduteImporto { get; set; }
        public string FattureNonScaduteTesto => $"€ {FattureNonScaduteImporto:N0}";

        private decimal FattureScaduteImporto { get; set; }
        public string FattureScaduteImportoTesto => $"€ {FattureScaduteImporto:N0}";

        public double PctFattureIncassato { get; private set; }
        public double PctFattureNonScadute { get; private set; }
        public double PctFattureScadute { get; private set; }

        // --- 6. KPI secondari ---

        public int ClientiAttivi { get; private set; }
        public int FattureAnnoCorrente { get; private set; }
        public int ToDoAperti { get; private set; }
        public int PromemoriaTotali { get; private set; }

        // PASS 2 — contatori di contesto "N di M": dicono quanto esiste oltre
        // ciò che la lista mostra, senza mostrare tutto. Derivati dalle stesse
        // query (conteggio pre-take), nessuna nuova interrogazione.
        public int TotaleClientiInRitardo { get; private set; }
        public int TotaleStudiDaIncassare { get; private set; }
        public int TotaleStudi { get; private set; }

        // --- Clienti: composizione per stato (Attivo/StandBy/Cessato).
        // Riusa Cliente.Stato, già esistente: nessuna logica nuova, solo un
        // conteggio raggruppato al posto del conteggio secco precedente. ---

        public int ClientiStandBy { get; private set; }
        public int ClientiCessati { get; private set; }
        public bool HaClienti => (ClientiAttivi + ClientiStandBy + ClientiCessati) > 0;
        public double PctClientiAttivi { get; private set; }
        public double PctClientiStandBy { get; private set; }
        public double PctClientiCessati { get; private set; }

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
        public ICommand ApriStudioCommand { get; }
        public ICommand ApriStudiCommand { get; }

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
            ApriStudioCommand = new RelayCommand<VoceHomeStudio>(v =>
            {
                if (v != null)
                    _navigatore?.ApriStudi(v.Id);
            });
            ApriStudiCommand = new RelayCommand(() => _navigatore?.ApriStudi());

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
            TotaleClientiInRitardo = ritardoPerCliente.Count;
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
            CaricaStudi();
            CaricaClienti();

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

            var oggi = DateTime.Today;
            var limite = oggi.AddDays(7);

            // Contatori di contesto per la caption e per la nota "senza scadenza"
            // (visibili anche se non finiscono nella lista in evidenza).
            ToDoInScadenza = aperti.Count(t => !t.IsScaduto
                && t.DataScadenza.HasValue
                && t.DataScadenza.Value.Date >= oggi
                && t.DataScadenza.Value.Date <= limite);
            ToDoSenzaScadenza = aperti.Count(t => !t.DataScadenza.HasValue);

            // 1. Prendo gli scaduti (max 3)
            var scaduti = aperti
                .Where(t => t.IsScaduto)
                .OrderBy(t => t.DataScadenza ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priorita)
                .Take(3)
                .ToList();

            foreach (var t in scaduti)
                ToDoInEvidenza.Add(CreaVoce(t));

            // 2. Se mancano elementi, prendo i non scaduti rilevanti
            int mancanti = 3 - ToDoInEvidenza.Count;

            if (mancanti > 0)
            {
                var rilevanti = aperti
                    .Where(t => !t.IsScaduto)
                    .OrderByDescending(t => t.Priorita)                 // prima la priorità
                    .ThenBy(t => t.DataScadenza ?? DateTime.MaxValue)   // poi la scadenza (se c’è)
                    .Take(mancanti)
                    .ToList();

                foreach (var t in rilevanti)
                    ToDoInEvidenza.Add(CreaVoce(t));
            }

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
                         .OrderByDescending(p => p.Priorita).ThenByDescending(p => p.DataCreazione)
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
            var statiClienti = db.Clienti.AsNoTracking().ToDictionary(c => c.Id, c => c.Stato);
            var compilazioni = db.Compilazioni.AsNoTracking().Where(c => c.Anno == anno).ToList();

            var ritardoPerCliente = new Dictionary<int, int>();
            int comp = 0, corso = 0, rit = 0;

            foreach (var assegnazione in assegnazioni)
            {
                if (!statiClienti.TryGetValue(assegnazione.ClienteId, out var stato) || stato != StatoCliente.Attivo) continue;
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

            var tutte = db.Fatture.AsNoTracking().ToList();
            FattureAnnoCorrente = tutte.Count(f => FattureViewModel.AnnoFattura(f) == anno);

            var valide = tutte.Where(f => !f.Annullata && FattureViewModel.AnnoFattura(f) == anno).ToList();
            var daIncassare = valide.Where(f => !f.DataPagamento.HasValue).ToList();

            FattureDaIncassareNumero = daIncassare.Count;
            FattureDaIncassareTesto = $"€ {daIncassare.Sum(f => f.Importo):N0}";

            FattureScaduteNumero = valide.Count(f =>
                f.DataScadenza.HasValue
                && f.DataScadenza.Value.Date < DateTime.Now.Date
                && !f.DataPagamento.HasValue);

            var oggi = DateTime.Now.Date;
            var incassate = valide.Where(f => f.DataPagamento.HasValue).ToList();
            var scadute = daIncassare.Where(f => f.DataScadenza.HasValue && f.DataScadenza.Value.Date < oggi).ToList();
            var nonScadute = daIncassare.Where(f => !(f.DataScadenza.HasValue && f.DataScadenza.Value.Date < oggi)).ToList();

            FattureTotaleAnnoImporto = valide.Sum(f => f.Importo);

            FattureIncassatoNumero = incassate.Count;
            FattureIncassatoImporto = incassate.Sum(f => f.Importo);

            FattureNonScaduteNumero = nonScadute.Count;
            FattureNonScaduteImporto = nonScadute.Sum(f => f.Importo);

            FattureScaduteImporto = scadute.Sum(f => f.Importo);

            if (FattureTotaleAnnoImporto > 0)
            {
                double totale = (double)FattureTotaleAnnoImporto;
                PctFattureIncassato = (double)FattureIncassatoImporto / totale;
                PctFattureNonScadute = (double)FattureNonScaduteImporto / totale;
                PctFattureScadute = (double)FattureScaduteImporto / totale;
            }
        }

        /// <summary>
        /// PASS 2 — studi con crediti aperti (top 4 per importo da incassare),
        /// per il blocco "Studi da incassare". Regola (stessa semantica di
        /// FattureViewModel/StudiViewModel):
        ///   - da incassare = fatture non annullate con DataPagamento == null;
        ///   - quota scaduta = parte di quella con DataScadenza passata;
        ///   - fatture pagate non entrano in nessun totale.
        /// </summary>
        private void CaricaStudi()
        {
            using var db = new ClabDbContext();
            var oggi = DateTime.Now.Date;

            var fatture = db.Fatture.AsNoTracking()
                .Where(f => !f.Annullata && f.DataPagamento == null)
                .Select(f => new { f.ReferenteId, f.Importo, f.DataScadenza })
                .ToList();

            TotaleStudi = db.Referenti.AsNoTracking().Count(x => x.Attivo);

            var righe = db.Referenti.AsNoTracking()
                .Select(r => new { r.Id, r.Nome })
                .ToList()
                .Select(r => new
                {
                    r.Id,
                    r.Nome,
                    DaIncassare = fatture.Where(f => f.ReferenteId == r.Id).Sum(f => f.Importo),
                    Scaduto = fatture.Where(f => f.ReferenteId == r.Id
                                                 && f.DataScadenza != null
                                                 && f.DataScadenza.Value.Date < oggi).Sum(f => f.Importo)
                })
                .ToList();

            TotaleStudiDaIncassare = righe.Count(x => x.DaIncassare > 0);

            foreach (var r in righe.Where(x => x.DaIncassare > 0)
                                    .OrderByDescending(x => x.DaIncassare)
                                    .ThenByDescending(x => x.Scaduto)
                                    .Take(4))
                StudiDaIncassare.Add(new VoceHomeStudio
                {
                    Id = r.Id,
                    Nome = r.Nome,
                    Scaduto = r.Scaduto,
                    DaIncassare = r.DaIncassare
                });
        }

        /// <summary>
        /// Composizione clienti per stato (Attivo/StandBy/Cessato), per la card
        /// "Clienti" accanto a "Situazione studi". Riusa Cliente.Stato, già
        /// esistente: un solo conteggio raggruppato, nessuna query aggiuntiva
        /// rispetto al precedente conteggio secco dei soli attivi.
        /// </summary>
        private void CaricaClienti()
        {
            using var db = new ClabDbContext();
            var conteggi = db.Clienti.AsNoTracking()
                .GroupBy(c => c.Stato)
                .Select(g => new { Stato = g.Key, Numero = g.Count() })
                .ToList();

            ClientiAttivi = conteggi.FirstOrDefault(x => x.Stato == StatoCliente.Attivo)?.Numero ?? 0;
            ClientiStandBy = conteggi.FirstOrDefault(x => x.Stato == StatoCliente.StandBy)?.Numero ?? 0;
            ClientiCessati = conteggi.FirstOrDefault(x => x.Stato == StatoCliente.Cessato)?.Numero ?? 0;

            int totale = ClientiAttivi + ClientiStandBy + ClientiCessati;
            if (totale > 0)
            {
                double t = totale;
                PctClientiAttivi = ClientiAttivi / t;
                PctClientiStandBy = ClientiStandBy / t;
                PctClientiCessati = ClientiCessati / t;
            }
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

    /// <summary>Studio con crediti aperti per il blocco "Studi da incassare" della Home.</summary>
    public class VoceHomeStudio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal Scaduto { get; set; }
        public decimal DaIncassare { get; set; }

        public bool HaScaduto => Scaduto > 0;
        public string ScadutoTesto => $"\u20AC {Scaduto:N0} scaduti";
        public string DaIncassareTesto => $"\u20AC {DaIncassare:N0} da incassare";
    }
}