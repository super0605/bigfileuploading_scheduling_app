using FYIStockPile.Storage;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MVVMC;
using FYIStockPile.Interfaces;
using System.Windows.Input;

namespace FYIStockPile.Wizard
{
    class WelcomeViewModel : MVVMCViewModel
    {
        private AppRepository pile;

        public string _isMigrateKey;
        public bool _isUseProxy;
        public string _password;
        public string _host;
        public string _port;
        public string _username;
        public WelcomeViewModel()
        {
            AppConfigRepository AppConfigData = new AppConfigRepository();
            pile = AppConfigData.Pile;                        
            _isMigrateKey = pile.Settings.BucketName;
            _isUseProxy = pile.Settings.UseProxy;
            _password = pile.Settings.Password;
            _host = pile.Settings.Host;
            _port = pile.Settings.Port;
            _username = pile.Settings.Username;
        }

        public string IsMigrateKey
        {
            get { return _isMigrateKey; }
            set
            {
                _isMigrateKey = value;
                OnPropertyChanged();
            }
        }

        public bool IsUseProxy
        {
            get { return _isUseProxy; }
            set
            {
                _isUseProxy = value;
                OnPropertyChanged();
            }
        }

        public ICommand PasswordChangedCommand
        {
            get
            {
                return new RelayCommand<object>(ExecChangePassword);
            }
        }

        private void ExecChangePassword(object obj)
        {
            _password = ((System.Windows.Controls.PasswordBox)obj).Password;
        }

        public string Host
        {
            get { return _host; }
            set
            {
                _host = value;
                OnPropertyChanged();
            }
        }

        public string Port
        {
            get { return _port; }
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }
    }
}
