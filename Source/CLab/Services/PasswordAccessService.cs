using CLab.Data;
using CLab.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace CLab.Services
{
    /// <summary>
    /// Flussi di accesso del modulo Password verso il database: verifica
    /// configurazione, configurazione iniziale (creazione DEK + doppia
    /// protezione + salvataggio), sblocco con Master Password e recovery.
    ///
    /// È un servizio specifico del modulo (non generico): incapsula la
    /// persistenza di PasswordConfig in modo che gli stessi flussi siano
    /// riusabili dal modulo Password e dal pulsante di recovery in
    /// Impostazioni, senza duplicare logica crittografica sensibile.
    ///
    /// Master Password e Recovery Code esistono solo come parametri di
    /// metodo: mai in proprietà, mai in log, mai nel database.
    /// </summary>
    public static class PasswordAccessService
    {
        /// <summary>True se il modulo Password è già stato configurato (esiste la riga PasswordConfig).</summary>
        public static bool IsConfigurato()
        {
            using var db = new ClabDbContext();
            return db.PasswordConfig.AsNoTracking().Any();
        }

        /// <summary>Carica la configurazione (null se non ancora configurato).</summary>
        public static PasswordConfig? CaricaConfig()
        {
            using var db = new ClabDbContext();
            return db.PasswordConfig.AsNoTracking().FirstOrDefault();
        }

        /// <summary>
        /// Configurazione iniziale: crea DEK, doppia protezione (Master + Recovery),
        /// salva PasswordConfig e restituisce il Recovery Code da mostrare
        /// UNA SOLA VOLTA. Non salva il Recovery Code da nessuna parte.
        /// Ritorna false se il modulo è già configurato.
        /// </summary>
        public static bool ConfiguraIniziale(string masterPassword, out string? recoveryCode)
        {
            recoveryCode = null;

            if (IsConfigurato())
                return false;

            var (config, codice) = PasswordCryptoService.CreaConfigurazione(masterPassword);

            using var db = new ClabDbContext();
            db.PasswordConfig.Add(config);
            db.SaveChanges();

            // Il codice torna SOLO al chiamante per la visualizzazione singola:
            // qui non viene memorizzato in alcuna proprietà statica.
            recoveryCode = codice;
            return true;
        }

        /// <summary>
        /// Sblocco con Master Password: deriva la KEK, prova a decifrare la DEK.
        /// In caso di successo la DEK va consegnata alla sessione dal chiamante.
        /// </summary>
        public static bool SbloccaConMaster(PasswordConfig config, string masterPassword, out byte[]? dek)
        {
            return PasswordCryptoService.ProvaSbloccoConMaster(config, masterPassword, out dek);
        }

        /// <summary>
        /// Recovery: recupera la STESSA DEK con il Recovery Code, rinegozia
        /// solo la protezione Master (nuovo salt + nuova Master KEK) e salva.
        /// Non modifica i blob Recovery e non tocca le credenziali esistenti.
        /// </summary>
        public static bool RecuperaConRecovery(string recoveryCode, string nuovaMasterPassword)
        {
            var config = CaricaConfig();
            if (config == null)
                return false;

            var normalizzato = PasswordCryptoService.NormalizzaRecoveryCode(recoveryCode);

            // Traccia il recupero su un'istanza staccata e applica la modifica
            // su una riga attached, per non portare lo stato di tracking nella KDF.
            var nuovaMaster = nuovaMasterPassword;
            return PasswordCryptoService.RecuperaConRecovery(config, normalizzato, nuovaMaster, aggiornata =>
            {
                using var db = new ClabDbContext();
                var riga = db.PasswordConfig.First();
                riga.MasterSalt = aggiornata.MasterSalt;
                riga.DekCifrataMaster = aggiornata.DekCifrataMaster;
                riga.DekNonceMaster = aggiornata.DekNonceMaster;
                riga.DekTagMaster = aggiornata.DekTagMaster;
                db.SaveChanges();
            });
        }
    }
}