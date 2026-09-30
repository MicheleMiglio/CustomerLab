using CLab.Models;
using System;
using System.Security.Cryptography;
using System.Text.Json;

namespace CLab.Services
{
    /// <summary>
    /// Livello crittografico del modulo Password. Architettura approvata:
    ///
    ///   Master Password → PBKDF2-HMAC-SHA256 → Master KEK   → AES-GCM → DEK
    ///   Recovery Code   → PBKDF2-HMAC-SHA256 → Recovery KEK → AES-GCM → DEK
    ///   DEK             → AES-256-GCM       → payload credenziali
    ///
    /// Due protezioni indipendenti della STESSA DEK: cambiare/resettae la
    /// Master Password (recovery) significa solo rinegoziare la protezione
    /// Master della DEK, senza mai ricifrare le credenziali.
    ///
    /// Master Password, Recovery Code e DEK non vengono mai persistiti in
    /// chiaro né scritti nei log. Nessuna dipendenza esterna: solo API
    /// System.Security.Cryptography.
    /// </summary>
    public static class PasswordCryptoService
    {
        /// <summary>Iterazioni PBKDF2-HMAC-SHA256 (raccomandazione OWASP).</summary>
        public const int IterazioniPbkdf2Default = 600_000;

        private const int DimensioneChiave = 32;   // 256 bit (DEK / KEK)
        private const int DimensioneNonce = 12;    // 96 bit (standard GCM)
        private const int DimensioneTag = 16;      // 128 bit
        private const int DimensioneSalt = 16;     // 128 bit

        // --- Derivazione KEK ---

        /// <summary>Deriva una KEK di 32 byte da un segreto (Master Password o Recovery Code).</summary>
        public static byte[] DerivaKek(string segreto, byte[] salt, int iterazioni)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                segreto, salt, iterazioni, HashAlgorithmName.SHA256, DimensioneChiave);
        }

        /// <summary>Azzera un buffer di chiave (rendendolo inutilizzabile).</summary>
        public static void Azzera(byte[]? chiave)
        {
            if (chiave != null)
                CryptographicOperations.ZeroMemory(chiave);
        }

        // --- AES-256-GCM ---

        /// <summary>
        /// Cifra i dati con AES-256-GCM: nonce casuale nuovo a ogni chiamata.
        /// Restituisce ciphertext, nonce e tag separati (come voluti in DB).
        /// </summary>
        public static (byte[] Cifrato, byte[] Nonce, byte[] Tag) Cifra(byte[] datiChiaro, byte[] chiave)
        {
            if (chiave.Length != DimensioneChiave)
                throw new ArgumentException("La chiave deve essere di 32 byte.", nameof(chiave));

            var nonce = RandomNumberGenerator.GetBytes(DimensioneNonce);
            var cifrato = new byte[datiChiaro.Length];
            var tag = new byte[DimensioneTag];

            using var aes = new AesGcm(chiave, DimensioneTag);
            aes.Encrypt(nonce, datiChiaro, cifrato, tag);

            return (cifrato, nonce, tag);
        }

        /// <summary>
        /// Decifra con AES-256-GCM. Lancia AuthenticationTagMismatchException
        /// (o CryptographicException) se chiave/nonce/tag non corrispondono:
        /// i chiamanti la traducono in "segreto non valido", senza dettagli.
        /// </summary>
        public static byte[] Decifra(byte[] cifrato, byte[] chiave, byte[] nonce, byte[] tag)
        {
            if (chiave.Length != DimensioneChiave)
                throw new ArgumentException("La chiave deve essere di 32 byte.", nameof(chiave));

            var chiaro = new byte[cifrato.Length];
            using var aes = new AesGcm(chiave, DimensioneTag);
            aes.Decrypt(nonce, cifrato, tag, chiaro);
            return chiaro;
        }

        // --- Payload credenziale ---

        /// <summary>Serializza il payload (JSON UTF-8) e lo cifra con la DEK.</summary>
        public static (byte[] PayloadCifrato, byte[] Nonce, byte[] Tag) CifraPayload(PasswordPayload payload, byte[] dek)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(payload);
            return Cifra(json, dek);
        }

        /// <summary>Decifra il payload della credenziale con la DEK di sessione.</summary>
        public static PasswordPayload DecifraPayload(byte[] payloadCifrato, byte[] dek, byte[] nonce, byte[] tag)
        {
            var json = Decifra(payloadCifrato, dek, nonce, tag);
            var payload = JsonSerializer.Deserialize<PasswordPayload>(json);

            if (payload == null)
                throw new InvalidOperationException("Payload credenziale non valido.");

            if (payload.Version > PasswordPayload.VersioneCorrente)
                throw new NotSupportedException(
                    $"Versione payload {payload.Version} non supportata (massima {PasswordPayload.VersioneCorrente}).");

            return payload;
        }

        // --- Configurazione iniziale ---

        /// <summary>
        /// Prima configurazione: genera DEK, salt e Recovery Code, protegge la
        /// DEK con entrambe le KEK e restituisce la riga di configurazione più
        /// il Recovery Code (da mostrare UNA SOLA VOLTA all'amministratore).
        /// </summary>
        public static (PasswordConfig Config, string RecoveryCode) CreaConfigurazione(string masterPassword)
        {
            var dek = RandomNumberGenerator.GetBytes(DimensioneChiave);
            var masterSalt = RandomNumberGenerator.GetBytes(DimensioneSalt);
            var recoverySalt = RandomNumberGenerator.GetBytes(DimensioneSalt);

            try
            {
                var recoveryCode = GeneraRecoveryCode();

                var masterKek = DerivaKek(masterPassword, masterSalt, IterazioniPbkdf2Default);
                var recoveryKek = DerivaKek(recoveryCode, recoverySalt, IterazioniPbkdf2Default);

                try
                {
                    var (cifrataMaster, nonceMaster, tagMaster) = Cifra(dek, masterKek);
                    var (cifrataRecovery, nonceRecovery, tagRecovery) = Cifra(dek, recoveryKek);

                    var config = new PasswordConfig
                    {
                        Id = PasswordConfig.IdSingleton,
                        MasterSalt = masterSalt,
                        RecoverySalt = recoverySalt,
                        DekCifrataMaster = cifrataMaster,
                        DekNonceMaster = nonceMaster,
                        DekTagMaster = tagMaster,
                        DekCifrataRecovery = cifrataRecovery,
                        DekNonceRecovery = nonceRecovery,
                        DekTagRecovery = tagRecovery,
                        IterazioniPbkdf2 = IterazioniPbkdf2Default,
                        ConfiguratoIl = DateTime.Now
                    };

                    return (config, recoveryCode);
                }
                finally
                {
                    Azzera(masterKek);
                    Azzera(recoveryKek);
                }
            }
            finally
            {
                Azzera(dek);
            }
        }

        /// <summary>
        /// Tenta lo sblocco con la Master Password: deriva la Master KEK e prova
        /// a decifrare la DEK. Se il tag GCM non verifica il segreto è errato.
        /// La KEK viene sempre azzerata; la DEK restituita è responsabilità
        /// del chiamante (sessione) e va azzerata al lock.
        /// </summary>
        public static bool ProvaSbloccoConMaster(PasswordConfig config, string masterPassword, out byte[]? dek)
        {
            return ProvaSblocco(config.DekCifrataMaster, config.DekNonceMaster, config.DekTagMaster,
                masterPassword, config.MasterSalt, config.IterazioniPbkdf2, out dek);
        }

        /// <summary>Tenta lo sblocco con il Recovery Code (stessa DEK).</summary>
        public static bool ProvaSbloccoConRecovery(PasswordConfig config, string recoveryCode, out byte[]? dek)
        {
            return ProvaSblocco(config.DekCifrataRecovery, config.DekNonceRecovery, config.DekTagRecovery,
                recoveryCode, config.RecoverySalt, config.IterazioniPbkdf2, out dek);
        }

        private static bool ProvaSblocco(byte[] dekCifrata, byte[] nonce, byte[] tag,
            string segreto, byte[] salt, int iterazioni, out byte[]? dek)
        {
            dek = null;
            var kek = DerivaKek(segreto, salt, iterazioni);
            try
            {
                dek = Decifra(dekCifrata, kek, nonce, tag);
                return true;
            }
            catch (Exception ex) when (ex is CryptographicException || ex is System.Security.Authentication.AuthenticationException)
            {
                Azzera(dek);
                dek = null;
                return false;
            }
            finally
            {
                Azzera(kek);
            }
        }

        // --- Recovery: rinegoziazione della protezione Master della DEK ---

        /// <summary>
        /// Recovery della Master Password: recupera la DEK con il Recovery Code,
        /// genera un nuovo salt Master, deriva la nuova Master KEK dalla nuova
        /// password e sostituisce SOLO la protezione Master della DEK.
        /// La DEK non cambia: le credenziali NON vengono ricifrate e il
        /// Recovery Code resta valido.
        /// </summary>
        public static bool RecuperaConRecovery(PasswordConfig config, string recoveryCode,
            string nuovaMasterPassword, Action<PasswordConfig> applicaModifiche)
        {
            if (!ProvaSbloccoConRecovery(config, recoveryCode, out var dek) || dek == null)
                return false;

            try
            {
                var nuovoSalt = RandomNumberGenerator.GetBytes(DimensioneSalt);
                var nuovaKek = DerivaKek(nuovaMasterPassword, nuovoSalt, config.IterazioniPbkdf2)
                    ?? throw new InvalidOperationException("Derivazione KEK fallita.");
                try
                {
                    var (cifrata, nonce, tag) = Cifra(dek, nuovaKek);

                    config.MasterSalt = nuovoSalt;
                    config.DekCifrataMaster = cifrata;
                    config.DekNonceMaster = nonce;
                    config.DekTagMaster = tag;

                    applicaModifiche(config);
                    return true;
                }
                finally
                {
                    Azzera(nuovaKek);
                }
            }
            finally
            {
                Azzera(dek);
            }
        }

        // --- Recovery Code ---

        /// <summary>
        /// Alfabeto senza caratteri confondibili (0 O 1 I L): 31 simboli,
        /// 16 caratteri totali ≈ 78 bit di entropia, generati con CSPRNG.
        /// </summary>
        private const string AlfabetoRecovery = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Genera un Recovery Code nel formato XXXX-XXXX-XXXX-XXXX con CSPRNG.</summary>
        public static string GeneraRecoveryCode()
        {
            Span<char> caratteri = stackalloc char[19];
            var indice = 0;

            for (var gruppo = 0; gruppo < 4; gruppo++)
            {
                if (gruppo > 0)
                    caratteri[indice++] = '-';

                for (var i = 0; i < 4; i++)
                    caratteri[indice++] = AlfabetoRecovery[RandomNumberGenerator.GetInt32(AlfabetoRecovery.Length)];
            }

            return new string(caratteri);
        }

        /// <summary>Normalizza un Recovery Code prima della derivazione (maiuscole, senza spazi).</summary>
        public static string NormalizzaRecoveryCode(string codice)
        {
            return codice.Trim().ToUpperInvariant().Replace(" ", string.Empty);
        }
    }
}