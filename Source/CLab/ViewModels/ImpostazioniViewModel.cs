using CLab.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public enum PasswordMessaggioTipo
    {
        Errore,
        Successo,
        Informazione
    }

    public class ImpostazioniViewModel : ViewModelBase
    {
        private const string TestoConfermaRichiesto = "ELIMINA";

        public string Versione { get; }

        public string PercorsoDatabase { get; }

        public string DimensioneDatabaseTesto { get; }

        private string _testoConferma = string.Empty;

        public string TestoConferma
        {
            get => _testoConferma;
            set
            {
                if (_testoConferma == value)
                    return;

                _testoConferma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PuoEliminare));
            }
        }

        public bool PuoEliminare => TestoConferma.Trim().Equals(TestoConfermaRichiesto, StringComparison.Ordinal);
        public bool MostraResetConfigurazioneTest => true;

        public ICommand ApriCartellaDatiCommand { get; }
        public ICommand EseguiBackupCommand { get; }
        public ICommand EliminaDatiCommand { get; }
        public ICommand ResetConfigurazionePerTestCommand { get; }

        public ImpostazioniViewModel()
        {
            var versioneAssembly = Assembly.GetExecutingAssembly().GetName().Version;
            Versione = versioneAssembly != null
                ? $"{versioneAssembly.Major}.{versioneAssembly.Minor}.{versioneAssembly.Build}"
                : "n/d";

            PercorsoDatabase = CLab.Data.ClabDbContext.PercorsoDatabase;

            DimensioneDatabaseTesto = CalcolaDimensioneTesto(PercorsoDatabase);

            ApriCartellaDatiCommand = new RelayCommand(ApriCartellaDati);
            EseguiBackupCommand = new RelayCommand(EseguiBackup);
            EliminaDatiCommand = new RelayCommand(EliminaDati, () => PuoEliminare);

            BloccaPasswordCommand = new RelayCommand(BloccaPassword, () => PasswordSbloccato);
            ReimpostaPasswordCommand = new RelayCommand(ReimpostaPassword, () => PasswordConfigurato);

            ResetConfigurazionePerTestCommand = new RelayCommand(ResetConfigurazionePerTest);

            AggiornaStatoPassword();
        }

        // ============================================================
        // MODULO PASSWORD (integrazione): stato, lock manuale e
        // recupero della password principale. La logica crittografica
        // resta nei servizi del modulo Password: qui c'è solo il punto
        // di accesso, che usa gli stessi flussi del modulo (nessuna
        // duplicazione di codice sensibile).
        // ============================================================

        private TextBox? _codiceRecupero;
        private PasswordBox? _nuovaMaster;
        private PasswordBox? _nuovaMasterConferma;

        /// <summary>
        /// Collega i campi segreti del recupero: i PasswordBox non sono
        /// bindabili, quindi li aggancia il code-behind quando l'albero
        /// visuale è pronto.
        /// </summary>
        public void CollegaControlliPassword(
            TextBox? codiceRecupero,
            PasswordBox? nuovaMaster,
            PasswordBox? nuovaMasterConferma)
        {
            _codiceRecupero = codiceRecupero;
            _nuovaMaster = nuovaMaster;
            _nuovaMasterConferma = nuovaMasterConferma;
        }

        private string _passwordStato = string.Empty;

        /// <summary>Stato del modulo Password mostrato in Impostazioni.</summary>
        public string PasswordStato
        {
            get => _passwordStato;
            private set { _passwordStato = value; OnPropertyChanged(); }
        }

        private bool _passwordConfigurato;

        public bool PasswordConfigurato
        {
            get => _passwordConfigurato;
            private set
            {
                _passwordConfigurato = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordNonConfigurato));
            }
        }

        /// <summary>True se il modulo Password non è mai stato configurato.</summary>
        public bool PasswordNonConfigurato => !PasswordConfigurato;

        private bool _passwordSbloccato;

        /// <summary>True se c'è una sessione Password aperta (chiave dati in memoria).</summary>
        public bool PasswordSbloccato
        {
            get => _passwordSbloccato;
            private set { _passwordSbloccato = value; OnPropertyChanged(); }
        }

        private string _passwordMessaggio = string.Empty;

        /// <summary>Esito dell'ultima operazione sul modulo Password.</summary>
        public string PasswordMessaggio
        {
            get => _passwordMessaggio;
            private set { _passwordMessaggio = value; OnPropertyChanged(); }
        }

        private PasswordMessaggioTipo _passwordMessaggioTipo = PasswordMessaggioTipo.Informazione;

        public PasswordMessaggioTipo PasswordMessaggioTipo
        {
            get => _passwordMessaggioTipo;
            private set
            {
                _passwordMessaggioTipo = value;
                OnPropertyChanged();
            }
        }

        private void ImpostaPasswordMessaggio(string messaggio, PasswordMessaggioTipo tipo)
        {
            PasswordMessaggioTipo = tipo;
            PasswordMessaggio = messaggio;
        }

        public ICommand BloccaPasswordCommand { get; }
        public ICommand ReimpostaPasswordCommand { get; }

        /// <summary>
        /// Rilegge lo stato reale del modulo Password: configurazione salvata e
        /// validità della sessione (che dipende dal timeout, non dalla UI).
        /// </summary>
        public void AggiornaStatoPassword()
        {
            PasswordConfigurato = PasswordAccessService.IsConfigurato();
            PasswordSbloccato = PasswordSessionService.Istanza.SessioneValida();

            PasswordStato = PasswordConfigurato
                ? (PasswordSbloccato ? "Configurato · sessione aperta" : "Configurato · sessione bloccata")
                : "Non configurato";
        }

        /// <summary>Lock manuale: invalida la sessione e azzera la chiave dati.</summary>
        private void BloccaPassword()
        {
            PasswordSessionService.Istanza.Lock();
            ImpostaPasswordMessaggio(
    "Modulo bloccato.",
    PasswordMessaggioTipo.Informazione);
            AggiornaStatoPassword();
        }

        /// <summary>
        /// Recupero: riottiene la chiave dati con il codice di recupero e ne
        /// rinegozia la protezione con la nuova password principale. Le voci
        /// salvate non vengono ricifrate e il codice di recupero resta valido.
        /// </summary>
        public bool ProvaRecuperoPassword(string codiceRecupero, string nuovaMaster, string conferma)
        {
            if (!PasswordConfigurato)
            {
                ImpostaPasswordMessaggio(
        "Il modulo Password non è ancora configurato.",
        PasswordMessaggioTipo.Errore);

                return false;
            }

            if (string.IsNullOrWhiteSpace(codiceRecupero))
            {
                ImpostaPasswordMessaggio(
        "Inserisci il codice di recupero.",
        PasswordMessaggioTipo.Errore);
                return false;
            }

            if (string.IsNullOrEmpty(nuovaMaster) || nuovaMaster.Length < 8)
            {
                ImpostaPasswordMessaggio(
        "La nuova password principale deve avere almeno 8 caratteri.",
        PasswordMessaggioTipo.Errore);
                return false;
            }

            if (nuovaMaster != conferma)
            {
                ImpostaPasswordMessaggio(
    "Le due password non coincidono.",
    PasswordMessaggioTipo.Errore);
                return false;
            }

            if (!PasswordAccessService.RecuperaConRecovery(codiceRecupero, nuovaMaster))
            {
                ImpostaPasswordMessaggio(
    "Codice di recupero non valido.",
    PasswordMessaggioTipo.Errore);
                return false;
            }

            ImpostaPasswordMessaggio(
    "Password principale reimpostata: il modulo Password si sblocca con la nuova password.",
    PasswordMessaggioTipo.Successo);
            AggiornaStatoPassword();
            return true;
        }

        private void ReimpostaPassword()
        {
            if (_codiceRecupero == null || _nuovaMaster == null || _nuovaMasterConferma == null)
                return;

            ProvaRecuperoPassword(_codiceRecupero.Text, _nuovaMaster.Password, _nuovaMasterConferma.Password);

            _codiceRecupero.Clear();
            _nuovaMaster.Clear();
            _nuovaMasterConferma.Clear();
        }

        private static string CalcolaDimensioneTesto(string percorso)
        {
            if (!File.Exists(percorso))
                return "n/d";

            var bytes = new FileInfo(percorso).Length;
            return bytes < 1024 * 1024
                ? $"{bytes / 1024.0:0.#} KB"
                : $"{bytes / 1024.0 / 1024.0:0.#} MB";
        }

        private void ApriCartellaDati()
        {
            var cartella = Path.GetDirectoryName(PercorsoDatabase);
            if (cartella == null)
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{PercorsoDatabase}\"",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// FASE 9 — Backup manuale del database. Usa l'API di backup di SQLite
        /// (SqliteConnection.BackupDatabase, già disponibile con Microsoft.Data.Sqlite
        /// usato dal progetto): copia coerente anche con connessioni attive e journaling,
        /// senza modificare il database originale né chiudere l'applicazione.
        /// </summary>
        private void EseguiBackup()
        {
            var dialogo = new SaveFileDialog
            {
                Title = "Esegui backup del database CLab",
                FileName = $"clab_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                DefaultExt = ".db",
                Filter = "Database CLab (*.db)|*.db|Tutti i file (*.*)|*.*",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialogo.ShowDialog() != true)
                return;

            try
            {
                using var origine = new SqliteConnection($"Data Source={PercorsoDatabase}");
                origine.Open();

                using var destinazione = new SqliteConnection($"Data Source={dialogo.FileName}");
                destinazione.Open();

                origine.BackupDatabase(destinazione);

                MessageBox.Show(
                    "Backup completato correttamente.",
                    "Backup database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Impossibile completare il backup. Verifica di avere i permessi di scrittura nella cartella selezionata.\n\n" +
                    $"Dettaglio: {ex.Message}",
                    "Backup database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EliminaDati()
        {
            if (!PuoEliminare)
                return;

            var conferma = MessageBox.Show(
                "Stai per eliminare definitivamente TUTTI i dati: clienti, referenti, programmi, attività, scadenze e fatture.\n\n" +
                "L'operazione non è reversibile. Continuare?",
                "Elimina tutti i dati",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (conferma != MessageBoxResult.Yes)
                return;

            try
            {
                SqliteConnection.ClearAllPools();

                EliminaFileSeEsiste(PercorsoDatabase);
                EliminaFileSeEsiste(PercorsoDatabase + "-wal");
                EliminaFileSeEsiste(PercorsoDatabase + "-shm");
            }
            catch (IOException)
            {
                MessageBox.Show(
                    "Non è stato possibile eliminare i dati perché il file è in uso. Chiudi eventuali pannelli aperti e riprova.",
                    "Elimina tutti i dati",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(
                "Dati eliminati. L'applicazione verrà chiusa: riaprila per continuare con un archivio vuoto.",
                "Elimina tutti i dati",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Application.Current.Shutdown();
        }

        private void ResetConfigurazionePerTest()
        {
            var esito = MessageBox.Show(
                "ATTENZIONE: verranno eliminate tutte le credenziali, password e la configurazione del modulo.\n\n" +
                "L'operazione non è reversibile.\n\n" +
                "Continuare?",
                "Reset modulo Password",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (esito != MessageBoxResult.Yes)
                return;

            PasswordAccessService.ResetConfigurazionePerTest();
        }

        private static void EliminaFileSeEsiste(string percorso)
        {
            if (File.Exists(percorso))
                File.Delete(percorso);
        }
    }
}