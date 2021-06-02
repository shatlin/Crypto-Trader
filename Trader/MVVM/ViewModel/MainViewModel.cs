using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trader.Core;

namespace Trader.MVVM.ViewModel
{
    class MainViewModel: ObservableObject
    {

        public RelayCommand HomeViewCommand { get;set;}
        public RelayCommand BalanceViewCommand { get; set; }

        public HomeViewModel HomeVM { get; set; }
        public BalanceViewModel BalanceVM { get; set; }

        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set { _currentView = value; OnPropertyChanged();}
        }


        public MainViewModel()
        {
            HomeVM= new HomeViewModel();
            BalanceVM = new BalanceViewModel();
            CurrentView = HomeVM;
            HomeViewCommand=new RelayCommand(o=>
            {
               CurrentView=HomeVM;
            });

            BalanceViewCommand = new RelayCommand(o =>
            {
                CurrentView = BalanceVM;
            });
        }
    }
}
