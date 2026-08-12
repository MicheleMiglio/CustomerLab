namespace CLab.Models
{
    public class Programma
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;

        public override string ToString() => Nome;
    }
}