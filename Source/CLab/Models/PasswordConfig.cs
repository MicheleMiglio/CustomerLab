using System;

namespace CLab.Models
{
    /// <summary>
    /// Configurazione singleton del modulo Password: una sola riga (Id = 1)
    /// con il materiale crittografico di protezione della DEK.
    /// La DEK (chiave dati) NON è mai salvata in chiaro: è cifrata due volte
    /// con AES-256-GCM, una con la KEK derivata dalla Master Password e una
    /// con la KEK derivata dal Recovery Code (doppia protezione indipendente).
    /// Master Password e Recovery Code non sono mai persistiti in alcuna forma.
    /// </summary>
    public class PasswordConfig
    {
        /// <summary>Id fisso = 1: la tabella contiene al massimo una riga.</summary>
        public int Id { get; set; }

        /// <summary>Salt PBKDF2 della Master Password (16 byte casuali).</summary>
        public byte[] MasterSalt { get; set; } = Array.Empty<byte>();

        /// <summary>Salt PBKDF2 del Recovery Code (16 byte casuali, distinto dal MasterSalt).</summary>
        public byte[] RecoverySalt { get; set; } = Array.Empty<byte>();

        /// <summary>DEK (32 byte) cifrata con la Master KEK (32 + 16 di tag GCM).</summary>
        public byte[] DekCifrataMaster { get; set; } = Array.Empty<byte>();

        /// <summary>Nonce GCM usato per DekCifrataMaster (12 byte).</summary>
        public byte[] DekNonceMaster { get; set; } = Array.Empty<byte>();

        /// <summary>Tag GCM di DekCifrataMaster (16 byte).</summary>
        public byte[] DekTagMaster { get; set; } = Array.Empty<byte>();

        /// <summary>La stessa DEK cifrata con la Recovery KEK (32 + 16 di tag GCM).</summary>
        public byte[] DekCifrataRecovery { get; set; } = Array.Empty<byte>();

        /// <summary>Nonce GCM usato per DekCifrataRecovery (12 byte).</summary>
        public byte[] DekNonceRecovery { get; set; } = Array.Empty<byte>();

        /// <summary>Tag GCM di DekCifrataRecovery (16 byte).</summary>
        public byte[] DekTagRecovery { get; set; } = Array.Empty<byte>();

        /// <summary>Numero di iterazioni PBKDF2 usato in configurazione, salvato per gestirne l'evoluzione.</summary>
        public int IterazioniPbkdf2 { get; set; }

        public DateTime ConfiguratoIl { get; set; } = DateTime.Now;
    }
}