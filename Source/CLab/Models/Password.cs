using System;

namespace CLab.Models
{
    /// <summary>
    /// Una credenziale del modulo Password. I contenuti sensibili (Nome, Sito,
    /// Username, Password, Note) NON esistono come colonne in chiaro: vengono
    /// serializzati in un unico payload JSON (vedi PasswordPayload) e cifrati
    /// con AES-256-GCM usando la DEK della sessione. Nel database restano solo
    /// il blob cifrato, il nonce e il tag di autenticazione.
    /// Nessuna relazione con le altre entity di CLab, per progetto.
    /// </summary>
    public class Password
    {
        public int Id { get; set; }

        /// <summary>Payload JSON cifrato (AES-256-GCM) con la DEK di sessione.</summary>
        public byte[] PayloadCifrato { get; set; } = Array.Empty<byte>();

        /// <summary>Nonce GCM (12 byte) generato a ogni cifratura; mai riutilizzato.</summary>
        public byte[] Nonce { get; set; } = Array.Empty<byte>();

        /// <summary>Tag di autenticazione GCM (16 byte).</summary>
        public byte[] Tag { get; set; } = Array.Empty<byte>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}