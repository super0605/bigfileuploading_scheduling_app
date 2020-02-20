using FYIStockPile.Interfaces;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FYIStockPile.Storage
{
    public class AppSettings
    {
        // ... some codes
    }
    
    
    public class AppRepository
    {
        public AppRepository(LiteDatabase db, Slack slack)
        {
            this.Database = db;
            this.Slack = slack;
            var settings = Database.GetCollection<AppSettings>("settings");

            var results = settings.FindOne(x => x.Id == 1);
            if (results != null)
            {
                Settings.Id = results.Id;
                Settings.LastDocumentId = results.LastDocumentId;
                Settings.LastDocumentModified = results.LastDocumentModified;

                Settings.Region = results.Region;
                Settings.AWSSecret = results.AWSSecret;
                Settings.AWSKeyID = results.AWSKeyID;
                Settings.BucketName = results.BucketName;
                Settings.MyobServerName = results.MyobServerName;
                Settings.MyobDatabaseName = results.MyobDatabaseName;
                Settings.Mode = results.Mode;
                Settings.Folder = results.Folder;
                Settings.FolderLists = results.FolderLists;
                Settings.IncludeFolderName = results.IncludeFolderName;
                Settings.Restart = results.Restart;
                Settings.UseProxy = results.UseProxy;
                Settings.Password = results.Password;
                Settings.Host = results.Host;
                Settings.Port = results.Port;
                Settings.Username = results.Username;
            }
        }

        private LiteDatabase Database;
        public Slack Slack;
        public bool SyncEnabled = false;
        

        public AppSettings Settings = new AppSettings();

        public void Save()
        {
            var settings = Database.GetCollection<AppSettings>("settings");
            if (Settings.Id == 0)
            {
                settings.Insert(Settings);
            }
            else
            {
                var response = settings.Update(Settings);
            }
            
        }
        public void LogMessage(string text)
        {
            Console.WriteLine(text);
            //System.IO.StreamWriter file = new System.IO.StreamWriter(Environment.CurrentDirectory + "\\log.txt", true);
            //file.WriteLine( DateTime.Now.ToString("o") + " - " + text);
            //file.Close();
        }
    }
}
