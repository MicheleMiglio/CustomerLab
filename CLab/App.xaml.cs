using CLab.Data;
using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace CLab
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var db = new ClabDbContext())
            {
                db.Database.Migrate();
            }
        }
    }
}