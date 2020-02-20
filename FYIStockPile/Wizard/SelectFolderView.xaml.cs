using LiteDB;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FYIStockPile.Interfaces;
using FYIStockPile.Storage;

namespace FYIStockPile.Wizard
{
    /// <summary>
    /// Interaction logic for SelectFolderView.xaml
    /// </summary>
    public partial class SelectFolderView : UserControl
    {
        private AppConfigRepository AppConfigData = new AppConfigRepository();
        private AppConfigRepository _AppConfigData;

        public List<string> folderLists = new List<string>();
        private bool addFolderWarning = false;

        public SelectFolderView()
        {
            InitializeComponent();
            if (AppConfigData.Pile.Settings.Restart)
            {
                _AppConfigData  = new AppConfigRepository();
                folderLists = _AppConfigData.Pile.Settings.FolderLists;
                OutputBlock.Inlines.Clear();
                for (var i = 0; i < folderLists.Count(); i++)
                {
                    try
                    {
                        OutputBlock.Inlines.Add(folderLists[i] + "\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        private void AddFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result == CommonFileDialogResult.Ok)
            {
                if (folderLists.Count > 0)
                {
                    //foreach (String folder in folderLists.ToList())
                    //{
                    //    if (folder == dialog.FileName || dialog.FileName.Contains(folder) || folder.Contains(dialog.FileName))
                    //    {
                    //        addFolderWarning = true;
                    //        if (folder == dialog.FileName)
                    //        {
                    //            MessageBox.Show("Warning: Duplicated Folder\n" + folder, "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //        }
                    //        //else if (dialog.FileName.Contains(folder))
                    //        //{
                    //        //    MessageBox.Show("Warning: You already added a parent folder of this folder\nParent Folder: " + folder + "\nChild Folder: " + dialog.FileName, "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //        //}
                    //        //else if (folder.Contains(dialog.FileName))
                    //        //{
                    //        //    MessageBox.Show("Warning: You already added a child folder of this folder\nParent Folder: " + dialog.FileName + "\nChild Folder: " + folder, "FYIStockPile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //        //}
                    //    }
                    //}

                    if (!addFolderWarning)
                    {
                        folderLists.Add(dialog.FileName);
                        AppConfigData.Pile.Settings.FolderLists = folderLists;
                        AppConfigData.Pile.Save();
                        OutputBlock.Inlines.Add(dialog.FileName + "\n");
                    }
                }
                else
                {
                    if (!addFolderWarning)
                    {
                        folderLists.Add(dialog.FileName);
                        AppConfigData.Pile.Settings.FolderLists.Clear();
                        AppConfigData.Pile.Settings.FolderLists.Add(dialog.FileName);
                        AppConfigData.Pile.Save();
                        OutputBlock.Inlines.Add(dialog.FileName + "\n");
                    }
                }
            }
        }
    }
}
