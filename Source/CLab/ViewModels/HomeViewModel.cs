using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace CLab.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public string Saluto { get; }
        public string DataOggiTesto { get; }

        // --- Clienti ---
        private int _totaleClienti;
        public int TotaleClienti { get => _totaleClienti; private set { _totaleClienti = value; OnPropertyChanged(); } }

        private int _clientiAttivi;
        public int ClientiAttivi { get => _clientiAttivi; private set { _clientiAttivi = value; OnPropertyChanged(); } }

        private GraficoTorta _tortaClienti = new();
        public GraficoTorta TortaClienti { get => _tortaClienti; private set { _tortaClienti = value; OnPropertyChanged(); } }

        // --- Scadenzario, aggregato su tutti i clienti ---
        private int _adempimentiInRitardo;
        public int AdempimentiInRitardo { get => _adempimentiInRitardo; private set { _adempimentiInRitardo = value; OnPropertyChanged(); } }

        private GraficoTorta _tortaAdempimenti = new();
        public GraficoTorta TortaAdempimenti { get => _tortaAdempimenti; private set { _tortaAdempimenti = value; OnPropertyChanged(); } }

        public ObservableCollection<ClienteDaControllare> ClientiDaControllare { get; set; } = new();

        // --- Fatture, anno corrente ---
        private string _fatturatoAnnoTesto = "€ 0";
        public string FatturatoAnnoTesto { get => _fatturatoAnnoTesto; private set { _fatturatoAnnoTesto = value; OnPropertyChanged(); } }

        private string _incassatoAnnoTesto = "€ 0";
        public string IncassatoAnnoTesto { get => _incassatoAnnoTesto; private set { _incassatoAnnoTesto = value; OnPropertyChanged(); } }

        private string _daIncassareAnnoTesto = "€ 0";
        public string DaIncassareAnnoTesto { get => _daIncassareAnnoTesto; private set { _daIncassareAnnoTesto = value; OnPropertyChanged(); } }

        private int _fattureScaduteAnno;
        public int FattureScaduteAnno { get => _fattureScaduteAnno; private set { _fattureScaduteAnno = value; OnPropertyChanged(); } }

        public HomeViewModel()
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
            DataOggiTesto = char.ToUpper(testoData[0]) + testoData.Substring(1);

            CaricaClienti();
            CaricaAdempimenti();
            CaricaFatture();
            CaricaReferenti();
        }

        private void CaricaClienti()
        {
            using var db = new ClabDbContext();
            var clienti = db.Clienti.AsNoTracking().ToList();

            TotaleClienti = clienti.Count;

            int attivi = clienti.Count(c => c.Stato == StatoCliente.Attivo);
            int standBy = clienti.Count(c => c.Stato == StatoCliente.StandBy);
            int cessati = clienti.Count(c => c.Stato == StatoCliente.Cessato);

            ClientiAttivi = attivi;
            TortaClienti = CostruisciTorta(attivi, standBy, cessati);
        }

        private void CaricaAdempimenti()
        {
            using var db = new ClabDbContext();
            int anno = DateTime.Now.Year;

            var clienti = db.Clienti.AsNoTracking().ToDictionary(c => c.Id, c => c.RagioneSociale);
            var attivitaCatalogo = db.Attivita.AsNoTracking().ToDictionary(a => a.Id, a => a);
            var assegnazioni = db.ClientiAttivita.AsNoTracking().ToList();
            var compilazioni = db.Compilazioni.AsNoTracking().Where(c => c.Anno == anno).ToList();

            int compilate = 0, inCorso = 0, inRitardo = 0;
            var ritardoPerCliente = new Dictionary<int, int>();

            foreach (var assegnazione in assegnazioni)
            {
                if (!attivitaCatalogo.TryGetValue(assegnazione.AttivitaId, out var attivita)) continue;
                if (attivita.TipoCampo == TipoCampoAttivita.TestoLibero) continue; // opzionale, escluso come in Scadenzario

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

                    string stato = CalcolaStato(attivita.Periodicita, anno, periodo, compilato);

                    switch (stato)
                    {
                        case "Compilato": compilate++; break;
                        case "InCorso": inCorso++; break;
                        case "Ritardo":
                            inRitardo++;
                            ritardoPerCliente[assegnazione.ClienteId] = ritardoPerCliente.GetValueOrDefault(assegnazione.ClienteId) + 1;
                            break;
                    }
                }
            }

            AdempimentiInRitardo = inRitardo;
            TortaAdempimenti = CostruisciTorta(compilate, inCorso, inRitardo);

            ClientiDaControllare.Clear();
            foreach (var kv in ritardoPerCliente.OrderByDescending(k => k.Value).Take(6))
            {
                ClientiDaControllare.Add(new ClienteDaControllare
                {
                    RagioneSociale = clienti.TryGetValue(kv.Key, out var nome) ? nome : "—",
                    NumeroInRitardo = kv.Value
                });
            }
        }

        // Stessa identica logica del calcolo stato già usata in Scadenzario.
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

        private void CaricaFatture()
        {
            using var db = new ClabDbContext();
            int anno = DateTime.Now.Year;

            var fatture = db.Fatture.AsNoTracking()
                .Where(f => !f.Annullata && f.DataEmissione.Year == anno)
                .ToList();

            decimal totale = fatture.Sum(f => f.Importo);
            decimal incassato = fatture.Where(f => f.DataPagamento.HasValue).Sum(f => f.Importo);

            FatturatoAnnoTesto = $"€ {totale:N0}";
            IncassatoAnnoTesto = $"€ {incassato:N0}";
            DaIncassareAnnoTesto = $"€ {(totale - incassato):N0}";
            FattureScaduteAnno = fatture.Count(f =>
                f.DataScadenza.HasValue && f.DataScadenza.Value.Date < DateTime.Now.Date && !f.DataPagamento.HasValue);
        }

        // Riusa GraficoTorta (già definita in ScadenzarioViewModel.cs, stesso namespace):
        // qui è solo una torta generica a 3 fette, i nomi delle proprietà non sono legati al significato.
        private static GraficoTorta CostruisciTorta(int fetta1, int fetta2, int fetta3)
        {
            var torta = new GraficoTorta { Completate = fetta1, InCorso = fetta2, InRitardo = fetta3 };
            int totale = fetta1 + fetta2 + fetta3;

            if (totale == 0)
            {
                torta.Vuoto = true;
                return torta;
            }

            double p1 = fetta1 / (double)totale;
            double p2 = p1 + fetta2 / (double)totale;

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

            var figura = new System.Windows.Media.PathFigure { StartPoint = new System.Windows.Point(cx, cy), IsClosed = true };
            figura.Segments.Add(new LineSegment(p0, true));
            figura.Segments.Add(new ArcSegment(p1, new System.Windows.Size(raggio, raggio), 0, grandeArco, SweepDirection.Clockwise, true));

            var geometria = new PathGeometry();
            geometria.Figures.Add(figura);
            return geometria;
        }

        private static System.Windows.Point PuntoSuCirconferenza(double cx, double cy, double r, double angoloGradi)
        {
            double rad = angoloGradi * Math.PI / 180.0;
            return new System.Windows.Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
        }

        private int _referentiAttiviCount;
        public int ReferentiAttiviCount { get => _referentiAttiviCount; private set { _referentiAttiviCount = value; OnPropertyChanged(); } }

        public ObservableCollection<ClientiPerReferente> TopReferenti { get; set; } = new();

        private void CaricaReferenti()
        {
            using var db = new ClabDbContext();
            var referentiAttivi = db.Referenti.AsNoTracking().Where(r => r.Attivo).ToList();
            ReferentiAttiviCount = referentiAttivi.Count;

            var clienti = db.Clienti.AsNoTracking().ToList();

            TopReferenti.Clear();
            foreach (var r in referentiAttivi
                         .Select(r => new ClientiPerReferente { Nome = r.Nome, NumeroClienti = clienti.Count(c => c.ReferenteId == r.Id) })
                         .Where(x => x.NumeroClienti > 0)
                         .OrderByDescending(x => x.NumeroClienti)
                         .Take(6))
            {
                TopReferenti.Add(r);
            }
        }
    }

    public class ClienteDaControllare
    {
        public string RagioneSociale { get; set; } = string.Empty;
        public int NumeroInRitardo { get; set; }
    }

    public class ClientiPerReferente
    {
        public string Nome { get; set; } = string.Empty;
        public int NumeroClienti { get; set; }
    }
}