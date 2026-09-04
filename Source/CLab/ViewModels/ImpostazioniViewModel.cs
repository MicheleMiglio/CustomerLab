using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
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

        public ICommand ApriCartellaDatiCommand { get; }
        public ICommand EseguiBackupCommand { get; }
        public ICommand EliminaDatiCommand { get; }

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

        private static void EliminaFileSeEsiste(string percorso)
        {
            if (File.Exists(percorso))
                File.Delete(percorso);
        }
    }
}