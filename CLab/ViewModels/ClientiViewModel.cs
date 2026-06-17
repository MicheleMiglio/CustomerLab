using System.Collections.ObjectModel;
using System.Linq;
using CLab.Data;
using CLab.Models;

namespace CLab.ViewModels
{
    public class ClientiViewModel : ViewModelBase
    {
        public ObservableCollection<Cliente> Clienti { get; set; }

        public ClientiViewModel()
        {
            Clienti = new ObservableCollection<Cliente>();
            CaricaClienti();
        }

        private void CaricaClienti()
        {
            using (var db = new ClabDbContext())
            {
                var listaClienti = db.Clienti.ToList();

                Clienti.Clear();
                foreach (var cliente in listaClienti)
                {
                    Clienti.Add(cliente);
                }
            }
        }
    }
}