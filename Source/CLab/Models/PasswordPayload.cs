namespace CLab.Models
{
    /// <summary>
    /// Contenuto logico di una credenziale, serializzato con System.Text.Json,
    /// convertito in UTF-8 e cifrato interamente come unico payload AES-256-GCM.
    /// La proprietà Version è obbligatoria (attualmente 1) per consentire
    /// eventuali evoluzioni future del formato senza rompere i dati esistenti.
    /// Esiste solo in memoria durante la sessione sbloccata: mai persistito.
    /// </summary>
    public class PasswordPayload
    {
        public const int VersioneCorrente = 1;

        public int Version { get; set; } = VersioneCorrente;
        public string Nome { get; set; } = string.Empty;
        public string Sito { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }
}