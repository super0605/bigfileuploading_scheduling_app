using FYIStockPile.Storage;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MVVMC;
using FYIStockPile.Interfaces;

namespace FYIStockPile.Wizard
{
    class SelectFolderViewModel : MVVMCViewModel
    {
        private AppRepository pile;
        private AppConfigRepository AppConfigData = new AppConfigRepository();

        private string _outputFolderLists;
        private bool _includeFolderName;

        public SelectFolderViewModel()
        {
            pile = AppConfigData.Pile;           
            _includeFolderName = pile.Settings.IncludeFolderName;            
        }

        public string OutputFolderLists
        {
            get { return _outputFolderLists; }
            set
            {
                _outputFolderLists = value;
                OnPropertyChanged();
            }
        }

        public bool IncludeFolderName
        {
            get { return _includeFolderName; }
            set
            {
                _includeFolderName = value;
                OnPropertyChanged();
            }
        }

    }
}
