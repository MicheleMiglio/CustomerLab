using System;
using System.Security.Cryptography;
using System.Text;

namespace CLab.Services
{
    /// <summary>
    /// Generatore di password crittograficamente sicuro: usa esclusivamente
    /// RandomNumberGenerator (CSPRNG di System.Security.Cryptography), mai
    /// System.Random, timestamp o GUID. Garantisce che ogni categoria
    /// selezionata sia rappresentata almeno una volta, completa dal pool
    /// complessivo e mescola con Fisher-Yates basato su CSPRNG.
    /// </summary>
    public static class PasswordGeneratorService
    {
        public const int LunghezzaMinima = 8;
        public const int LunghezzaMassima = 64;
        public const int LunghezzaDefault = 20;

        /// <summary>Set simboli costante e leggibile: caratteri comuni, privi
        /// di ambiguità frequenti (niente virgolette/backslash che complicano
        /// copia/incolla in molti contesti).</summary>
        public const string Simboli = "!@#$%^&*()-_=+[]{}?";

        private const string Maiuscole = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Minuscole = "abcdefghijklmnopqrstuvwxyz";
        private const string Numeri = "0123456789";

        /// <summary>
        /// Genera una password. Ritorna null (con errore valorizzato) se la
        /// lunghezza è fuori range o nessuna categoria è selezionata: la UI
        /// può mostrare l'errore senza eccezioni.
        /// </summary>
        public static string? Genera(int lunghezza, bool maiuscole, bool minuscole, bool numeri, bool simboli, out string? errore)
        {
            errore = null;

            if (lunghezza < LunghezzaMinima || lunghezza > LunghezzaMassima)
            {
                errore = $"La lunghezza deve essere tra {LunghezzaMinima} e {LunghezzaMassima} caratteri.";
                return null;
            }

            var pool = new StringBuilder();
            var categorie = new (bool attiva, string insieme)[] {
                (maiuscole, Maiuscole),
                (minuscole, Minuscole),
                (numeri, Numeri),
                (simboli, Simboli),
            };

            foreach (var (attiva, insieme) in categorie)
                if (attiva)
                    pool.Append(insieme);

            if (pool.Length == 0)
            {
                errore = "Seleziona almeno una categoria.";
                return null;
            }

            // Un carattere garantito per ogni categoria selezionata.
            var caratteri = new char[lunghezza];
            var indice = 0;

            foreach (var (attiva, insieme) in categorie)
                if (attiva && indice < lunghezza)
                    caratteri[indice++] = insieme[RandomNumberGenerator.GetInt32(insieme.Length)];

            // Il resto dal pool complessivo.
            var poolFinale = pool.ToString();
            while (indice < lunghezza)
                caratteri[indice++] = poolFinale[RandomNumberGenerator.GetInt32(poolFinale.Length)];

            // Fisher-Yates con CSPRNG.
            for (var i = lunghezza - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (caratteri[i], caratteri[j]) = (caratteri[j], caratteri[i]);
            }

            return new string(caratteri);
        }

        // --- Validazione (usabile anche dai test) ---

        public static bool LunghezzaValida(int lunghezza, string password)
            => password.Length == lunghezza;

        public static bool CaratteriTuttiNelPool(string password, bool maiuscole, bool minuscole, bool numeri, bool simboli)
        {
            foreach (var c in password)
            {
                var ok =
                    (maiuscole && Maiuscole.Contains(c)) ||
                    (minuscole && Minuscole.Contains(c)) ||
                    (numeri && Numeri.Contains(c)) ||
                    (simboli && Simboli.Contains(c));

                if (!ok)
                    return false;
            }

            return true;
        }

        public static bool CategoriaRappresentata(string password, string insieme)
        {
            foreach (var c in password)
                if (insieme.Contains(c))
                    return true;

            return false;
        }

        public static string InsiemeMaiuscole => Maiuscole;
        public static string InsiemeMinuscole => Minuscole;
        public static string InsiemeNumeri => Numeri;
    }
}