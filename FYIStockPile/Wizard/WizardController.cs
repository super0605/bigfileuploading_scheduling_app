using FYIStockPile.Storage;
using LiteDB;
using System;
using MVVMC;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using FYIStockPile.Interfaces;

namespace FYIStockPile.Wizard
{
    public class Model
    {
        public string Region { get; set; }
        public string AWSSecret { get; set; }
        public string AWSKeyID { get; set; }
        public string BucketName { get; set; }
        public string MyobServerName { get; set; }
        public string MyobDatabaseName { get; set; }
        public string Folder { get; set; }
        public bool IncludeFolderName { get; set; }
        public string Mode { get; set; }
        public bool Restart { get; set; }
        public bool UseProxy { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string Username { get; set; }
    }

    class WizardController : Controller
    {
        public Model _model;
        private static LiteDatabase Database = new LiteDatabase(@"fyi_data.db");

        private static Slack slack = new Slack();
        private AppRepository Pile = new AppRepository(Database, slack);
        private MyobRepository MYOB = new MyobRepository(Database);
        private S3StorageRepository S3 = new S3StorageRepository(Database);
        private AppRepository _Pile;
        private AppRepository _RestartPile;

        public override void Initial()
        {
            _model = new Model();
            Welcome();
        }

        private void Welcome()
        {
            ExecuteNavigation();
        }

        private void ChooseSystem()
        {
            ExecuteNavigation();
        }

        public void MYOBSettings()
        {
            AppRepository Pile_Mode_Myob = new AppRepository(Database, slack);
            _model.Mode = "mode_myob";
            Pile_Mode_Myob.Settings.Mode = "mode_myob";
            Pile_Mode_Myob.Save(); // save mode MYOB
            ExecuteNavigation();
        }

        public void SelectFolder()
        {
            ExecuteNavigation();
        }
        public void Uploading()
        {
            ExecuteNavigation();
        }

        public async void CheckProductKey()
        {
            if (this.GetCurrentViewModel() is WelcomeViewModel welcomeViewModel)
            {
                _model.BucketName = welcomeViewModel.IsMigrateKey;
                _model.UseProxy = welcomeViewModel.IsUseProxy;
                _model.Host = welcomeViewModel.Host;
                _model.Port = welcomeViewModel.Port;
                _model.Username = welcomeViewModel.Username;
                _model.Password = welcomeViewModel._password;
                AppRepository Pile_Welcome = new AppRepository(Database, slack);
                Pile_Welcome.Settings.BucketName = _model.BucketName;
                Pile_Welcome.Settings.UseProxy = _model.UseProxy;
                Pile_Welcome.Settings.Host = _model.Host;
                Pile_Welcome.Settings.Port = _model.Port;
                Pile_Welcome.Settings.Username = _model.Username;
                Pile_Welcome.Settings.Password = _model.Password;

                if (_model.BucketName == "" || _model.BucketName == null)
                {
                    string startMessage = "{ \"text\": \"Please add migrate key before clicking the Next button?\" }";
                    await slack.TryPostJsonAsync(startMessage);
                    MessageBox.Show("Please add migrate key before clicking the Next button?", "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    bool validateKey = S3.ValidateMigrateKey(_model.BucketName, Pile_Welcome);
                    if (validateKey)
                    {
                        Pile_Welcome.Save(); // Save Bucket Name
                        ChooseSystem();
                    }
                    else
                    {
                        MessageBox.Show("You do not have access to this Key. Please contact FYI Support", "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    
                }
            }
        }

        public void SelectFileSystem()
        {
            AppRepository Pile_Mode_FileSystem = new AppRepository(Database, slack);
            _model.Mode = "mode_file_system";
            Pile_Mode_FileSystem.Settings.Mode = "mode_file_system";
            Pile_Mode_FileSystem.Save(); // save mode FileSystem
            SelectFolder();
        }

        public void TestDatabase()
        {
            if (this.GetCurrentViewModel() is MYOBSettingsViewModel myobSettingsViewModel)
            {
                AppRepository Pile_MYOB = new AppRepository(Database, slack);
                _model.MyobDatabaseName = myobSettingsViewModel.IsDBName;
                _model.MyobServerName = myobSettingsViewModel.IsServerName;
                Pile_MYOB.Settings.MyobServerName = _model.MyobServerName;
                Pile_MYOB.Settings.MyobDatabaseName = _model.MyobDatabaseName;

                if (_model.MyobServerName == "" || _model.MyobServerName == null || _model.MyobDatabaseName == "" || _model.MyobDatabaseName == null)
                {
                    MessageBox.Show("Warning: you didn't enter server name or database name.", "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    if (MYOB.ConnectionTest(Pile))
                    {
                        Pile_MYOB.Save(); // save MYOB Settings
                       // MessageBox.Show("Connect Test: Success", "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Information);
                        SelectFolder();
                    }
                    else
                    {
                        MessageBox.Show("Connect Test: Error", "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        public void AddFolderDone()
        {
            _Pile = new AppRepository(Database, slack);
            if (this.GetCurrentViewModel() is SelectFolderViewModel selectFolderViewModel)
            {
                _model.IncludeFolderName = selectFolderViewModel.IncludeFolderName;
                _Pile.Settings.IncludeFolderName = _model.IncludeFolderName;
                _Pile.Settings.BucketName = _model.BucketName;
                _Pile.Settings.MyobServerName = _model.MyobServerName;
                _Pile.Settings.MyobDatabaseName = _model.MyobDatabaseName;
                _Pile.Settings.Mode = _model.Mode;
                _Pile.Settings.Restart = false;
                _Pile.Save();

                Uploading();
            }
        }

        public void RestartWizard()
        {
            _RestartPile = new AppRepository(Database, slack);
            _model.Restart = true;
            _RestartPile.Settings.Restart = true;

            _RestartPile.Save();
            Welcome();
        }
    }
}
