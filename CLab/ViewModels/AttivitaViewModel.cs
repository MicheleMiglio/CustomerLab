using CLab.Data;
using CLab.Models;
using CLab.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public class AttivitaViewModel : ViewModelBase
    {
        public ObservableCollection<Attivita> Elenco { get; set; } = new();

        public ObservableCollection<Attivita> ElencoFiltrato { get; set; } = new();

        private string _filtroCatalogoTesto = string.Empty;
        public string FiltroCatalogoTesto
        {
            get => _filtroCatalogoTesto;
            set
            {
                _filtroCatalogoTesto = value;
                OnPropertyChanged();
                ApplicaFiltroCatalogo();
            }
        }

        private bool _pannelloAperto;
        public bool PannelloAperto
        {
            get => _pannelloAperto;
            set { _pannelloAperto = value; OnPropertyChanged(); }
        }

        private int _attivitaInModificaId;

        private string _formNome = string.Empty;
        public string FormNome
        {
            get => _formNome;
            set { _formNome = value; OnPropertyChanged(); }
        }

        private Periodicita _formPeriodicita = Periodicita.Mensile;
        public Periodicita FormPeriodicita
        {
            get => _formPeriodicita;
            set { _formPeriodicita = value; OnPropertyChanged(); }
        }

        private TipoCampoAttivita _formTipoCampo = TipoCampoAttivita.SiNo;
        public TipoCampoAttivita FormTipoCampo
        {
            get => _formTipoCampo;
            set { _formTipoCampo = value; OnPropertyChanged(); }
        }

        private int? _formTestoLunghezzaMassima = 200;
        public int? FormTestoLunghezzaMassima
        {
            get => _formTestoLunghezzaMassima;
            set { _formTestoLunghezzaMassima = value; OnPropertyChanged(); }
        }

        private bool _formNumeroEImporto;
        public bool FormNumeroEImporto
        {
            get => _formNumeroEImporto;
            set { _formNumeroEImporto = value; OnPropertyChanged(); }
        }

        private bool _formTendinaRichiedeImporto;
        public bool FormTendinaRichiedeImporto
        {
            get => _formTendinaRichiedeImporto;
            set { _formTendinaRichiedeImporto = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> FormOpzioni { get; set; } = new();

        private string _formNuovaOpzione = string.Empty;
        public string FormNuovaOpzione
        {
            get => _formNuovaOpzione;
            set { _formNuovaOpzione = value; OnPropertyChanged(); }
        }

        // --- Comandi ---

        public ICommand NuovoCommand { get; }
        public ICommand ModificaCommand { get; }
        public ICommand SalvaCommand { get; }
        public ICommand AnnullaCommand { get; }
        public ICommand EliminaCommand { get; }
        public ICommand AggiungiOpzioneCommand { get; }
        public ICommand RimuoviOpzioneCommand { get; }

        public AttivitaViewModel()
        {
            NuovoCommand = new RelayCommand(Nuovo);
            ModificaCommand = new RelayCommand<Attivita>(Modifica);
            SalvaCommand = new RelayCommand(Salva);
            AnnullaCommand = new RelayCommand(Annulla);
            EliminaCommand = new RelayCommand<Attivita>(Elimina);
            AggiungiOpzioneCommand = new RelayCommand(AggiungiOpzione);
            RimuoviOpzioneCommand = new RelayCommand<string>(RimuoviOpzione);
            MostraCatalogoCommand = new RelayCommand(MostraCatalogo);
            MostraConfigurazioneCommand = new RelayCommand(MostraConfigurazione);

            CaricaElenco();
        }

        private void CaricaElenco()
        {
            using var db = new ClabDbContext();

            Elenco.Clear();
            foreach (var a in db.Attivita.OrderBy(x => x.Nome).ToList())
                Elenco.Add(a);

            ApplicaFiltroCatalogo();
        }

        private void ApplicaFiltroCatalogo()
        {
            ElencoFiltrato.Clear();

            var filtrate = string.IsNullOrWhiteSpace(FiltroCatalogoTesto)
                ? Elenco
                : Elenco.Where(a => a.Nome.Contains(FiltroCatalogoTesto, StringComparison.OrdinalIgnoreCase));

            foreach (var a in filtrate)
                ElencoFiltrato.Add(a);
        }

        private void Nuovo()
        {
            _attivitaInModificaId = 0;
            FormNome = string.Empty;
            FormPeriodicita = Periodicita.Mensile;
            FormTipoCampo = TipoCampoAttivita.SiNo;
            FormTestoLunghezzaMassima = 200;
            FormNumeroEImporto = false;
            FormTendinaRichiedeImporto = false;
            FormOpzioni.Clear();
            FormNuovaOpzione = string.Empty;

            PannelloAperto = true;
        }

        private void Modifica(Attivita? a)
        {
            if (a == null) return;

            using var db = new ClabDbContext();
            var opzioni = db.OpzioniAttivita
                             .Where(o => o.AttivitaId == a.Id)
                             .OrderBy(o => o.Ordine)
                             .Select(o => o.Testo)
                             .ToList();

            _attivitaInModificaId = a.Id;
            FormNome = a.Nome;
            FormPeriodicita = a.Periodicita;
            FormTipoCampo = a.TipoCampo;
            FormTestoLunghezzaMassima = a.TestoLunghezzaMassima ?? 200;
            FormNumeroEImporto = a.NumeroEImporto;
            FormTendinaRichiedeImporto = a.TendinaRichiedeImporto;
            FormOpzioni.Clear();
            foreach (var o in opzioni) FormOpzioni.Add(o);
            FormNuovaOpzione = string.Empty;

            PannelloAperto = true;
        }

        private void AggiungiOpzione()
        {
            if (string.IsNullOrWhiteSpace(FormNuovaOpzione)) return;

            FormOpzioni.Add(FormNuovaOpzione.Trim());
            FormNuovaOpzione = string.Empty;
        }

        private void RimuoviOpzione(string? opzione)
        {
            if (opzione == null) return;
            FormOpzioni.Remove(opzione);
        }

        private void Salva()
        {
            if (string.IsNullOrWhiteSpace(FormNome))
            {
                MessageBox.Show("Il nome dell'attività è obbligatorio.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (FormTipoCampo == TipoCampoAttivita.Tendina && FormOpzioni.Count < 2)
            {
                MessageBox.Show("Una tendina richiede almeno due opzioni.", "Attenzione",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new ClabDbContext();

            Attivita entita;

            if (_attivitaInModificaId == 0)
            {
                entita = new Attivita();
                db.Attivita.Add(entita);
            }
            else
            {
                entita = db.Attivita.First(a => a.Id == _attivitaInModificaId);

                // Rimuoviamo le vecchie opzioni: le riscriviamo da capo con quelle del form
                var vecchieOpzioni = db.OpzioniAttivita.Where(o => o.AttivitaId == entita.Id);
                db.OpzioniAttivita.RemoveRange(vecchieOpzioni);
            }

            entita.Nome = FormNome.Trim();
            entita.Periodicita = FormPeriodicita;
            entita.TipoCampo = FormTipoCampo;
            entita.TestoLunghezzaMassima = FormTipoCampo == TipoCampoAttivita.TestoLibero
                ? FormTestoLunghezzaMassima : null;
            entita.NumeroEImporto = FormTipoCampo == TipoCampoAttivita.Numero && FormNumeroEImporto;
            entita.TendinaRichiedeImporto = FormTipoCampo == TipoCampoAttivita.Tendina && FormTendinaRichiedeImporto;

            db.SaveChanges();

            if (FormTipoCampo == TipoCampoAttivita.Tendina)
            {
                int ordine = 0;
                foreach (var testo in FormOpzioni)
                {
                    db.OpzioniAttivita.Add(new AttivitaOpzione
                    {
                        AttivitaId = entita.Id,
                        Testo = testo,
                        Ordine = ordine++
                    });
                }
                db.SaveChanges();
            }

            PannelloAperto = false;
            CaricaElenco();

            if (ConfigurazioneAttiva)
            {
                CaricaMatrice();
                OnPropertyChanged(nameof(ConfigurazioneAttiva));
            }
        }

        private void Annulla()
        {
            PannelloAperto = false;
        }

        private void Elimina(Attivita? a)
        {
            if (a == null) return;

            using var db = new ClabDbContext();

            int clientiCoinvolti = db.ClientiAttivita.Count(ca => ca.AttivitaId == a.Id);
            int compilazioniCoinvolte = db.Compilazioni.Count(c => c.AttivitaId == a.Id);

            string messaggio = $"Eliminando \"{a.Nome}\" dal catalogo:\n" +
                $"• Clienti interessati: {clientiCoinvolti}\n" +
                $"• Compilazioni che verranno cancellate: {compilazioniCoinvolte}\n" +
                "Continuare?";

            var esito = MessageBox.Show(messaggio, "Conferma eliminazione",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (esito != MessageBoxResult.Yes) return;

            var entita = db.Attivita.First(x => x.Id == a.Id);
            db.Attivita.Remove(entita);
            db.SaveChanges();

            CaricaElenco();

            if (ConfigurazioneAttiva)
            {
                CaricaMatrice();
                OnPropertyChanged(nameof(ConfigurazioneAttiva));
            }
        }

        // --- Configurazione (matrice Clienti × Attività) ---

        private bool _configurazioneAttiva;
        public bool ConfigurazioneAttiva
        {
            get => _configurazioneAttiva;
            set { _configurazioneAttiva = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RigaMatriceCliente> Matrice { get; set; } = new();

        public ICommand MostraCatalogoCommand { get; }
        public ICommand MostraConfigurazioneCommand { get; }

        private void MostraCatalogo()
        {
            ConfigurazioneAttiva = false;
        }

        private void MostraConfigurazione()
        {
            CaricaMatrice();
            ConfigurazioneAttiva = true;
        }

        private List<RigaMatriceCliente> _matriceCompleta = new();

        private string _filtroMatriceTesto = string.Empty;
        public string FiltroMatriceTesto
        {
            get => _filtroMatriceTesto;
            set
            {
                _filtroMatriceTesto = value;
                OnPropertyChanged();
                ApplicaFiltroMatrice();
            }
        }

        private void CaricaMatrice()
        {
            using var db = new ClabDbContext();

            var clienti = db.Clienti.OrderBy(c => c.RagioneSociale).ToList();
            var assegnazioni = db.ClientiAttivita.ToList();

            _matriceCompleta = new List<RigaMatriceCliente>();

            foreach (var cliente in clienti)
            {
                var riga = new RigaMatriceCliente
                {
                    ClienteId = cliente.Id,
                    RagioneSociale = cliente.RagioneSociale,
                    PartitaIva = cliente.PartitaIva
                };

                foreach (var att in Elenco)
                {
                    bool selezionata = assegnazioni.Any(a => a.ClienteId == cliente.Id && a.AttivitaId == att.Id);

                    var cella = new CellaMatrice
                    {
                        ClienteId = cliente.Id,
                        AttivitaId = att.Id,
                        Selezionata = selezionata
                    };
                    cella.OnCambiata = CellaMatriceCambiata;
                    riga.Celle.Add(cella);
                }

                _matriceCompleta.Add(riga);
            }

            ApplicaFiltroMatrice();
        }

        private void ApplicaFiltroMatrice()
        {
            Matrice.Clear();

            var filtrate = string.IsNullOrWhiteSpace(FiltroMatriceTesto)
                ? _matriceCompleta
                : _matriceCompleta.Where(r =>
                    r.RagioneSociale.Contains(FiltroMatriceTesto, StringComparison.OrdinalIgnoreCase) ||
                    (r.PartitaIva != null && r.PartitaIva.Contains(FiltroMatriceTesto, StringComparison.OrdinalIgnoreCase)));

            foreach (var riga in filtrate)
                Matrice.Add(riga);
        }

        private void CellaMatriceCambiata(CellaMatrice cella)
        {
            using var db = new ClabDbContext();

            if (cella.Selezionata)
            {
                bool esiste = db.ClientiAttivita.Any(a => a.ClienteId == cella.ClienteId && a.AttivitaId == cella.AttivitaId);
                if (!esiste)
                {
                    db.ClientiAttivita.Add(new ClienteAttivita
                    {
                        ClienteId = cella.ClienteId,
                        AttivitaId = cella.AttivitaId
                    });
                    db.SaveChanges();
                }
                return;
            }

            // Tolta la spunta: chiediamo SEMPRE conferma, il messaggio si
            // arricchisce solo se ci sono davvero compilazioni da perdere.
            int compilazioniCoinvolte = db.Compilazioni.Count(c => c.ClienteId == cella.ClienteId && c.AttivitaId == cella.AttivitaId);

            string nomeAttivita = Elenco.First(a => a.Id == cella.AttivitaId).Nome;
            string nomeCliente = _matriceCompleta.First(r => r.ClienteId == cella.ClienteId).RagioneSociale;

            string messaggio = compilazioniCoinvolte > 0
                ? $"Ci sono {compilazioniCoinvolte} compilazion{(compilazioniCoinvolte == 1 ? "e" : "i")} registrat{(compilazioniCoinvolte == 1 ? "a" : "e")} " +
                  $"per \"{nomeAttivita}\" su {nomeCliente}.\nRimuovendola, verranno eliminate definitivamente. Continuare?"
                : $"Rimuovere \"{nomeAttivita}\" da {nomeCliente}?";

            var esito = MessageBox.Show(messaggio, "Conferma rimozione", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (esito != MessageBoxResult.Yes)
            {
                cella.OnCambiata = null;
                cella.Selezionata = true;
                cella.OnCambiata = CellaMatriceCambiata;
                return;
            }

            if (compilazioniCoinvolte > 0)
            {
                db.Compilazioni.RemoveRange(
                    db.Compilazioni.Where(c => c.ClienteId == cella.ClienteId && c.AttivitaId == cella.AttivitaId));
            }

            var esistente = db.ClientiAttivita.FirstOrDefault(a => a.ClienteId == cella.ClienteId && a.AttivitaId == cella.AttivitaId);
            if (esistente != null)
                db.ClientiAttivita.Remove(esistente);

            db.SaveChanges();
        }
    }

    public class RigaMatriceCliente
    {
        public int ClienteId { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public string? PartitaIva { get; set; }
        public ObservableCollection<CellaMatrice> Celle { get; set; } = new();
    }

    public class CellaMatrice : ViewModelBase
    {
        public int ClienteId { get; set; }
        public int AttivitaId { get; set; }

        private bool _selezionata;
        public bool Selezionata
        {
            get => _selezionata;
            set
            {
                if (_selezionata == value) return;
                _selezionata = value;
                OnPropertyChanged();
                OnCambiata?.Invoke(this);
            }
        }

        public Action<CellaMatrice>? OnCambiata;
    }
}