using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CLab.ViewModels
{
    /// <summary>
    /// FASE 5 CLab 2.0: modulo Studi. Studio NON è una nuova entità: è il
    /// Referente (asse economico). Il modulo aggrega lato SQL (query EF Core
    /// traducibili) il numero di clienti collegati (Cliente.ReferenteId) e i
    /// dati economici delle fatture (Fattura.ReferenteId), senza caricare le
    /// fatture in memoria. Nessuna modifica a Models/Data: solo lettura e
    /// navigazione verso i moduli esistenti.
    /// </summary>
    public class StudiViewModel : ViewModelBase
    {
        private readonly INavigatore? _navigatore;

        public ObservableCollection<VoceStudio> Studi { get; } = new();
        private List<VoceStudio> _studiCompleti = new();

        private string _filtroTesto = string.Empty;
        public string FiltroTesto
        {
            get => _filtroTesto;
            set { _filtroTesto = value; OnPropertyChanged(); ApplicaFiltro(); }
        }

        public string ContatoreTesto { get; private set; } = string.Empty;

        public bool NessunoStudio => _studiCompleti.Count == 0;
        public string EmptyStateTesto => NessunoStudio
            ? "Nessuno studio presente."
            : "Nessuno studio corrisponde alla ricerca.";

        public string EmptyStateSuggerimento => NessunoStudio
            ? "Gli studi corrispondono ai referenti: creali dal modulo Clienti con \"Gestione referenti\"."
            : string.Empty;

        public ICommand ApriDettaglioCommand { get; }
        public ICommand ChiudiDettaglioCommand { get; }
        public ICommand ApriFattureCommand { get; }

        // --- Pannello dettaglio studio (sola lettura, SidePanelControl) ---

        private bool _dettaglioAperto;
        public bool DettaglioAperto { get => _dettaglioAperto; set { _dettaglioAperto = value; OnPropertyChanged(); } }

        public string DettaglioTitolo { get; private set; } = string.Empty;
        public string DettaglioSottotitolo { get; private set; } = string.Empty;

        public string KpiFatturato { get; private set; } = "€ 0";
        public string KpiIncassato { get; private set; } = "€ 0";
        public string KpiDaIncassare { get; private set; } = "€ 0";
        public string KpiScaduto { get; private set; } = "€ 0";
        public bool HaScaduto { get; private set; }

        public ObservableCollection<VoceFatturaStudio> FattureStudio { get; } = new();
        public bool HaFattureStudio => FattureStudio.Count > 0;
        public string EmptyFattureTesto => "Nessuna fattura intestata a questo studio.";

        public ObservableCollection<VoceClienteStudio> ClientiStudio { get; } = new();
        public bool HaClientiStudio => ClientiStudio.Count > 0;
        public string EmptyClientiTesto => "Nessun cliente collegato a questo studio.";

        public StudiViewModel(INavigatore? navigatore = null)
        {
            _navigatore = navigatore;

            ApriDettaglioCommand = new RelayCommand<VoceStudio>(ApriDettaglio);
            ChiudiDettaglioCommand = new RelayCommand(() => DettaglioAperto = false);
            ApriFattureCommand = new RelayCommand(() => _navigatore?.ApriFatture());

            Carica();
        }

        /// <summary>Apertura contestuale del dettaglio (es. dalla ricerca globale).</summary>
        public void ApriDettaglioPerReferente(int referenteId)
        {
            var studio = _studiCompleti.FirstOrDefault(s => s.Id == referenteId);
            if (studio != null)
                ApriDettaglio(studio);
        }

        private void Carica()
        {
            using var db = new ClabDbContext();
            var oggi = DateTime.Now.Date;

            // FIX Sum(decimal): il provider SQLite di EF Core non supporta
            // l'operatore aggregato 'Sum' su espressioni decimal (NotSupportedException).
            // Il filtraggio e la proiezione restano lato database (solo le colonne
            // necessarie delle fatture valide: nessun caricamento dell'intero
            // database e nessuna modifica a Models); le somme vengono poi calcolate
            // lato client in decimal, per preservare la precisione monetaria
            // (una somma lato SQL in double degraderebbe gli importi).
            var referenti = db.Referenti.AsNoTracking()
                .Select(r => new { r.Id, r.Nome, r.Attivo })
                .OrderBy(r => r.Nome)
                .ToList();

            var fatture = db.Fatture.AsNoTracking()
                .Where(f => !f.Annullata)
                .Select(f => new { f.ReferenteId, f.Importo, f.DataPagamento, f.DataScadenza })
                .ToList();

            var numeroClienti = db.Clienti.AsNoTracking()
                .Where(c => c.ReferenteId != null)
                .Select(c => new { c.Id, c.ReferenteId })
                .ToList()
                .GroupBy(c => c.ReferenteId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            _studiCompleti = referenti.Select(r =>
            {
                var mie = fatture.Where(f => f.ReferenteId == r.Id).ToList();

                return new VoceStudio
                {
                    Id = r.Id,
                    Nome = r.Nome,
                    Attivo = r.Attivo,
                    NumeroClienti = numeroClienti.GetValueOrDefault(r.Id),
                    Fatturato = mie.Sum(f => f.Importo),
                    DaIncassare = mie.Where(f => f.DataPagamento == null).Sum(f => f.Importo),
                    Scaduto = mie.Where(f => f.DataPagamento == null
                                             && f.DataScadenza != null
                                             && f.DataScadenza.Value.Date < oggi).Sum(f => f.Importo)
                };
            }).ToList();

            ApplicaFiltro();
        }

        private void ApplicaFiltro()
        {
            var q = FiltroTesto.Trim().ToLower();
            var lista = string.IsNullOrEmpty(q)
                ? _studiCompleti
                : _studiCompleti.Where(s => s.Nome.ToLower().Contains(q)).ToList();

            Studi.Clear();
            foreach (var s in lista)
                Studi.Add(s);

            ContatoreTesto = lista.Count == 1 ? "1 studio" : $"{lista.Count} studi";
            OnPropertyChanged(nameof(ContatoreTesto));
            OnPropertyChanged(nameof(NessunoStudio));
            OnPropertyChanged(nameof(EmptyStateTesto));
            OnPropertyChanged(nameof(EmptyStateSuggerimento));
        }

        private void ApriDettaglio(VoceStudio? studio)
        {
            if (studio == null) return;

            CaricaDettaglio(studio.Id);
            DettaglioAperto = true;
        }

        private void CaricaDettaglio(int referenteId)
        {
            using var db = new ClabDbContext();
            var oggi = DateTime.Now.Date;

            var referente = db.Referenti.AsNoTracking().First(r => r.Id == referenteId);
            DettaglioTitolo = referente.Nome;
            DettaglioSottotitolo = referente.Attivo ? "STUDIO" : "STUDIO DISATTIVATO";
            OnPropertyChanged(nameof(DettaglioTitolo));
            OnPropertyChanged(nameof(DettaglioSottotitolo));

            var fatture = db.Fatture.AsNoTracking()
                .Where(f => f.ReferenteId == referenteId)
                .ToList();
            var valide = fatture.Where(f => !f.Annullata).ToList();

            KpiFatturato = FormattaEuro(valide.Sum(f => f.Importo));
            KpiIncassato = FormattaEuro(valide.Where(f => f.DataPagamento.HasValue).Sum(f => f.Importo));
            KpiDaIncassare = FormattaEuro(valide.Where(f => !f.DataPagamento.HasValue).Sum(f => f.Importo));
            var scaduto = valide.Where(f => !f.DataPagamento.HasValue
                                            && f.DataScadenza.HasValue
                                            && f.DataScadenza.Value.Date < oggi).Sum(f => f.Importo);
            KpiScaduto = FormattaEuro(scaduto);
            HaScaduto = scaduto > 0;
            OnPropertyChanged(nameof(KpiFatturato));
            OnPropertyChanged(nameof(KpiIncassato));
            OnPropertyChanged(nameof(KpiDaIncassare));
            OnPropertyChanged(nameof(KpiScaduto));
            OnPropertyChanged(nameof(HaScaduto));

            // Stesso pattern di riga del modulo Fatture (Fattura + intestatario).
            FattureStudio.Clear();
            foreach (var f in valide.OrderByDescending(f => f.DataEmissione).Take(50))
                FattureStudio.Add(new VoceFatturaStudio
                {
                    Fattura = f,
                    ReferenteRagioneSociale = referente.Nome
                });
            OnPropertyChanged(nameof(HaFattureStudio));
            OnPropertyChanged(nameof(EmptyFattureTesto));

            ClientiStudio.Clear();
            var clienti = db.Clienti.AsNoTracking()
                .Where(c => c.ReferenteId == referenteId)
                .OrderBy(c => c.RagioneSociale)
                .ToList();
            foreach (var c in clienti)
                ClientiStudio.Add(new VoceClienteStudio { Cliente = c });
            OnPropertyChanged(nameof(HaClientiStudio));
            OnPropertyChanged(nameof(EmptyClientiTesto));
        }

        private static string FormattaEuro(decimal valore) => $"€ {valore:N2}";
    }


    /// <summary>Riga della lista Studi (FASE 5): aggregati economici dello studio.</summary>
    public class VoceStudio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Attivo { get; set; } = true;

        /// <summary>Stato per il badge unificato: Regolare (attivo) o Disabilitato.</summary>
        public string StatoStudio => Attivo ? "Regolare" : "Disabilitato";

        public int NumeroClienti { get; set; }
        public decimal Fatturato { get; set; }
        public decimal DaIncassare { get; set; }
        public decimal Scaduto { get; set; }

        public bool HaScaduto => Scaduto > 0;

        public string ClientiTesto => NumeroClienti == 1 ? "1 cliente" : $"{NumeroClienti} clienti";
        public string FatturatoTesto => $"€ {Fatturato:N0}";
        public string DaIncassareTesto => $"€ {DaIncassare:N0}";
        public string ScadutoTesto => $"€ {Scaduto:N0}";
    }

    /// <summary>Fattura nello studio (stesso pattern di RigaFattura del modulo Fatture).</summary>
    public class VoceFatturaStudio
    {
        public Fattura Fattura { get; set; } = new();
        public string ReferenteRagioneSociale { get; set; } = string.Empty;
    }

    /// <summary>Cliente collegato allo studio nella lista del dettaglio.</summary>
    public class VoceClienteStudio
    {
        public Cliente Cliente { get; set; } = new();
    }
}
