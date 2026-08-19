using CLab.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace CLab
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Forza la cultura italiana per l'intera applicazione, sia per il thread
            // (formattazioni "manuali", ToString, ecc.) sia per i binding WPF.
            // Senza questo, i controlli WPF usano di default la cultura "en-US"
            // per interpretare il testo digitato negli TextBox (a prescindere dalla
            // lingua di Windows), causando la virgola dei decimali interpretata come
            // separatore delle migliaia (es. "1448,80" diventava "144.880").
            var culturaItaliana = new CultureInfo("it-IT");
            CultureInfo.DefaultThreadCurrentCulture = culturaItaliana;
            CultureInfo.DefaultThreadCurrentUICulture = culturaItaliana;
            Thread.CurrentThread.CurrentCulture = culturaItaliana;
            Thread.CurrentThread.CurrentUICulture = culturaItaliana;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            base.OnStartup(e);

            using (var db = new ClabDbContext())
            {
                db.Database.Migrate();
            }
        }
    }
}