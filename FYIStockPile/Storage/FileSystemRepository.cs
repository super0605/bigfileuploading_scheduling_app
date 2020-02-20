using FYIStockPile.Models;
using FYIStockPile.Storage.Model;
using LiteDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FYIStockPile.Wizard;

namespace FYIStockPile.Storage
{
    public class FileSystemRepository
    {

        private LiteDatabase Database;
        public bool IsRunning = false;
        public int ThreadsNum = 5;
        public bool IsUploadingStop = false;

        public FileSystemRepository(LiteDatabase db)
        {
            this.Database = db;
        }


        public async Task<bool> Export(AppRepository app, S3StorageRepository S3, Del message, Del threadMessage, Del uploadStatusMessage, Del uploadedMessage, Del skippedMessage, Del handleUploadCompleted)
        {
            IsRunning = true;
            IsUploadingStop = false;
            await LoadDocuments(app, message, threadMessage, uploadStatusMessage, uploadedMessage, skippedMessage, handleUploadCompleted);

            return false;
        }


        private async Task<bool> LoadDocuments(AppRepository app, Del message, Del threadMessage, Del uploadStatusMessage, Del uploadedMessage, Del skippedMessage, Del handleUploadCompleted)
        {
            app.LogMessage("FS: Getting documents remaining to be exported");
            uploadStatusMessage("Getting documents remaining to be exported");

            var fileList = new List<FileInfo>();
            var folderList = new List<DirectoryInfo>();
            foreach (string folder in app.Settings.FolderLists)
            {
                DirectoryInfo d = new DirectoryInfo(folder);
                var r = d.GetFiles("*.*", SearchOption.AllDirectories);
                fileList.AddRange(r);
                uploadStatusMessage("Loading subfolders " + d.Name);
                for (int i = 0; i < r.Count(); i++)
                {
                    folderList.Add(d);
                }
            }
            FileInfo[] Files = fileList.ToArray();
            DirectoryInfo[] Dirs = folderList.ToArray();

            var fileIndex = 0;
            var completedThreads = 0;
            var fileCounts = Files.Count();

            var skipped = 0;
            var uploaded = 0;
            var errors = 0;

            var documents = Database.GetCollection<Document>("documents");
            documents.EnsureIndex(x => x.FilePath);
            ThreadsNum = ThreadsNum < fileCounts ? ThreadsNum : fileCounts;

            Thread[] threadsArray = new Thread[ThreadsNum];
            for (int i = 0; i < ThreadsNum; i++)
            {
                threadsArray[i] = new Thread(async (object param) =>
                {
                    int threadId = (int)param;
                    while (fileIndex < fileCounts)
                    {
                        if (IsUploadingStop)
                        {
                            Thread.Sleep(1000);
                        }
                        else 
                        {
                            try
                            {
                                var activeIndex = fileIndex;
                                fileIndex++;

                                if (activeIndex < ThreadsNum)
                                {
                                    activeIndex = threadId;
                                }

                                var file = Files[activeIndex];

                                Document document = new Document();
                                document.FilePath = file.FullName;
                                document.DateModified = file.LastWriteTimeUtc;

                                uploadStatusMessage(GetWorkingFolder(Dirs[activeIndex].ToString()));

                                var existing = documents.FindOne(x => x.FilePath == document.FilePath);

                                StorageRequest storage = new StorageRequest
                                {
                                    Path = file.FullName,
                                    Name = file.Name,
                                    Type = makeCloudFolderPath(Dirs[activeIndex].ToString(), file.Name, file.FullName, app.Settings.IncludeFolderName),
                                    EncryptionKeyId = null,
                                };

                                S3StorageRepository s3 = new S3StorageRepository(Database);

                                if (existing == null || existing.DateModified < document.DateModified)
                                {
                                    var display = "";
                                    var uploadResponse = false;
                                    try
                                    {
                                        string statusofThread = MakeStatusOfThread(threadId, file.Length, file.Name);
                                        threadMessage(statusofThread);
                                        uploadResponse = await s3.UploadAsync(app, storage);
                                    }
                                    catch (Exception e)
                                    {
                                        app.LogMessage(file.FullName + " - " + e.Message);
                                        errors++;
                                        display = activeIndex.ToString() + " of " + fileCounts.ToString() + " ERROR";
                                    }

                                    try
                                    {
                                        if (uploadResponse)
                                        {
                                            if (existing == null)
                                            {
                                                documents.Insert(document);
                                                display = activeIndex.ToString() + " of " + fileCounts.ToString() + " UPLOADED";
                                            }
                                            else
                                            {
                                                existing.DateModified = file.LastWriteTimeUtc;
                                                documents.Update(existing);
                                                display = activeIndex.ToString() + " of " + fileCounts.ToString() + " UPDATED";
                                            }

                                            uploaded++;

                                            string _uploadedMsg = "Completed " + uploaded.ToString() + " of " + fileCounts.ToString();
                                            uploadedMessage(_uploadedMsg);
                                            string _skippedMsg = "Skipped " + skipped.ToString() + " of " + fileCounts.ToString();
                                            skippedMessage(_skippedMsg);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        app.LogMessage(file.FullName + " - Error during LiteDB - " + e.Message);
                                        errors++;
                                        display = activeIndex.ToString() + " of " + fileCounts.ToString() + " ERROR";
                                    }

                                    message(display);
                                }
                                else
                                {
                                    var display = activeIndex.ToString() + " of " + fileCounts.ToString() + " SKIPPED";
                                    message(display);
                                    skipped++;

                                    string _uploadedMsg = "Completed " + uploaded.ToString() + " of " + fileCounts.ToString();
                                    uploadedMessage(_uploadedMsg);
                                    string _skippedMsg = "Skipped " + skipped.ToString() + " of " + fileCounts.ToString();
                                    skippedMessage(_skippedMsg);
                                }

                            }
                            catch (Exception e)
                            {
                                app.LogMessage("Error during LiteDB - " + e.Message);
                                errors++;
                            }
                        }
                    }

                    if (threadId < threadsArray.Length)
                    {
                        completedThreads++;
                        message("FS: Thread " + threadId + " DONE");

                        string statusofThread = MakeStatusOfThread(threadId, 0, "Finished");
                        threadMessage(statusofThread);

                        if (completedThreads + 1 > ThreadsNum)
                        {
                            IsRunning = false;
                            message("FS: Skipped " + skipped.ToString());
                            message("FS: Uploaded " + uploaded.ToString());
                            message("FS: Done loading documents");

                            app.LogMessage("FS: Skipped " + skipped.ToString());
                            app.LogMessage("FS: Uploaded " + uploaded.ToString());
                            app.LogMessage("FS: Done loading documents");

                            handleUploadCompleted("completed");

                            if (uploaded > 0)
                            {
                                string endMessage = "{\"text\":\"End uploading\",\"blocks\":[{\"type\": \"section\",\"text\": {\"type\": \"mrkdwn\",\"text\": \"End uploading:\"}},{\"type\": \"section\",\"block_id\": \"section789\",\"fields\": [{\"type\": \"mrkdwn\",\"text\": \">*Bucket* : " + app.Settings.BucketName + "\n>*Uploaded* : " + uploaded.ToString() + "\n>*Skipped:* : " + skipped.ToString() + "\n*Errors:* : " + errors.ToString() + "\n>\"}]}]}";
                                await app.Slack.TryPostJsonAsync(endMessage);
                            }

                        }
                    }
                });

                threadsArray[i].Start(i);
            }
            return true;

        }

        public void StopUploading()
        {
            IsUploadingStop = true;
        }

        public void ResumeUploading()
        {
            IsUploadingStop = false;
        }

        private string makeCloudFolderPath(string rootFolder, string fileName, string fullPath, bool includeFolderName)
        {
            string result = "import";
            string parentFolder = "";
            string[] splitStr1 = fullPath.Split(new string[] { rootFolder }, StringSplitOptions.RemoveEmptyEntries);
            string _result = splitStr1[0];

            string[] _rootFolder = rootFolder.Split(new String[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
            if (_rootFolder.Length > 0)
            {
                parentFolder = _rootFolder[_rootFolder.Length - 1];
            }

            if (includeFolderName)
            {
                result = result + "/" + parentFolder;
            }

            if (_result == "\\" + fileName)
            {
                _result = "";
            }
            else
            {
                string[] splitStr2 = _result.Split(new string[] { "\\" + fileName }, StringSplitOptions.RemoveEmptyEntries);
                _result = splitStr2[0];
            }

            if (_result != "")
            {
                String[] chunkFolders = _result.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string chunkFolder in chunkFolders)
                {
                    if (chunkFolder != "")
                    {
                        result += "/" + chunkFolder;
                    }
                }
            }

            return result;
        }

        private static string SizeTypeCheck(long totalSizes)
        {
            float size = 0;
            string unit = "KB";
            string result = "";

            if (totalSizes < (long)Math.Pow(2, 20))
            {
                size = totalSizes / (long)Math.Pow(2, 10);
                unit = "KB";
            }
            else if ((long)Math.Pow(2, 10) < totalSizes && totalSizes < (long)Math.Pow(2, 30))
            {
                size = totalSizes / (long)Math.Pow(2, 20);
                unit = "MB";
            }
            else if (totalSizes > (long)Math.Pow(2, 30))
            {
                size = totalSizes / (long)Math.Pow(2, 30);
                unit = "GB";
            }

            result = size.ToString() + " " + unit;

            return result;
        }

        private string MakeStatusOfThread(int threadIdx, long fileSize, string filePath)
        {
            string result = "THREAD";
            result = result + " " + (threadIdx + 1).ToString() + ": Uploading - " + SizeTypeCheck(fileSize) + " - " + filePath;
            if (fileSize == 0 && filePath == "Finished")
            {
                result = "THREAD " + (threadIdx + 1).ToString() + ": Finished";
            }

            return result;
        }

        private string GetWorkingFolder(string folderPath)
        {
            string result = "Working on Folder ";
            result = result + folderPath.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries).Last();

            return result;
        }
    }
}
