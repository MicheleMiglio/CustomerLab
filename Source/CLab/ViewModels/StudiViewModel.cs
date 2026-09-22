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
            set { 
                _filtroTesto = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HaFiltriAttivi)); 
                OnPropertyChanged(nameof(EmptyStateTesto)); 
                ApplicaFiltro(); 
            }
        }

        // --- Filtri (stato / ricerca) ---
        //     Stato: chiavi del badge unificato ("Regolare", "Ritardo",
        //     "InAttesa", "Disabilitato"); "Tutti" = nessun filtro.

        private string _filtroStato = "Tutti";
        public string FiltroStato
        {
            get => _filtroStato;
            set { _filtroStato = value; OnPropertyChanged(); OnPropertyChanged(nameof(HaFiltriAttivi)); OnPropertyChanged(nameof(EmptyStateTesto)); ApplicaFiltro(); }
        }

        public bool HaFiltriAttivi => FiltroStato != "Tutti" || !string.IsNullOrWhiteSpace(FiltroTesto);

        public ICommand ImpostaStatoFiltroCommand { get; }
        public ICommand AzzeraFiltriCommand { get; }

        public string ContatoreTesto { get; private set; } = string.Empty;

        public bool NessunoStudio => _studiCompleti.Count == 0;
        public string EmptyStateTesto => NessunoStudio
            ? "Nessuno studio presente."
            : "Nessuno studio corrisponde ai filtri o alla ricerca.";

        public string EmptyStateSuggerimento => NessunoStudio
            ? "Crea il primo studio con il pulsante + Nuovo studio."
            : string.Empty;

        public ICommand ApriFattureStudioCommand { get; }
        public ICommand ApriClientiStudioCommand { get; }

        // --- Creazione / modifica / eliminazione studio (regole del modulo Clienti) ---

        public ICommand NuovoStudioCommand { get; }
        public ICommand ModificaStudioCommand { get; }
        public ICommand SalvaStudioCommand { get; }
        public ICommand AnnullaStudioCommand { get; }
        public ICommand EliminaStudioCommand { get; }
        public ICommand RiattivaStudioCommand { get; }

        // --- Dati dello studio nel modal (solo in modifica): prime 5 fatture
        //     per data pagamento più recente e primi 5 clienti in ordine
        //     alfabetico, con collegamento ai moduli prefiltrati. ---

        public ObservableCollection<VoceFatturaStudio> FattureStudio { get; } = new();
        public bool HaFattureStudio => FattureStudio.Count > 0;
        public string EmptyFattureTesto => "Nessuna fattura intestata a questo studio.";

        public ObservableCollection<VoceClienteStudio> ClientiStudio { get; } = new();
        public bool HaClientiStudio => ClientiStudio.Count > 0;
        public string EmptyClientiTesto => "Nessun cliente collegato a questo studio.";

        /// <summary>I dati (fatture/clienti) si mostrano solo in modifica:
        /// uno studio nuovo non ha ancora fatture né clienti collegati.</summary>
        public bool ModalMostraDati => _studioInModificaId != 0;

        private string _studioCorrenteNome = string.Empty;

        // --- Modal nuova/modifica studio (ModalDialogControl, pattern Fatture) ---

        private bool _pannelloAperto;
        public bool PannelloAperto { get => _pannelloAperto; set { _pannelloAperto = value; OnPropertyChanged(); } }

        /// <summary>Titolo del modal: "Nuovo studio" o "Modifica studio" in base all'operazione.</summary>
        private string _titoloPannello = "Nuovo studio";
        public string TitoloPannello { get => _titoloPannello; set { _titoloPannello = value; OnPropertyChanged(); } }

        private int _studioInModificaId;

        private bool _haModifiche;
        public bool HaModifiche
        {
            get => _haModifiche;
            set { _haModifiche = value; OnPropertyChanged(); }
        }
        private void SegnaModificato() => HaModifiche = true;

        private string _formNome = string.Empty;
        public string FormNome
        {
            get => _formNome;
            set { _formNome = value; OnPropertyChanged(); SegnaModificato(); }
        }

        /// <summary>Empty state della lista filtrata (diverso da NessunoStudio,
        /// che è la baseline "non esiste alcuno studio").</summary>
        public bool NessunRisultato => Studi.Count == 0;

        public StudiViewModel(INavigatore? navigatore = null)
        {
            _navigatore = navigatore;

            ApriFattureStudioCommand = new RelayCommand(() =>
            {
                PannelloAperto = false;
                _navigatore?.ApriFatture(null, _studioCorrenteNome);
            });
            ApriClientiStudioCommand = new RelayCommand(() =>
            {
                PannelloAperto = false;
                _navigatore?.ApriClienti(_studioCorrenteNome);
            });

            NuovoStudioCommand = new RelayCommand(NuovoStudio);
            ModificaStudioCommand = new RelayCommand<VoceStudio>(ModificaStudio);
            SalvaStudioCommand = new RelayCommand(SalvaStudio, () => !string.IsNullOrWhiteSpace(FormNome));
            AnnullaStudioCommand = new RelayCommand(() => PannelloAperto = false);
            EliminaStudioCommand = new RelayCommand<VoceStudio>(EliminaStudio);
            RiattivaStudioCommand = new RelayCommand<VoceStudio>(RiattivaStudio);
            ImpostaStatoFiltroCommand = new RelayCommand<string?>(s => FiltroStato = s ?? "Tutti");
            AzzeraFiltriCommand = new RelayCommand(() => { FiltroStato = "Tutti"; FiltroTesto = string.Empty; });

            Carica();
        }

        /// <summary>Apertura contestuale (es. dalla ricerca globale): apre lo
        /// stesso modal di modifica usato da doppio click e azione Modifica.</summary>
        public void ApriDettaglioPerReferente(int referenteId)
        {
            var studio = _studiCompleti.FirstOrDefault(s => s.Id == referenteId);
            if (studio != null)
                ModificaStudio(studio);
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
                                             && f.DataScadenza.Value.Date < oggi).Sum(f => f.Importo),
                    // Stato dello studio (priorità: Disabilitato → Ritardo → InAttesa → Regolare):
                    // fatture attive non pagate con scadenza superata vs senza scadenza superata.
                    InRitardo = mie.Count(f => f.DataPagamento == null
                                               && f.DataScadenza != null
                                               && f.DataScadenza.Value.Date < oggi),
                    InAttesa = mie.Count(f => f.DataPagamento == null
                                              && !(f.DataScadenza != null && f.DataScadenza.Value.Date < oggi))
                };
            }).ToList();

            ApplicaFiltro();
        }

        private void ApplicaFiltro()
        {
            var q = FiltroTesto.Trim().ToLower();
            var lista = _studiCompleti
                .Where(s => FiltroStato == "Tutti" || s.StatoStudio == FiltroStato)
                .Where(s => string.IsNullOrEmpty(q) || s.Nome.ToLower().Contains(q))
                .ToList();

            Studi.Clear();
            foreach (var s in lista)
                Studi.Add(s);

            ContatoreTesto = lista.Count == 1 ? "1 studio" : $"{lista.Count} studi";
            OnPropertyChanged(nameof(ContatoreTesto));
            OnPropertyChanged(nameof(NessunRisultato));
            OnPropertyChanged(nameof(NessunoStudio));
            OnPropertyChanged(nameof(EmptyStateTesto));
            OnPropertyChanged(nameof(EmptyStateSuggerimento));
        }

        /// <summary>Carica i dati mostrati nel modal: prime 5 fatture attive
        /// ordinate per data pagamento più recente e primi 5 clienti in ordine
        /// alfabetico (l'elenco completo resta nei moduli Fatture/Clienti).</summary>
        private void CaricaDatiModal(int referenteId, string nomeStudio)
        {
            using var db = new ClabDbContext();

            var annoCorrente = DateTime.Now.Year;

            var fatture = db.Fatture
                .AsNoTracking()
                .Where(f =>
                    f.ReferenteId == referenteId &&
                    !f.Annullata &&
                    (
                        f.DataPagamento == null ||
                        f.DataPagamento.Value.Year == annoCorrente
                    )
                )
                .OrderBy(f => f.DataPagamento == null ? 0 : 1)              // prima senza data pagamento
                .ThenByDescending(f => f.DataPagamento)                     // poi le pagate, dalla più recente
                .Take(5)
                .ToList();

            FattureStudio.Clear();
            foreach (var f in fatture)
                FattureStudio.Add(new VoceFatturaStudio
                {
                    Fattura = f,
                    ReferenteRagioneSociale = nomeStudio
                });
            OnPropertyChanged(nameof(HaFattureStudio));

            ClientiStudio.Clear();
            var clienti = db.Clienti.AsNoTracking()
                .Where(c => c.ReferenteId == referenteId)
                .OrderBy(c => c.RagioneSociale)
                .Take(5)
                .ToList();
            foreach (var c in clienti)
                ClientiStudio.Add(new VoceClienteStudio { Cliente = c });
            OnPropertyChanged(nameof(HaClientiStudio));
        }

        // --- Creazione / modifica / eliminazione studio ---
        //     Regole identiche a Clienti (referenti): nome unico campo
        //     obbligatorio e trimmato, delete fisico solo senza collegamenti,
        //     disattivazione con conferma (clienti → Cessato), riattivazione
        //     con eventuale riattivazione dei clienti Cessati.

        private void NuovoStudio()
        {
            _studioInModificaId = 0;
            TitoloPannello = "Nuovo studio";
            FormNome = string.Empty;
            _studioCorrenteNome = string.Empty;
            HaModifiche = false; // il caricamento non è una modifica dell'utente
            OnPropertyChanged(nameof(ModalMostraDati));
            PannelloAperto = true;
        }

        private void ModificaStudio(VoceStudio? studio)
        {
            if (studio == null) return;

            _studioInModificaId = studio.Id;
            TitoloPannello = "Modifica studio";
            FormNome = studio.Nome;
            _studioCorrenteNome = studio.Nome;
            HaModifiche = false;
            OnPropertyChanged(nameof(ModalMostraDati));
            CaricaDatiModal(studio.Id, studio.Nome);
            PannelloAperto = true;
        }

        private void SalvaStudio()
        {
            if (string.IsNullOrWhiteSpace(FormNome)) return;

            using var db = new ClabDbContext();

            if (_studioInModificaId == 0)
            {
                db.Referenti.Add(new Referente { Nome = FormNome.Trim(), Attivo = true });
            }
            else
            {
                var entita = db.Referenti.First(x => x.Id == _studioInModificaId);
                entita.Nome = FormNome.Trim();
            }

            db.SaveChanges();
            PannelloAperto = false;
            RicaricaDopoAzione(_studioInModificaId);
        }

        /// <summary>Stessa regola di EliminaReferente: se lo studio non è
        /// collegato a nulla (clienti, ToDo, fatture) viene eliminato davvero;
        /// altrimenti chiede conferma e lo disattiva (clienti → Cessato).
        /// Le fatture intestate restano: la disattivazione non cancella dati.</summary>
        private void EliminaStudio(VoceStudio? studio)
        {
            if (studio == null) return;

            using var db = new ClabDbContext();
            int numeroClienti = db.Clienti.Count(c => c.ReferenteId == studio.Id);
            int numeroToDo = db.ToDo.Count(t => t.ReferenteId == studio.Id);
            int numeroFatture = db.Fatture.Count(f => f.ReferenteId == studio.Id);

            if (numeroClienti == 0 && numeroToDo == 0 && numeroFatture == 0)
            {
                var entita = db.Referenti.First(x => x.Id == studio.Id);
                db.Referenti.Remove(entita);
                db.SaveChanges();
                RicaricaDopoAzione(studio.Id);
                return;
            }

            var esito = MessageBox.Show(
                $"\"{studio.Nome}\" è collegato a {numeroClienti} client{(numeroClienti == 1 ? "e" : "i")}" +
                (numeroFatture > 0
                    ? $" e ha {numeroFatture} fattur{(numeroFatture == 1 ? "a" : "e")} intestat{(numeroFatture == 1 ? "a" : "e")}"
                    : "") + ".\n" +
                "Non può essere eliminato: verrà disattivato (sparirà dalla tendina) e " +
                $"{(numeroClienti == 1 ? "il cliente collegato passerà" : "i clienti collegati passeranno")} in stato Cessato.\nContinuare?",
                "Conferma disattivazione", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (esito != MessageBoxResult.Yes) return;

            var entitaRef = db.Referenti.First(x => x.Id == studio.Id);
            entitaRef.Attivo = false;

            foreach (var c in db.Clienti.Where(c => c.ReferenteId == studio.Id).ToList())
                c.Stato = StatoCliente.Cessato;

            db.SaveChanges();
            RicaricaDopoAzione(studio.Id);
        }

        /// <summary>Stessa regola di RiattivaReferente: riattiva lo studio e, se
        /// ci sono clienti Cessati (probabilmente per la disattivazione precedente),
        /// chiede se riportarli in stato Attivo.</summary>
        private void RiattivaStudio(VoceStudio? studio)
        {
            if (studio == null) return;

            using var db = new ClabDbContext();
            var entita = db.Referenti.First(x => x.Id == studio.Id);
            entita.Attivo = true;
            db.SaveChanges();

            var clientiCessati = db.Clienti.Where(c => c.ReferenteId == studio.Id && c.Stato == StatoCliente.Cessato).ToList();

            if (clientiCessati.Count > 0)
            {
                var esito = MessageBox.Show(
                    $"\"{studio.Nome}\" è di nuovo attivo. Ci sono {clientiCessati.Count} client{(clientiCessati.Count == 1 ? "e" : "i")} " +
                    "attualmente Cessati (probabilmente a causa della precedente disattivazione).\nVuoi riportarli in stato Attivo?",
                    "Riattivare anche i clienti?", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (esito == MessageBoxResult.Yes)
                {
                    foreach (var c in clientiCessati) c.Stato = StatoCliente.Attivo;
                    db.SaveChanges();
                }
            }

            RicaricaDopoAzione(studio.Id);
        }

        /// <summary>Ricarica aggregati e lista dopo ogni azione di salvataggio
        /// o eliminazione/riattivazione (il modal è già chiuso in quel punto).</summary>
        private void RicaricaDopoAzione(int studioId)
        {
            Carica();
        }
    }


    /// <summary>Riga della lista Studi (FASE 5): aggregati economici dello studio.</summary>
    public class VoceStudio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Attivo { get; set; } = true;

        /// <summary>Fatture attive non pagate con scadenza superata.</summary>
        public int InRitardo { get; set; }

        /// <summary>Fatture attive non pagate senza scadenza superata.</summary>
        public int InAttesa { get; set; }

        /// <summary>Stato per il badge unificato (chiavi di StatoSemantico), in
        /// ordine di priorità: Disabilitato (manuale) → Ritardo → InAttesa → Regolare.</summary>
        public string StatoStudio =>
            !Attivo ? "Disabilitato"
            : InRitardo > 0 ? "Ritardo"
            : InAttesa > 0 ? "InAttesa"
            : "Regolare";

        public int NumeroClienti { get; set; }
        public decimal Fatturato { get; set; }
        public decimal DaIncassare { get; set; }
        public decimal Scaduto { get; set; }

        public bool HaScaduto => Scaduto > 0;
        public bool InRitardoVisibile => InRitardo > 0;

        public string InRitardoTesto => InRitardo == 1 ? "1 in ritardo" : $"{InRitardo} in ritardo";
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
