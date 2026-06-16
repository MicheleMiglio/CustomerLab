namespace CLab.Models
{
    public class Cliente
    {
        public int Id { get; set; }
        public string RagioneSociale { get; set; } = string.Empty;
        public string? PartitaIva { get; set; }
        public string? CodiceFiscale { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Note { get; set; }
        public bool Attivo { get; set; } = true;
    }
}
