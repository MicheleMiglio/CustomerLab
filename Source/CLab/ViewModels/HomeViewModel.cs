using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public string Saluto { get; }
        public string DataOggiTesto { get; }
        public string SintesiGiornata { get; private set; } = string.Empty;

        public string ToDoSezioneSottoTitolo { get; private set; } = string.Empty;
        public string PromemoriaSezioneSottoTitolo { get; private set; } = string.Empty;

        public ObservableCollection<VoceHomeToDo> ProssimiToDo { get; } = new();
        public ObservableCollection<VoceHomePromemoria> PromemoriaInEvidenza { get; } = new();

        public ICommand ApriToDoCommand { get; }
        public ICommand ApriPromemoriaCommand { get; }

        public HomeViewModel(Action? apriToDo = null, Action? apriPromemoria = null)
        {
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

            ApriToDoCommand = new RelayCommand(() => apriToDo?.Invoke());
            ApriPromemoriaCommand = new RelayCommand(() => apriPromemoria?.Invoke());

            int todoScaduti = CaricaToDo();
            int promemoriaAlta = CaricaPromemoria();
            int adempimentiInRitardo = ContaAdempimentiInRitardo();
            int fattureScadute = ContaFattureScadute();

            var parti = new List<string>();
            if (todoScaduti > 0) parti.Add($"{todoScaduti} ToDo scaduti");
            if (adempimentiInRitardo > 0) parti.Add($"{adempimentiInRitardo} adempimenti in ritardo");
            if (fattureScadute > 0) parti.Add($"{fattureScadute} fatture scadute");
            if (promemoriaAlta > 0) parti.Add($"{promemoriaAlta} promemoria ad alta priorità");

            SintesiGiornata = parti.Count == 0
                ? "Niente in ritardo per oggi."
                : string.Join(" · ", parti) + ".";
        }

        private int CaricaToDo()
        {
            using var db = new ClabDbContext();
            var elenco = db.ToDo.AsNoTracking().ToList();

            var nomiClienti = db.Clienti.AsNoTracking().ToDictionary(c => c.Id, c => c.RagioneSociale);
            var nomiReferenti = db.Referenti.AsNoTracking().ToDictionary(r => r.Id, r => r.Nome);

            var aperti = elenco.Where(t => !t.Completato).ToList();
            int scaduti = aperti.Count(t => t.IsScaduto);

            ToDoSezioneSottoTitolo = aperti.Count == 0
                ? "Nessun ToDo aperto"
                : scaduti > 0
                    ? $"{aperti.Count} aperti · {scaduti} scaduti"
                    : $"{aperti.Count} aperti";

            foreach (var t in aperti
                         .OrderBy(t => t.DataScadenza.HasValue ? 0 : 1)
                         .ThenBy(t => t.DataScadenza ?? DateTime.MaxValue)
                         .ThenByDescending(t => t.Priorita)
                         .Take(8))
            {
                string cliente = t.ClienteId.HasValue && nomiClienti.TryGetValue(t.ClienteId.Value, out var cn)
                    ? cn
                    : (t.ClienteNomeStorico
                        ?? (t.ReferenteId.HasValue && nomiReferenti.TryGetValue(t.ReferenteId.Value, out var rn) ? rn : "—"));

                ProssimiToDo.Add(new VoceHomeToDo
                {
                    Titolo = t.Titolo,
                    ScadenzaTesto = t.DataScadenza.HasValue ? t.DataScadenza.Value.ToString("dd/MM/yyyy") : "Senza scadenza",
                    Cliente = cliente,
                    Priorita = t.Priorita,
                    IsScaduto = t.IsScaduto
                });
            }

            return scaduti;
        }

        private int CaricaPromemoria()
        {
            using var db = new ClabDbContext();
            var elenco = db.Promemoria.AsNoTracking().ToList();
            int alta = elenco.Count(p => p.Priorita == PrioritaPromemoria.Alta);

            PromemoriaSezioneSottoTitolo = elenco.Count == 0
                ? "Nessun promemoria"
                : alta > 0
                    ? $"{elenco.Count} aperti · {alta} alta priorità"
                    : $"{elenco.Count} aperti";

            foreach (var p in elenco
                         .OrderByDescending(p => p.Priorita)
                         .ThenByDescending(p => p.DataCreazione)
                         .Take(8))
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

            return alta;
        }

        private static int ContaAdempimentiInRitardo()
        {
            using var db = new ClabDbContext();
            int anno = DateTime.Now.Year;

            var attivitaCatalogo = db.Attivita.AsNoTracking().ToDictionary(a => a.Id, a => a);
            var assegnazioni = db.ClientiAttivita.AsNoTracking().ToList();
            var compilazioni = db.Compilazioni.AsNoTracking().Where(c => c.Anno == anno).ToList();

            int inRitardo = 0;
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
                    var comp = compilazioni.FirstOrDefault(c =>
                        c.ClienteId == assegnazione.ClienteId && c.AttivitaId == assegnazione.AttivitaId && c.Periodo == periodo);

                    bool compilato = comp != null && attivita.TipoCampo switch
                    {
                        TipoCampoAttivita.SiNo => comp.ValoreBooleano == true,
                        TipoCampoAttivita.Numero => comp.ValoreNumero.HasValue,
                        TipoCampoAttivita.Tendina => !string.IsNullOrWhiteSpace(comp.ValoreTesto),
                        _ => false
                    };

                    if (CalcolaStato(attivita.Periodicita, anno, periodo, compilato) == "Ritardo")
                        inRitardo++;
                }
            }

            return inRitardo;
        }

        private static string CalcolaStato(Periodicita periodicita, int anno, int periodo, bool compilato)
        {
            if (compilato) return "Compilato";

            var oggi = DateTime.Now;

            if (periodicita == Periodicita.Annuale)
                return anno < oggi.Year ? "Ritardo" : "InCorso";

            if (anno < oggi.Year) return "Ritardo";
            if (anno > oggi.Year) return "Futuro";

            int correnteIndice = periodicita == Periodicita.Mensile ? oggi.Month : ((oggi.Month - 1) / 3) + 1;
            if (periodo < correnteIndice) return "Ritardo";
            if (periodo == correnteIndice) return "InCorso";
            return "Futuro";
        }

        private static int ContaFattureScadute()
        {
            using var db = new ClabDbContext();
            int anno = DateTime.Now.Year;
            return db.Fatture.AsNoTracking().Count(f =>
                !f.Annullata
                && f.DataEmissione.Year == anno
                && f.DataScadenza.HasValue
                && f.DataScadenza.Value.Date < DateTime.Now.Date
                && !f.DataPagamento.HasValue);
        }
    }

    public class VoceHomeToDo
    {
        public string Titolo { get; set; } = string.Empty;
        public string ScadenzaTesto { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public PrioritaToDo Priorita { get; set; }
        public bool IsScaduto { get; set; }
    }

    public class VoceHomePromemoria
    {
        public string Titolo { get; set; } = string.Empty;
        public string Descrizione { get; set; } = string.Empty;
        public PrioritaPromemoria Priorita { get; set; }
        public bool HaDescrizione { get; set; }
    }
}
