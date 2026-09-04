using CLab.Data;
using CLab.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CLab.ViewModels
{
    /// <summary>
    /// Dispatcher centrale della navigazione CLab 2.0 (FASE 4): implementa
    /// INavigatore, così le sorgenti di navigazione (es. la Home) aprono i
    /// moduli in modo contestuale senza dipendere direttamente da MainViewModel.
    /// </summary>
    public class MainViewModel : ViewModelBase, INavigatore
    {
        private object? _vistaCorrente;
        private MenuItemModel? _dashboardMenu;
        private MenuItemModel? _promemoriaMenu;
        private MenuItemModel? _clientiMenu;
        private MenuItemModel? _attivitaMenu;
        private MenuItemModel? _scadenzarioMenu;
        private MenuItemModel? _fattureMenu;
        private MenuItemModel? _todoMenu;
        private MenuItemModel? _impostazioniMenu;

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
            AggiornaBadgePromemoria();
            ApriDashboard(_dashboardMenu);
        }


        private void CreaMenu()
        {
            _dashboardMenu = new MenuItemModel
            {
                Titolo = "Home",
                Icona = "House",
                Comando = new RelayCommand<MenuItemModel>(ApriDashboard)
            };
            MenuPrincipale.Add(_dashboardMenu);

            _promemoriaMenu = new MenuItemModel
            {
                Titolo = "Promemoria",
                Icona = "Bell",
                Comando = new RelayCommand<MenuItemModel>(ApriPromemoria)
            };
            MenuPrincipale.Add(_promemoriaMenu);

            _scadenzarioMenu = new MenuItemModel
            {
                Titolo = "Scadenzario",
                Icona = "Calendar",
                Comando = new RelayCommand<MenuItemModel>(ApriScadenzario)
            };
            MenuPrincipale.Add(_scadenzarioMenu);

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

            _fattureMenu = new MenuItemModel
            {
                Titolo = "Fatture",
                Icona = "Receipt",
                Comando = new RelayCommand<MenuItemModel>(ApriFatture)
            };
            MenuPrincipale.Add(_fattureMenu);

            _todoMenu = new MenuItemModel
            {
                Titolo = "ToDo",
                Icona = "CheckSquare",
                Comando = new RelayCommand<MenuItemModel>(ApriToDo)
            };
            MenuPrincipale.Add(_todoMenu);

            _impostazioniMenu = new MenuItemModel
            {
                Titolo = "Impostazioni",
                Icona = "Gear",
                Comando = new RelayCommand<MenuItemModel>(ApriImpostazioni),
                Separatore = true
            };
            MenuFooter.Add(_impostazioniMenu);
        }

        private void ApriDashboard(MenuItemModel? menu)
        {
            SelezionaMenu(menu);

            // FASE 4B: la Home riceve il navigatore per la navigazione contestuale.
            VistaCorrente = new HomeViewModel(this);
        }

        private void ApriPromemoria(MenuItemModel? menu) => ApriPromemoria();

        public void ApriPromemoria()
        {
            SelezionaMenu(_promemoriaMenu);

            VistaCorrente = new PromemoriaViewModel(AggiornaBadgePromemoria);
        }

        private void AggiornaBadgePromemoria()
        {
            if (_promemoriaMenu == null)
                return;

            using var db = new ClabDbContext();
            _promemoriaMenu.Contatore = db.Promemoria.Count();
        }

        private void ApriClienti(MenuItemModel? menu) => ApriClienti();

        public void ApriClienti()
        {
            SelezionaMenu(_clientiMenu);

            // FASE 5: il navigatore abilita i collegamenti operativi dalla
            // Situazione Cliente (Scadenzario/ToDo/Ritenute per Id).
            VistaCorrente = new ClientiViewModel(this);
        }

        private void ApriScadenzario(MenuItemModel? menu) => ApriScadenzario();

        public void ApriScadenzario(int? clienteId = null, string? scheda = null, bool soloRitardi = false)
        {
            SelezionaMenu(_scadenzarioMenu);

            var vm = new ScadenzarioViewModel(ApriConfigurazioneAttivitaPerCliente);
            if (clienteId.HasValue)
                vm.ApriPerCliente(clienteId.Value, scheda, soloRitardi);

            VistaCorrente = vm;
        }

        private void ApriAttivita(MenuItemModel? menu)
        {
            SelezionaMenu(menu);

            VistaCorrente = new AttivitaViewModel();
        }

        private void ApriFatture(MenuItemModel? menu) => ApriFatture();

        public void ApriFatture(int? anno = null)
        {
            SelezionaMenu(_fattureMenu);

            var vm = new FattureViewModel();
            if (anno.HasValue)
                vm.ApriSuAnno(anno.Value);

            VistaCorrente = vm;
        }

        private void ApriToDo(MenuItemModel? menu) => ApriToDo();

        public void ApriToDo(int? clienteId = null, bool soloScaduti = false, bool prioritaAlta = false)
        {
            SelezionaMenu(_todoMenu);

            var vm = new ToDoViewModel();
            if (clienteId.HasValue || soloScaduti || prioritaAlta)
                vm.ApriConFiltri(clienteId, soloScaduti, prioritaAlta);

            VistaCorrente = vm;
        }

        private void ApriImpostazioni(MenuItemModel? menu)
        {
            SelezionaMenu(menu);
            VistaCorrente = new ImpostazioniViewModel();
        }

        private void ApriConfigurazioneAttivitaPerCliente(int clienteId)
        {
            SelezionaMenu(_attivitaMenu);

            var vm = new AttivitaViewModel();
            vm.MostraConfigurazioneCommand.Execute(null);
            vm.ApriConfigurazionePerCliente(clienteId);

            VistaCorrente = vm;
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