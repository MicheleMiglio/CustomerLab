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
    public class FattureViewModel : ViewModelBase
    {
        public ObservableCollection<Referente> ReferentiDisponibili { get; set; } = new();

        private List<RigaFattura> _fattureComplete = new();
        public ObservableCollection<RigaFattura> FattureFiltrate { get; set; } = new();

        private string _filtroTesto = string.Empty;
        public string FiltroTesto
        {
            get => _filtroTesto;
            set { _filtroTesto = value; OnPropertyChanged(); OnPropertyChanged(nameof(HaFiltriAttivi)); OnPropertyChanged(nameof(EmptyStateTesto)); ApplicaFiltro(); }
        }

        // --- Navigazione annuale CLab 2.0 ---
        //     REGOLA ANNO CLab 2.0: se DataPagamento è valorizzata vale l'anno di
        //     DataPagamento; se NULL la fattura appartiene all'anno corrente.
        //     Centralizzata in AnnoFattura(): lista, totali e KPI usano sempre la
        //     stessa funzione (la Home la riusa via HomeViewModel.IncludeAnnoFattura).
        //     L'anno si sceglie con il navigatore ‹ anno ›: apertura sull'anno
        //     corrente, navigazione libera senza limiti, nessuna voce "tutti".

        private int _annoSelezionato = DateTime.Now.Year;
        public int AnnoSelezionato
        {
            get => _annoSelezionato;
            private set
            {
                _annoSelezionato = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EmptyStateTesto));
                OnPropertyChanged(nameof(HaFiltriAttivi));
            }
        }

        public ICommand AnnoPrecedenteCommand { get; }
        public ICommand AnnoSuccessivoCommand { get; }

        public string EmptyStateTesto => HaFiltriAttivi
            ? "Nessuna fattura corrisponde ai filtri applicati."
            : $"Nessuna fattura nel {AnnoSelezionato}.";

        /// <summary>Regola anno definitiva CLab 2.0 (FASE 7, unificata in FASE 10):
        /// anno di DataPagamento se presente, altrimenti anno corrente. È l'unica
        /// regola di dominio: filtro lista, totali, KPI, navigatore annuale del
        /// modulo e KPI fatture della Home la usano (HomeViewModel.IncludeAnnoFattura).</summary>
        public static int AnnoFattura(Fattura f) => f.DataPagamento?.Year ?? DateTime.Now.Year;

        // --- Filtri (stato / studio / ricerca) ---
        //     Stato: "Tutte" | "Emesse" | "Pagate" | "Annullate" (chip segmentati).
        //     Studio: Referente selezionato o null ("tutti gli studi").

        private string _filtroStato = "Tutte";
        public string FiltroStato
        {
            get => _filtroStato;
            set { _filtroStato = value; OnPropertyChanged(); OnPropertyChanged(nameof(HaFiltriAttivi)); OnPropertyChanged(nameof(EmptyStateTesto)); ApplicaFiltro(); }
        }

        private Referente? _filtroStudio;
        public Referente? FiltroStudio
        {
            get => _filtroStudio;
            set { _filtroStudio = value; OnPropertyChanged(); OnPropertyChanged(nameof(HaFiltriAttivi)); OnPropertyChanged(nameof(EmptyStateTesto)); ApplicaFiltro(); }
        }

        public bool HaFiltriAttivi => FiltroStato != "Tutte" || FiltroStudio != null || !string.IsNullOrWhiteSpace(FiltroTesto);

        public ICommand ImpostaStatoFiltroCommand { get; }
        public ICommand AzzeraFiltriCommand { get; }
        public ICommand RimuoviFiltroStudioCommand { get; }

        /// <summary>Quick action "Pagata oggi" sulla riga: imposta DataPagamento = oggi
        /// e persiste immediatamente con lo stesso percorso del modulo (EF + SaveChanges
        /// + reload); rispetta la regola anno (dopo il reload la fattura segue il filtro).</summary>
        public ICommand PagataOggiRigaCommand { get; }

        private string _totaleFatturatoTesto = "€ 0";
        public string TotaleFatturatoTesto { get => _totaleFatturatoTesto; private set { _totaleFatturatoTesto = value; OnPropertyChanged(); } }

        private string _incassatoTesto = "€ 0";
        public string IncassatoTesto { get => _incassatoTesto; private set { _incassatoTesto = value; OnPropertyChanged(); } }

        private string _daIncassareTesto = "€ 0";
        public string DaIncassareTesto { get => _daIncassareTesto; private set { _daIncassareTesto = value; OnPropertyChanged(); } }

        private string _anomalieTesto = "0";
        public string AnomalieTesto { get => _anomalieTesto; private set { _anomalieTesto = value; OnPropertyChanged(); } }

        private int _fattureScadute;
        public int FattureScadute { get => _fattureScadute; private set { _fattureScadute = value; OnPropertyChanged(); } }

        // --- Pannello nuova/modifica ---

        private bool _pannelloAperto;
        public bool PannelloAperto { get => _pannelloAperto; set { _pannelloAperto = value; OnPropertyChanged(); } }

        /// <summary>Titolo del modal: "Nuova fattura" o "Modifica fattura" in base all'operazione.</summary>
        private string _titoloPannello = "Nuova fattura";
        public string TitoloPannello { get => _titoloPannello; set { _titoloPannello = value; OnPropertyChanged(); } }

        // FASE 3: modifiche non salvate, per la conferma di chiusura del
        // SidePanelControl. Viene azzerato a ogni apertura del form (Nuova/Modifica).
        private bool _haModifiche;
        public bool HaModifiche { get => _haModifiche; set { _haModifiche = value; OnPropertyChanged(); } }
        private void SegnaModificato() => HaModifiche = true;

        private int _fatturaInModificaId;

        private Referente? _formReferente;
        public Referente? FormReferente { get => _formReferente; set { _formReferente = value; OnPropertyChanged(); SegnaModificato(); } }

        private string _formNumero = string.Empty;
        public string FormNumero { get => _formNumero; set { _formNumero = value; OnPropertyChanged(); SegnaModificato(); } }

        private DateTime? _formDataEmissione = DateTime.Now;
        public DateTime? FormDataEmissione { get => _formDataEmissione; set { _formDataEmissione = value; OnPropertyChanged(); SegnaModificato(); } }

        private decimal? _formImporto;
        public decimal? FormImporto { get => _formImporto; set { _formImporto = value; OnPropertyChanged(); SegnaModificato(); } }

        private DateTime? _formDataScadenza;
        public DateTime? FormDataScadenza { get => _formDataScadenza; set { _formDataScadenza = value; OnPropertyChanged(); SegnaModificato(); } }

        private DateTime? _formDataPagamento;
        public DateTime? FormDataPagamento { get => _formDataPagamento; set { _formDataPagamento = value; OnPropertyChanged(); SegnaModificato(); } }

        private bool _formAnnullata;
        public bool FormAnnullata { get => _formAnnullata; set { _formAnnullata = value; OnPropertyChanged(); SegnaModificato(); } }

        private string _formNota = string.Empty;
        public string FormNota { get => _formNota; set { _formNota = value; OnPropertyChanged(); SegnaModificato(); } }

        public ICommand NuovaCommand { get; }
        public ICommand ModificaCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand EliminaCommand { get; }
        public ICommand PulisciScadenzaCommand { get; }
        public ICommand PulisciPagamentoCommand { get; }
        public ICommand SegnaPagataOggiCommand { get; }
        public ICommand ApriStudioCommand { get; }

        private readonly INavigatore? _navigatore;

        public FattureViewModel(INavigatore? navigatore = null)
        {
            _navigatore = navigatore;

            NuovaCommand = new RelayCommand(Nuova);
            ModificaCommand = new RelayCommand<RigaFattura>(Modifica);
            SalvaCommand = new RelayCommand(Salva);
            AnnullaCommand = new RelayCommand(() => PannelloAperto = false);
            EliminaCommand = new RelayCommand<RigaFattura>(Elimina);
            PulisciScadenzaCommand = new RelayCommand(() => FormDataScadenza = null);
            PulisciPagamentoCommand = new RelayCommand(() => FormDataPagamento = null);
            SegnaPagataOggiCommand = new RelayCommand(() => FormDataPagamento = DateTime.Now);
            AnnoPrecedenteCommand = new RelayCommand(() => { AnnoSelezionato--; CaricaFatture(); });
            AnnoSuccessivoCommand = new RelayCommand(() => { AnnoSelezionato++; CaricaFatture(); });
            ImpostaStatoFiltroCommand = new RelayCommand<string?>(s => FiltroStato = s ?? "Tutte");
            AzzeraFiltriCommand = new RelayCommand(() => { FiltroStato = "Tutte"; FiltroStudio = null; FiltroTesto = string.Empty; });
            RimuoviFiltroStudioCommand = new RelayCommand(() => FiltroStudio = null);
            PagataOggiRigaCommand = new RelayCommand<RigaFattura>(SegnaPagataOggiRiga);
            ApriStudioCommand = new RelayCommand<RigaFattura>(ApriStudio);

            CaricaReferenti();
            CaricaFatture();
        }

        /// <summary>FASE 10: apre il dettaglio dello Studio (Referente) cui la
        /// fattura è intestata. Nessun Fattura.ClienteId: il rapporto resta
        /// Fattura → Referente.</summary>
        private void ApriStudio(RigaFattura? riga)
        {
            if (riga?.Fattura.ReferenteId == null) return;
            _navigatore?.ApriStudi(riga.Fattura.ReferenteId.Value);
        }

        private void CaricaReferenti()
        {
            using var db = new ClabDbContext();
            ReferentiDisponibili.Clear();
            foreach (var c in db.Referenti.AsNoTracking().Where(x => x.Attivo).OrderBy(x => x.Nome).ToList())
                ReferentiDisponibili.Add(c);
        }

        private void CaricaFatture()
        {
            using var db = new ClabDbContext();

            var Referenti = db.Referenti.AsNoTracking().ToDictionary(c => c.Id, c => c.Nome);

            _fattureComplete = db.Fatture.AsNoTracking()
                .OrderByDescending(f => f.DataEmissione)
                .ToList()
                .Select(f => new RigaFattura
                {
                    Fattura = f,
                    ReferenteRagioneSociale = f.ReferenteId.HasValue && Referenti.TryGetValue(f.ReferenteId.Value, out var nome) ? nome : "—"
                })
                .ToList();

            ApplicaFiltro();
            AggiornaTotali();
        }

        private void ApplicaFiltro()
        {
            FattureFiltrate.Clear();

            // Anno: sempre attivo (navigatore ‹ anno ›), con la regola AnnoFattura.
            var filtrate = _fattureComplete
                .Where(r => AnnoFattura(r.Fattura) == AnnoSelezionato)
                .Where(r =>
                    r.Fattura.Numero.Contains(FiltroTesto, StringComparison.OrdinalIgnoreCase) ||
                    r.ReferenteRagioneSociale.Contains(FiltroTesto, StringComparison.OrdinalIgnoreCase));

            // Stato (Stato calcolato: Emessa/Scaduta/Pagata/Annullata).
            // "Emesse" = tutte le non pagate non annullate (scadute incluse).
            if (FiltroStato == "Pagate")
                filtrate = filtrate.Where(r => r.Fattura.Pagata && !r.Fattura.Annullata);
            else if (FiltroStato == "Annullate")
                filtrate = filtrate.Where(r => r.Fattura.Annullata);
            else if (FiltroStato == "Emesse")
                filtrate = filtrate.Where(r => !r.Fattura.Pagata && !r.Fattura.Annullata);
            else if (FiltroStato == "Anomalie")
                filtrate = filtrate.Where(r => (r.Fattura.HaAnomalie || r.Fattura.Stato == "Scaduta") && !r.Fattura.Annullata);

            // Studio (Referente).
            if (FiltroStudio != null)
                filtrate = filtrate.Where(r => r.Fattura.ReferenteId == FiltroStudio.Id);

            foreach (var r in filtrate) FattureFiltrate.Add(r);
        }

        private void AggiornaTotali()
        {
            // I KPI seguono l'anno selezionato (navigatore) e restano stabili
            // durante ricerca/filtri: raccontano l'anno, non la vista filtrata.
            var valide = _fattureComplete
                .Where(r => !r.Fattura.Annullata && AnnoFattura(r.Fattura) == AnnoSelezionato)
                .ToList();

            decimal totale = valide.Sum(r => r.Fattura.Importo);
            decimal incassato = valide.Where(r => r.Fattura.Pagata).Sum(r => r.Fattura.Importo);

            TotaleFatturatoTesto = $"€ {totale:N0}";
            IncassatoTesto = $"€ {incassato:N0}";
            DaIncassareTesto = $"€ {(totale - incassato):N0}";

            int anomalie = valide.Count(r => r.Fattura.HaAnomalie);
            int scadute = valide.Count(r => r.Fattura.Stato == "Scaduta");

            anomalie += scadute; // KPI "Anomalie" = anomalie + scadute (FASE 10)

            AnomalieTesto = anomalie.ToString();

            FattureScadute = valide.Count(r => r.Fattura.Stato == "Scaduta");
        }

        /// <summary>
        /// FASE 7 — Quick action "Pagata oggi" sulla riga: imposta DataPagamento
        /// a oggi e persiste immediatamente con lo stesso percorso del modulo
        /// (entità EF + SaveChanges + CaricaFatture), identico a Salva/Elimina.
        /// Nessun dialog: azione atomica e reversibile dalla modifica.
        /// </summary>
        private void SegnaPagataOggiRiga(RigaFattura? r)
        {
            if (r == null || r.Fattura.DataPagamento.HasValue || r.Fattura.Annullata) return;

            using var db = new ClabDbContext();
            var entita = db.Fatture.First(f => f.Id == r.Fattura.Id);
            entita.DataPagamento = DateTime.Now;
            db.SaveChanges();

            // Regola anno: dopo il reload la fattura segue naturalmente il filtro
            // (se il pagamento cade in un anno diverso da quello filtrato, esce dalla lista).
            CaricaFatture();
        }

        /// <summary>
        /// Groundwork navigazione contestuale (FASE 4): apre il modulo con la
        /// lista e i totali limitati all'anno indicato. Nessun collegamento a
        /// un cliente: Fattura non ha ClienteId (valutazione rimandata a una
        /// eventuale futura fase DB).
        /// </summary>
        public void ApriSuAnno(int anno)
        {
            AnnoSelezionato = anno;
            CaricaFatture();
        }

        private void Nuova()
        {
            _fatturaInModificaId = 0;
            TitoloPannello = "Nuova fattura";
            FormReferente = null;
            FormNumero = string.Empty;
            FormDataEmissione = DateTime.Now;
            FormImporto = null;
            FormDataScadenza = null;
            FormDataPagamento = null;
            FormAnnullata = false;
            FormNota = string.Empty;

            HaModifiche = false; // FASE 3: il caricamento non è una modifica dell'utente
            PannelloAperto = true;
        }

        /// <summary>FASE 12: apre direttamente il pannello della fattura indicata
        /// (navigazione dalla ricerca globale). Stesso percorso di Modifica.</summary>
        public void ApriFatturaDiretta(int fatturaId)
        {
            var riga = _fattureComplete.FirstOrDefault(r => r.Fattura.Id == fatturaId);
            if (riga != null)
                Modifica(riga);
        }

        private void Modifica(RigaFattura? r)
        {
            if (r == null) return;
            var f = r.Fattura;

            _fatturaInModificaId = f.Id;
            TitoloPannello = "Modifica fattura";
            FormReferente = ReferentiDisponibili.FirstOrDefault(c => c.Id == f.ReferenteId);
            FormNumero = f.Numero;
            FormDataEmissione = f.DataEmissione;
            FormImporto = f.Importo;
            FormDataScadenza = f.DataScadenza;
            FormDataPagamento = f.DataPagamento;
            FormAnnullata = f.Annullata;
            FormNota = f.Nota ?? string.Empty;

            HaModifiche = false; // FASE 3: il caricamento non è una modifica dell'utente
            PannelloAperto = true;
        }

        private void Salva()
        {
            if (FormReferente == null || string.IsNullOrWhiteSpace(FormNumero) || !FormDataEmissione.HasValue || !FormImporto.HasValue)
            {
                MessageBox.Show("Referente, numero, data di emissione e importo sono obbligatori.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new ClabDbContext();

            Fattura entita;
            if (_fatturaInModificaId == 0)
            {
                entita = new Fattura();
                db.Fatture.Add(entita);
            }
            else
            {
                entita = db.Fatture.First(f => f.Id == _fatturaInModificaId);
            }

            entita.ReferenteId = FormReferente.Id;
            entita.Numero = FormNumero.Trim();
            entita.DataEmissione = FormDataEmissione.Value;
            entita.Importo = FormImporto.Value;
            entita.DataScadenza = FormDataScadenza;
            entita.DataPagamento = FormDataPagamento;
            entita.Annullata = FormAnnullata;
            entita.Nota = string.IsNullOrWhiteSpace(FormNota) ? null : FormNota.Trim();

            db.SaveChanges();

            PannelloAperto = false;
            CaricaFatture();
        }

        private void Elimina(RigaFattura? r)
        {
            if (r == null) return;

            var esito = MessageBox.Show($"Eliminare la fattura n. \"{r.Fattura.Numero}\"?", "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (esito != MessageBoxResult.Yes) return;

            using var db = new ClabDbContext();
            var entita = db.Fatture.First(x => x.Id == r.Fattura.Id);
            db.Fatture.Remove(entita);
            db.SaveChanges();

            CaricaFatture();
        }
    }

    /// <summary>Una fattura + il nome del Referente a cui è intestata, per la griglia.</summary>
    public class RigaFattura
    {
        public Fattura Fattura { get; set; } = new();
        public string ReferenteRagioneSociale { get; set; } = string.Empty;

        /// <summary>FASE 7: la quick action "Pagata oggi" ha senso solo per fatture attive non pagate.</summary>
        public bool PuòEsserePagataOggi => !Fattura.Pagata && !Fattura.Annullata;

        /// <summary>FASE 10: lo Studio è cliccabile solo se la fattura è intestata a un Referente.</summary>
        public bool HaStudio => Fattura.ReferenteId.HasValue;
    }
}