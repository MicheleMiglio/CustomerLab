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
    public class PromemoriaViewModel : ViewModelBase
    {
        private readonly Action? _aggiornaBadge;
        private int _promemoriaInModificaId;

        public ObservableCollection<Promemoria> Promemoria { get; set; } = new();

        private bool _ordinamentoPerPriorita;
        public bool OrdinamentoPerPriorita
        {
            get => _ordinamentoPerPriorita;
            set
            {
                if (_ordinamentoPerPriorita == value) return;
                _ordinamentoPerPriorita = value;
                OnPropertyChanged();
                Ordina();
            }
        }

        // --- Pannello nuovo/modifica ---

        private bool _pannelloAperto;
        public bool PannelloAperto { get => _pannelloAperto; set { _pannelloAperto = value; OnPropertyChanged(); } }

        private string _titoloPannello = "Nuovo promemoria";
        public string TitoloPannello { get => _titoloPannello; set { _titoloPannello = value; OnPropertyChanged(); } }

        private string _formTitolo = string.Empty;
        public string FormTitolo { get => _formTitolo; set { _formTitolo = value; OnPropertyChanged(); } }

        private string _formDescrizione = string.Empty;
        public string FormDescrizione { get => _formDescrizione; set { _formDescrizione = value; OnPropertyChanged(); } }

        private PrioritaPromemoria _formPriorita = PrioritaPromemoria.Media;
        public PrioritaPromemoria FormPriorita { get => _formPriorita; set { _formPriorita = value; OnPropertyChanged(); } }

        public ICommand MostraPerDataCommand { get; }
        public ICommand MostraPerPrioritaCommand { get; }
        public ICommand NuovoCommand { get; }
        public ICommand ModificaCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand ImpostaPrioritaCommand { get; }

        public PromemoriaViewModel(Action? aggiornaBadge = null)
        {
            _aggiornaBadge = aggiornaBadge;

            MostraPerDataCommand = new RelayCommand(() => OrdinamentoPerPriorita = false);
            MostraPerPrioritaCommand = new RelayCommand(() => OrdinamentoPerPriorita = true);
            NuovoCommand = new RelayCommand(Nuovo);
            ModificaCommand = new RelayCommand<Promemoria>(Modifica);
            SalvaCommand = new RelayCommand(Salva, () => !string.IsNullOrWhiteSpace(FormTitolo));
            AnnullaCommand = new RelayCommand(() => PannelloAperto = false);
            ImpostaPrioritaCommand = new RelayCommand<PrioritaPromemoria?>(p =>
            {
                if (p.HasValue) FormPriorita = p.Value;
            });

            Carica();
        }

        private void Carica()
        {
            using var db = new ClabDbContext();
            var elenco = db.Promemoria.AsNoTracking().ToList();

            Promemoria.Clear();
            foreach (var p in Ordinati(elenco))
                Promemoria.Add(p);
        }

        private IEnumerable<Promemoria> Ordinati(IEnumerable<Promemoria> elenco)
        {
            return OrdinamentoPerPriorita
                ? elenco.OrderByDescending(p => p.Priorita).ThenByDescending(p => p.DataCreazione)
                : elenco.OrderByDescending(p => p.DataCreazione);
        }

        private void Ordina()
        {
            var ordinati = Ordinati(Promemoria.ToList()).ToList();
            Promemoria.Clear();
            foreach (var p in ordinati)
                Promemoria.Add(p);
        }

        private void Nuovo()
        {
            _promemoriaInModificaId = 0;
            TitoloPannello = "Nuovo promemoria";
            FormTitolo = string.Empty;
            FormDescrizione = string.Empty;
            FormPriorita = PrioritaPromemoria.Media;
            PannelloAperto = true;
        }

        private void Modifica(Promemoria? p)
        {
            if (p == null) return;

            _promemoriaInModificaId = p.Id;
            TitoloPannello = "Modifica promemoria";
            FormTitolo = p.Titolo;
            FormDescrizione = p.Descrizione ?? string.Empty;
            FormPriorita = p.Priorita;
            PannelloAperto = true;
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(FormTitolo))
                return;

            using var db = new ClabDbContext();

            Promemoria entita;
            if (_promemoriaInModificaId == 0)
            {
                entita = new Promemoria { DataCreazione = DateTime.Now };
                db.Promemoria.Add(entita);
            }
            else
            {
                entita = db.Promemoria.First(x => x.Id == _promemoriaInModificaId);
            }

            entita.Titolo = FormTitolo.Trim();
            entita.Descrizione = string.IsNullOrWhiteSpace(FormDescrizione) ? null : FormDescrizione.Trim();
            entita.Priorita = FormPriorita;

            db.SaveChanges();

            PannelloAperto = false;
            Carica();
            _aggiornaBadge?.Invoke();
        }

        /// <summary>
        /// Cancellazione vera e propria. Chiamata dalla view a fade-out
        /// dell'animazione già concluso: qui il post-it sparisce anche
        /// dai dati, non solo dallo schermo.
        /// </summary>
        public void RimuoviDefinitivamente(Promemoria? p)
        {
            if (p == null) return;

            using var db = new ClabDbContext();
            var entita = db.Promemoria.FirstOrDefault(x => x.Id == p.Id);
            if (entita != null)
            {
                db.Promemoria.Remove(entita);
                db.SaveChanges();
            }

            Promemoria.Remove(p);
            _aggiornaBadge?.Invoke();
        }
    }
}