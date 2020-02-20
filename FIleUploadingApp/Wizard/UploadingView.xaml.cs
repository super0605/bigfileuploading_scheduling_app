using FYIStockPile.Storage;
using LiteDB;
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
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Timers;
using FYIStockPile.Interfaces;

namespace FYIStockPile.Wizard
{
    /// <summary>
    /// Interaction logic for UploadingView.xaml
    /// </summary>
    public partial class UploadingView : UserControl
    {
        private static LiteDatabase Database = new LiteDatabase(@"fyi_data.db");

        private static Slack slack = new Slack();
        private AppRepository Pile = new AppRepository(Database, slack);
        private MyobRepository MYOB = new MyobRepository(Database);
        private S3StorageRepository S3 = new S3StorageRepository(Database);
        private FileSystemRepository FileSystem = new FileSystemRepository(Database);

        private List<string> logs = new List<string>();
        private string[] threadLogs = new string[10];
        private string uploadStatusLog = "Working on X";
        private string uploadedLog = "Completed 0 of 0";
        private string skippedLog = "Skipped 0 of 0";
        private string myobLog = "Skipped 0 of 0";
        private bool uploadCompletedStatus = false;

        private Timer global_timer = new Timer();
        private Timer refresh_timer = new Timer();
        private Timer thread_status_timer = new Timer();
        private Timer upload_status = new Timer();
        private Timer uploaded = new Timer();
        private Timer skipped = new Timer();
        private Timer upload_completed = new Timer();

        public UploadingView()
        {
            InitializeComponent();

            if (Pile.Settings.Id > 0)
            {
                AppendText("Already run, reloading settings");
                if (Pile.Settings.LastDocumentModified != null)
                {
                    AppendText("MYOB: Last Document = " + Pile.Settings.LastDocumentModified);
                }
            }

            global_timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            global_timer.Interval = 1000;
            global_timer.Enabled = true;

            thread_status_timer.Elapsed += new ElapsedEventHandler(OnThreadStatusTimeEvent);
            thread_status_timer.Interval = 1000;
            thread_status_timer.Enabled = true;

            upload_status.Elapsed += new ElapsedEventHandler(OnUploadStatusEvent);
            upload_status.Interval = 1000;
            upload_status.Enabled = true;

            refresh_timer.Elapsed += new ElapsedEventHandler(OnRefreshEvent);
            refresh_timer.Interval = 1000 * 60 * 60;
            refresh_timer.Enabled = true;

            uploaded.Elapsed += new ElapsedEventHandler(OnUploadedEvent);
            uploaded.Interval = 1000;
            uploaded.Enabled = true;

            skipped.Elapsed += new ElapsedEventHandler(OnSkippedEvent);
            skipped.Interval = 1000;
            skipped.Enabled = true;

            upload_completed.Elapsed += new ElapsedEventHandler(OnUploadCompletedEvent);
            upload_completed.Interval = 1000;
            upload_completed.Enabled = true;
        }

        private void OnRefreshEvent(object source, ElapsedEventArgs e)
        {
            if (Pile.SyncEnabled)
            {
                AppendText("Checking refresh");
                Pile.LogMessage("Checking refresh");
                Task.Factory.StartNew(async () =>
                {
                    await RunSync();
                });
            }
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                OutputBlock.Inlines.Clear();

                for (var i = 0; i < logs.Count(); i++)
                {
                    try
                    {
                        //OutputBlock.Inlines.Add(logs[i] + "\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

            }));
        }

        private void OnThreadStatusTimeEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (string item in threadLogs)
                {
                    if (item != null && item != "")
                    {
                        int idx = Array.IndexOf(threadLogs, item);
                        TextBlock threadTB = (TextBlock)this.FindName("OutputBlock" + idx);
                        if (threadTB != null)
                        {
                            threadTB.Text = item;
                        }
                    }
                }
            }));
        }

        private void OnUploadStatusEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UploadStatusBlock.Text = uploadStatusLog;

                MyobBlock.Text = myobLog;
            }));
        }

        private void OnUploadedEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UplaodedBlock.Text = uploadedLog;
            }));
        }

        private void OnSkippedEvent(object source, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SkippedBlock.Text = skippedLog;
            }));
        }

        private void OnUploadCompletedEvent(object source, ElapsedEventArgs e)
        {
            if (uploadCompletedStatus)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnStart.IsEnabled = false;
                    btnPause.IsEnabled = false;
                    RestartWizard.IsEnabled = true;
                }));
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            if (MYOB.ConnectionTest(Pile))
            {
               // MessageBox.Show("Connect Test: Success");
            }
            else
            {
                MessageBox.Show("Connect Test: Error");
            }
        }

        private void AppendText(string text)
        {
            try
            {
                logs.Add(text);

                if (logs.Count() > 30)
                {
                    logs.RemoveAt(0);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void AppendThreadText(string text)
        {
            string str = text.Split(new string[] { ": " }, StringSplitOptions.None)[0];
            int threadIdx = Convert.ToInt32(str.Split(new string[] { "THREAD " }, StringSplitOptions.None)[1]);
            try
            {
                threadLogs[threadIdx] = text;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void AppendUploadStatusText(string text)
        {
            try
            {
                uploadStatusLog = text;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void AppendMyobText(string text)
        {
            try
            {
                myobLog = text;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        

        private void AppendUploadedText(string text)
        {
            try
            {
                uploadedLog = text;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void AppendSkippedText(string text)
        {
            try
            {
                skippedLog = text;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void HandleUploadCompleted(string text)
        {
            try
            {
                if (text == "completed")
                {
                    uploadCompletedStatus = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        private void Button_Start(object sender, RoutedEventArgs e)
        {
            Pile.SyncEnabled = true;
            btnStart.IsEnabled = false;
            btnPause.IsEnabled = true;
            RestartWizard.IsEnabled = false;
            StackPanel1.Visibility = Visibility.Visible;
            StackPanel2.Visibility = Visibility.Visible;
            if (Pile.Settings.Mode == "mode_myob")
            {
                Row1.Height = new GridLength(50);
                Row2.Height = new GridLength(350);
                MYOBStatus.Visibility = Visibility.Visible;
                MYOBStatusBorder.Visibility = Visibility.Visible;
            }
            Task.Factory.StartNew(async () =>
            {
                await RunSync();
            });
        }

        private void Button_Pause(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnPause.IsEnabled = false;
            Pile.SyncEnabled = false;
            FileSystem.StopUploading();
        }

        private void Button_Restart(object sender, RoutedEventArgs e)
        {
        }

        private async Task RunSync()
        {
            if (Pile.Settings.Mode == "mode_myob")
            {
                if (!MYOB.IsRunning && !FileSystem.IsRunning)
                {
                    AppendText("Starting Sync");
                    if (Pile.Settings.Mode == "mode_myob")
                    {
                        Pile.LogMessage("MYOB: Start");
                        AppendMyobText("Start");

                        await MYOB.Export(Pile, S3, AppendMyobText);

                        Pile.LogMessage("MYOB: End");
                        AppendMyobText("Done");
                    }
                }
                else
                {
                    AppendMyobText("MYOB Export already running");
                    Pile.LogMessage("MYOB Export already running");
                }
            }

            if (!MYOB.IsRunning)
            {
                if (!FileSystem.IsRunning)
                {
                    Pile.LogMessage("FS: Start");
                    AppendText("FS: Start");

                    await FileSystem.Export(Pile, S3, AppendText, AppendThreadText, AppendUploadStatusText, AppendUploadedText, AppendSkippedText, HandleUploadCompleted);
                }
                else if (FileSystem.IsUploadingStop)
                {
                    FileSystem.ResumeUploading();
                }
                else
                {
                    AppendText("FS Export already running");
                    Pile.LogMessage("FS Export already running");
                }
            } else
            {
                AppendMyobText("MYOB Export already running, cannot run file");
                Pile.LogMessage("MYOB Export already running, cannot run file");
            }
        }
    }
}
