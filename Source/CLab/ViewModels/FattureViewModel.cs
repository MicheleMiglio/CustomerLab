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

        private int _fatturaInModificaId;

        private Referente? _formReferente;
        public Referente? FormReferente { get => _formReferente; set { _formReferente = value; OnPropertyChanged(); } }

        private string _formNumero = string.Empty;
        public string FormNumero { get => _formNumero; set { _formNumero = value; OnPropertyChanged(); } }

        private DateTime? _formDataEmissione = DateTime.Now;
        public DateTime? FormDataEmissione { get => _formDataEmissione; set { _formDataEmissione = value; OnPropertyChanged(); } }

        private decimal? _formImporto;
        public decimal? FormImporto { get => _formImporto; set { _formImporto = value; OnPropertyChanged(); } }

        private DateTime? _formDataScadenza;
        public DateTime? FormDataScadenza { get => _formDataScadenza; set { _formDataScadenza = value; OnPropertyChanged(); } }

        private DateTime? _formDataPagamento;
        public DateTime? FormDataPagamento { get => _formDataPagamento; set { _formDataPagamento = value; OnPropertyChanged(); } }

        private bool _formAnnullata;
        public bool FormAnnullata { get => _formAnnullata; set { _formAnnullata = value; OnPropertyChanged(); } }

        private string _formNota = string.Empty;
        public string FormNota { get => _formNota; set { _formNota = value; OnPropertyChanged(); } }

        public ICommand NuovaCommand { get; }
        public ICommand ModificaCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand EliminaCommand { get; }
        public ICommand PulisciScadenzaCommand { get; }
        public ICommand PulisciPagamentoCommand { get; }
        public ICommand SegnaPagataOggiCommand { get; }

        public FattureViewModel()
        {
            NuovaCommand = new RelayCommand(Nuova);
            ModificaCommand = new RelayCommand<RigaFattura>(Modifica);
            SalvaCommand = new RelayCommand(Salva);
            AnnullaCommand = new RelayCommand(() => PannelloAperto = false);
            EliminaCommand = new RelayCommand<RigaFattura>(Elimina);
            PulisciScadenzaCommand = new RelayCommand(() => FormDataScadenza = null);
            PulisciPagamentoCommand = new RelayCommand(() => FormDataPagamento = null);
            SegnaPagataOggiCommand = new RelayCommand(() => FormDataPagamento = DateTime.Now);

            CaricaReferenti();
            CaricaFatture();
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

            foreach (var r in filtrate) FattureFiltrate.Add(r);
        }

        private void AggiornaTotali()
        {
            var valide = _fattureComplete.Where(r => !r.Fattura.Annullata).ToList();

            decimal totale = valide.Sum(r => r.Fattura.Importo);
            decimal incassato = valide.Where(r => r.Fattura.Pagata).Sum(r => r.Fattura.Importo);

            TotaleFatturatoTesto = $"€ {totale:N0}";
            IncassatoTesto = $"€ {incassato:N0}";
            DaIncassareTesto = $"€ {(totale - incassato):N0}";
            FattureScadute = valide.Count(r => r.Fattura.Stato == "Scaduta");
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

            PannelloAperto = true;
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
    }
}