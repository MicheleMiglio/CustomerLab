namespace CLab.Models
{
    public enum TipoContatto
    {
        Telefono,
        Email
    }

    public class Contatti
    {
        public int Id { get; set; }
        public TipoContatto Tipo { get; set; }
        public string Valore { get; set; } = string.Empty;
        public string? Etichetta { get; set; }
        public bool Principale { get; set; }

        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }
    }
}