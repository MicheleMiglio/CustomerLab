using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public class PromemoriaViewModel : ViewModelBase
    {
        private readonly Action? _aggiornaBadge;
        private int _promemoriaInModificaId;

        // Tre colonne fisse, una per priorità. Più recenti in alto.
        public ObservableCollection<Promemoria> PromemoriaAlta { get; } = new();
        public ObservableCollection<Promemoria> PromemoriaMedia { get; } = new();
        public ObservableCollection<Promemoria> PromemoriaBassa { get; } = new();

        // --- Pannello nuovo/modifica ---

        private bool _pannelloAperto;
        public bool PannelloAperto { get => _pannelloAperto; set { _pannelloAperto = value; OnPropertyChanged(); } }

        private bool _haModifiche;
        public bool HaModifiche { get => _haModifiche; set { _haModifiche = value; OnPropertyChanged(); } }
        private void SegnaModificato() => HaModifiche = true;

        private string _titoloPannello = "Nuovo promemoria";
        public string TitoloPannello { get => _titoloPannello; set { _titoloPannello = value; OnPropertyChanged(); } }

        private string _formTitolo = string.Empty;
        public string FormTitolo { get => _formTitolo; set { _formTitolo = value; OnPropertyChanged(); SegnaModificato(); } }

        private string _formDescrizione = string.Empty;
        public string FormDescrizione { get => _formDescrizione; set { _formDescrizione = value; OnPropertyChanged(); SegnaModificato(); } }

        private PrioritaPromemoria _formPriorita = PrioritaPromemoria.Media;
        public PrioritaPromemoria FormPriorita { get => _formPriorita; set { _formPriorita = value; OnPropertyChanged(); SegnaModificato(); } }

        public ICommand NuovoCommand { get; }
        public ICommand ModificaCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand ImpostaPrioritaCommand { get; }

        public PromemoriaViewModel(Action? aggiornaBadge = null)
        {
            _aggiornaBadge = aggiornaBadge;

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
            var elenco = db.Promemoria.AsNoTracking().ToList()
                           .OrderByDescending(p => p.DataCreazione)
                           .ToList();

            Riempi(PromemoriaAlta, elenco.Where(p => p.Priorita == PrioritaPromemoria.Alta));
            Riempi(PromemoriaMedia, elenco.Where(p => p.Priorita == PrioritaPromemoria.Media));
            Riempi(PromemoriaBassa, elenco.Where(p => p.Priorita == PrioritaPromemoria.Bassa));
        }

        private static void Riempi(ObservableCollection<Promemoria> colonna, System.Collections.Generic.IEnumerable<Promemoria> elementi)
        {
            colonna.Clear();
            foreach (var p in elementi)
                colonna.Add(p);
        }

        private void Nuovo()
        {
            _promemoriaInModificaId = 0;
            TitoloPannello = "Nuovo promemoria";
            FormTitolo = string.Empty;
            FormDescrizione = string.Empty;
            FormPriorita = PrioritaPromemoria.Media;
            HaModifiche = false;
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
            HaModifiche = false;
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
            Carica(); // se la priorità cambia, il post-it passa da solo alla nuova colonna
            _aggiornaBadge?.Invoke();
        }

        /// <summary>Cancellazione vera, chiamata a fade-out concluso.</summary>
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

            PromemoriaAlta.Remove(p);
            PromemoriaMedia.Remove(p);
            PromemoriaBassa.Remove(p);
            _aggiornaBadge?.Invoke();
        }
    }
}