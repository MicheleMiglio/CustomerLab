using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace CLab.ViewModels
{
    /// <summary>
    /// FASE 12 CLab 2.0: ricerca globale (Ctrl+K). Interroga dati reali con
    /// query LINQ/EF Core dirette (Clienti per nome/P.IVA, Studi per nome,
    /// Fatture per numero, ToDo per titolo), massimo 3 risultati per categoria,
    /// debounce ~200 ms, navigazione al risultato tramite INavigatore.
    /// Nessun motore full-text, nessun framework.
    /// </summary>
    public class GlobalSearchViewModel : ViewModelBase
    {
        private readonly INavigatore _navigatore;
        private readonly DispatcherTimer _debounce;

        public ICommand ApriCommand { get; }
        public ICommand ChiudiCommand { get; }
        public ICommand NavigaCommand { get; }

        private bool _aperta;
        public bool Aperta
        {
            get => _aperta;
            private set
            {
                _aperta = value;
                OnPropertyChanged();
                if (value) Testo = string.Empty;
            }
        }

        private string _testo = string.Empty;
        public string Testo
        {
            get => _testo;
            set
            {
                _testo = value;
                OnPropertyChanged();

                // Debounce ~200ms: la ricerca parte quando l'utente si ferma.
                _debounce.Stop();
                _debounce.Start();
            }
        }

        public ObservableCollection<VoceRisultatoRicerca> Risultati { get; } = new();
        public bool HaRisultati => Risultati.Count > 0;

        public string MessaggioVuoto { get; private set; } = string.Empty;
        public bool MostraMessaggioVuoto => Aperta && Testo.Trim().Length >= 2 && !HaRisultati;
        public bool MostraSuggerimento => Aperta && Testo.Trim().Length < 2;

        public GlobalSearchViewModel(INavigatore navigatore)
        {
            _navigatore = navigatore;

            ApriCommand = new RelayCommand(Apri);
            ChiudiCommand = new RelayCommand(Chiudi);
            NavigaCommand = new RelayCommand<VoceRisultatoRicerca>(Naviga);

            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _debounce.Tick += (_, _) =>
            {
                _debounce.Stop();
                EseguiRicerca();
            };
        }

        public void Apri() => Aperta = true;

        public void Chiudi() => Aperta = false;


        private void EseguiRicerca()
        {
            Risultati.Clear();

            var q = Testo.Trim().ToLower();
            if (q.Length < 2)
            {
                Notifica();
                return;
            }

            using var db = new ClabDbContext();

            foreach (var c in db.Clienti.AsNoTracking()
                         .Where(c => c.RagioneSociale.ToLower().Contains(q)
                                     || (c.PartitaIva ?? "").ToLower().Contains(q))
                         .OrderBy(c => c.RagioneSociale)
                         .Take(3))
                Risultati.Add(new VoceRisultatoRicerca
                {
                    Categoria = "Clienti",
                    Id = c.Id,
                    Titolo = c.RagioneSociale,
                    Sottotitolo = string.IsNullOrWhiteSpace(c.PartitaIva) ? "Cliente" : $"P.IVA {c.PartitaIva}"
                });

            foreach (var r in db.Referenti.AsNoTracking()
                         .Where(r => r.Nome.ToLower().Contains(q))
                         .OrderBy(r => r.Nome)
                         .Take(3))
                Risultati.Add(new VoceRisultatoRicerca { Categoria = "Studi", Id = r.Id, Titolo = r.Nome, Sottotitolo = "Studio" });

            foreach (var f in db.Fatture.AsNoTracking()
                         .Where(f => f.Numero.ToLower().Contains(q))
                         .OrderByDescending(f => f.DataEmissione)
                         .Take(3))
                Risultati.Add(new VoceRisultatoRicerca
                {
                    Categoria = "Fatture",
                    Id = f.Id,
                    Titolo = $"Fattura n. {f.Numero}",
                    Sottotitolo = f.DataEmissione.ToString("dd/MM/yyyy")
                });

            foreach (var t in db.ToDo.AsNoTracking()
                         .Where(t => t.Titolo.ToLower().Contains(q))
                         .OrderByDescending(t => t.DataCreazione)
                         .Take(3))
                Risultati.Add(new VoceRisultatoRicerca { Categoria = "ToDo", Id = t.Id, Titolo = t.Titolo, Sottotitolo = "ToDo" });

            MessaggioVuoto = $"Nessun risultato per \"{Testo.Trim()}\".";
            Notifica();
        }

        private void Naviga(VoceRisultatoRicerca? v)
        {
            if (v == null) return;

            switch (v.Categoria)
            {
                case "Clienti": _navigatore.ApriCliente(v.Id); break;
                case "Studi": _navigatore.ApriStudi(v.Id); break;
                case "Fatture": _navigatore.ApriFattura(v.Id); break;
                case "ToDo": _navigatore.ApriToDo(todoId: v.Id); break;
            }

            Aperta = false;
        }

        private void Notifica()
        {
            OnPropertyChanged(nameof(HaRisultati));
            OnPropertyChanged(nameof(MessaggioVuoto));
            OnPropertyChanged(nameof(MostraMessaggioVuoto));
            OnPropertyChanged(nameof(MostraSuggerimento));
        }
    }

    /// <summary>Risultato della ricerca globale (FASE 12).</summary>
    public class VoceRisultatoRicerca
    {
        public string Categoria { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Titolo { get; set; } = string.Empty;
        public string Sottotitolo { get; set; } = string.Empty;
    }
}
