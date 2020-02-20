using FYIStockPile.Storage;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMC;
using FYIStockPile.Interfaces;

namespace FYIStockPile.Wizard
{
    class MYOBSettingsViewModel : MVVMCViewModel
    {
        private AppRepository pile;
        private AppConfigRepository AppConfigData = new AppConfigRepository();

        private string _isServerName = "localhost";
        private string _isDBName = "VPMSER";

        public MYOBSettingsViewModel()
        {
            pile = AppConfigData.Pile;
            if (pile.Settings.Restart)
            {
                _isServerName = pile.Settings.MyobServerName;
                _isDBName = pile.Settings.MyobDatabaseName;
            }
        }

        public string IsServerName
        {
            get { return _isServerName; }
            set
            {
                _isServerName = value;
                OnPropertyChanged();
            }
        }

        
        public string IsDBName
        {
            get { return _isDBName; }
            set
            {
                _isDBName = value;
                OnPropertyChanged();
            }
        }
    }
}
