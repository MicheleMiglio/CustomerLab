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
            set { _filtroTesto = value; OnPropertyChanged(); ApplicaFiltro(); }
        }

        // --- Filtro anno (groundwork FASE 4, selettore reale FASE 7).
        //     REGOLA ANNO CLab 2.0: se DataPagamento è valorizzata vale l'anno di
        //     DataPagamento; se NULL la fattura appartiene all'anno corrente.
        //     Centralizzata in AnnoFattura(): filtro lista, totali e KPI usano
        //     sempre la stessa funzione. null = nessun filtro ("Tutti gli anni"). ---

        private int? _annoFiltrato;
        public int? AnnoFiltrato
        {
            get => _annoFiltrato;
            private set
            {
                _annoFiltrato = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AnnoFiltratoTesto));
                OnPropertyChanged(nameof(HaFiltroAnno));
                OnPropertyChanged(nameof(EmptyStateTesto));
                OnPropertyChanged(nameof(OpzioneAnnoSelezionata));
            }
        }

        public bool HaFiltroAnno => _annoFiltrato.HasValue;

        public string AnnoFiltratoTesto => _annoFiltrato.HasValue ? $"Anno {_annoFiltrato}" : string.Empty;

        public string EmptyStateTesto => HaFiltroAnno
            ? $"Nessuna fattura per l'{AnnoFiltratoTesto}."
            : "Nessuna fattura registrata.";

        /// <summary>Regola anno definitiva CLab 2.0 (FASE 7, unificata in FASE 10):
        /// anno di DataPagamento se presente, altrimenti anno corrente. È l'unica
        /// regola di dominio: filtro lista, totali, KPI, anni disponibili del
        /// modulo e KPI fatture della Home la usano (HomeViewModel.IncludeAnnoFattura).</summary>
        public static int AnnoFattura(Fattura f) => f.DataPagamento?.Year ?? DateTime.Now.Year;

        private List<OpzioneAnnoVoce> _opzioniAnnoFatture = new();
        public List<OpzioneAnnoVoce> OpzioniAnnoFatture
        {
            get => _opzioniAnnoFatture;
            private set { _opzioniAnnoFatture = value; OnPropertyChanged(); }
        }

        /// <summary>Voce selezionata nel selettore anno: sincronizza il filtro (in entrambe le direzioni).</summary>
        public OpzioneAnnoVoce? OpzioneAnnoSelezionata
        {
            get => _opzioniAnnoFatture.FirstOrDefault(o => o.Anno == _annoFiltrato);
            set
            {
                if (value == null || _annoFiltrato == value.Anno) return;
                _annoFiltrato = value.Anno;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AnnoFiltrato));
                OnPropertyChanged(nameof(AnnoFiltratoTesto));
                OnPropertyChanged(nameof(HaFiltroAnno));
                OnPropertyChanged(nameof(EmptyStateTesto));
                CaricaFatture();
            }
        }

        public ICommand RimuoviFiltroAnnoCommand { get; }

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

        private int _fattureScadute;
        public int FattureScadute { get => _fattureScadute; private set { _fattureScadute = value; OnPropertyChanged(); } }

        // --- Pannello nuova/modifica ---

        private bool _pannelloAperto;
        public bool PannelloAperto { get => _pannelloAperto; set { _pannelloAperto = value; OnPropertyChanged(); } }

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
            RimuoviFiltroAnnoCommand = new RelayCommand(() => { AnnoFiltrato = null; CaricaFatture(); });
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
            foreach (var c in db.Referenti.AsNoTracking().OrderBy(x => x.Nome).ToList())
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

            // FASE 7: anni disponibili per il selettore (regola AnnoFattura), più "Tutti gli anni".
            OpzioniAnnoFatture = new List<OpzioneAnnoVoce>
            {
                new OpzioneAnnoVoce { Anno = null, Etichetta = "Tutti gli anni" }
            };
            foreach (var a in _fattureComplete
                         .Select(r => AnnoFattura(r.Fattura))
                         .Distinct()
                         .OrderByDescending(a => a))
            {
                OpzioniAnnoFatture.Add(new OpzioneAnnoVoce { Anno = a, Etichetta = $"Anno {a}" });
            }
            OnPropertyChanged(nameof(OpzioneAnnoSelezionata));

            ApplicaFiltro();
            AggiornaTotali();
        }

        private void ApplicaFiltro()
        {
            FattureFiltrate.Clear();

            var filtrate = string.IsNullOrWhiteSpace(FiltroTesto)
                ? _fattureComplete
                : _fattureComplete.Where(r =>
                    r.Fattura.Numero.Contains(FiltroTesto, StringComparison.OrdinalIgnoreCase) ||
                    r.ReferenteRagioneSociale.Contains(FiltroTesto, StringComparison.OrdinalIgnoreCase));

            if (_annoFiltrato.HasValue)
                filtrate = filtrate.Where(r => AnnoFattura(r.Fattura) == _annoFiltrato.Value);

            foreach (var r in filtrate) FattureFiltrate.Add(r);
        }

        private void AggiornaTotali()
        {
            var valide = _fattureComplete.Where(r => !r.Fattura.Annullata).ToList();

            if (_annoFiltrato.HasValue)
                valide = valide.Where(r => AnnoFattura(r.Fattura) == _annoFiltrato.Value).ToList();

            decimal totale = valide.Sum(r => r.Fattura.Importo);
            decimal incassato = valide.Where(r => r.Fattura.Pagata).Sum(r => r.Fattura.Importo);

            TotaleFatturatoTesto = $"€ {totale:N0}";
            IncassatoTesto = $"€ {incassato:N0}";
            DaIncassareTesto = $"€ {(totale - incassato):N0}";
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
            AnnoFiltrato = anno;
            CaricaFatture();
        }

        private void Nuova()
        {
            _fatturaInModificaId = 0;
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

    /// <summary>Voce del selettore anno (FASE 7): null = "Tutti gli anni".</summary>
    public class OpzioneAnnoVoce
    {
        public int? Anno { get; set; }
        public string Etichetta { get; set; } = string.Empty;
    }
}