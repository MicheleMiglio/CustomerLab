using CLab.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object? _vistaCorrente;
        private MenuItemModel? _dashboardMenu;
        private MenuItemModel? _clientiMenu;
        private MenuItemModel? _attivitaMenu;

        public object? VistaCorrente
        {
            get => _vistaCorrente;
            set
            {
                if (_vistaCorrente == value)
                    return;

                _vistaCorrente = value;
                OnPropertyChanged();
            }
        }

        private MenuItemModel? _menuSelezionato;

        public MenuItemModel? MenuSelezionato
        {
            get => _menuSelezionato;
            set
            {
                if (_menuSelezionato == value)
                    return;

                _menuSelezionato = value;
                OnPropertyChanged();
            }
        }

        private bool _sidebarEspansa = true;

        public bool SidebarEspansa
        {
            get => _sidebarEspansa;
            set
            {
                if (_sidebarEspansa == value)
                    return;

                _sidebarEspansa = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(SidebarWidth));
            }
        }

        public ICommand ToggleSidebarCommand { get; }

        public ObservableCollection<MenuItemModel> MenuPrincipale { get; set; }

        public ObservableCollection<MenuItemModel> MenuFooter { get; set; }


        public MainViewModel()
        {
            ToggleSidebarCommand = new RelayCommand(ToggleSidebar);
        
            MenuPrincipale = new ObservableCollection<MenuItemModel>();

            MenuFooter = new ObservableCollection<MenuItemModel>();

            CreaMenu();

            ApriDashboard(_dashboardMenu);
        }


        private void CreaMenu()
        {
            _dashboardMenu = new MenuItemModel
            {
                Titolo = "Dashboard",
                Icona = "House",
                Comando = new RelayCommand<MenuItemModel>(ApriDashboard)
            };
            MenuPrincipale.Add(_dashboardMenu);

            _clientiMenu = new MenuItemModel
            {
                Titolo = "Clienti",
                Icona = "People",
                Comando = new RelayCommand<MenuItemModel>(ApriClienti)
            };
            MenuPrincipale.Add(_clientiMenu);

            _attivitaMenu = new MenuItemModel
            {
                Titolo = "Attività",
                Icona = "Clipboard",
                Comando = new RelayCommand<MenuItemModel>(ApriAttivita)
            };
            MenuPrincipale.Add(_attivitaMenu);

            MenuPrincipale.Add(
                new MenuItemModel
                {
                    Titolo = "Scadenze",
                    Icona = "Calendar",
                    Comando = null
                });

            MenuPrincipale.Add(
                new MenuItemModel
                {
                    Titolo = "Fatture",
                    Icona = "Receipt",
                    Comando = null
                });

            MenuFooter.Add(
                new MenuItemModel
                {
                    Titolo = "Impostazioni",
                    Icona = "Gear",
                    Comando = null,
                    Separatore = true
                });
        }

        private void ApriDashboard(MenuItemModel? menu)
        {
            SelezionaMenu(menu);

            VistaCorrente = new DashboardViewModel();
        }

        private void ApriClienti(MenuItemModel? menu)
        {
            SelezionaMenu(menu);

            VistaCorrente = new ClientiViewModel();
        }

        private void ApriAttivita(MenuItemModel? menu)
        {
            SelezionaMenu(menu);

            VistaCorrente = new AttivitaViewModel();
        }

        private void SelezionaMenu(MenuItemModel? menu)
        {
            foreach (var item in MenuPrincipale)
            {
                item.IsSelezionato = false;
            }

            foreach (var item in MenuFooter)
            {
                item.IsSelezionato = false;
            }

            if (menu != null)
            {
                menu.IsSelezionato = true;
            }

            MenuSelezionato = menu;
        }

        private void ToggleSidebar()
        {
            SidebarEspansa = !SidebarEspansa;
        }

        public GridLength SidebarWidth
        {
            get
            {
                return SidebarEspansa
                    ? new GridLength(220)
                    : new GridLength(70);
            }
        }
    }
}