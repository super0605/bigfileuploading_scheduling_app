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
using System.Threading.Tasks;
using System.Windows;

namespace FYIStockPile.Storage
{
    public delegate void Del(string text);

    public class MyobRepository
    {

        private LiteDatabase Database;
        public bool IsRunning = false;

        public MyobRepository(LiteDatabase db)
        {
            this.Database = db;
        }

        public bool ConnectionTest(AppRepository app)
        {
            var databaseName = app.Settings.MyobDatabaseName;
            var serverName = app.Settings.MyobServerName;

            if (!string.IsNullOrEmpty(databaseName) && !string.IsNullOrEmpty(serverName))
            {
                string connectionString = GetConnectionString(app);

                var sqlcon = new SqlConnection(connectionString);
                try
                {
                    sqlcon.Open();
                    sqlcon.Close();
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return false;
                }
            }

            return false;
        }

        public async Task<bool> Export(AppRepository app, S3StorageRepository S3, Del message)
        {
            IsRunning = true;
            LoadDocuments(app, message);
            await ExportDocuments(app, S3, message);

            IsRunning = false;
            return false;
        }

        private async Task ExportDocuments(AppRepository app, S3StorageRepository S3, Del message)
        {
            var documents = Database.GetCollection<Document>("myob_documents");

            var count = 0;
            var file_index = 0;
            var file_path = "";
            var file_name = "";
            StreamWriter jsonwrite = null;

            var records = documents.FindAll();
            foreach (var record in records)
            {
                if (count % 50000 == 0)
                {
                    await CloseAndUpload(app, S3, message, jsonwrite, file_path, file_name);
                    file_name = "index_" + file_index + ".json";
                    message("Creating export file " + file_name);

                    file_path = Environment.CurrentDirectory + "\\" + file_name;
                    FileStream fs = File.Create(file_path);
                    fs.Close();
                    file_index++;

                    jsonwrite = new StreamWriter(file_path, true);
                }

                string json = JsonConvert.SerializeObject(record, Formatting.None);

                jsonwrite.WriteLine(json);

                count++;
            }

            await CloseAndUpload(app, S3, message, jsonwrite, file_path, file_name);
        }

        private async Task CloseAndUpload(AppRepository app, S3StorageRepository S3, Del message, StreamWriter jsonwrite, string file_path, string file_name)
        {
            if (jsonwrite != null)
            {
                jsonwrite.Close();

                StorageRequest storage = new StorageRequest
                {
                    Name = file_name,
                    Type = "import",
                    EncryptionKeyId = null,
                    Path = file_path,
                };

                message("Uploading " + file_name);
                await S3.UploadAsync(app, storage);
            }
        }

        private void LoadDocuments(AppRepository app, Del message)
        {
            string connectionString = GetConnectionString(app);
            var sqlcon = new SqlConnection(connectionString);
            var sqlcon2 = new SqlConnection(connectionString);

            message("Getting documents remaining to be exported");

            int DocumentCount = GetDocumentCount(sqlcon, app.Settings.LastDocumentId, app.Settings.LastDocumentModified);

            var more = true;
            var counter = 0;

            var documents = Database.GetCollection<Document>("myob_documents");
            documents.EnsureIndex(x => x.Reference_Number);

            while (more)
            {
                more = false;
                SqlCommand command = GetDocumentQuery(sqlcon, app.Settings.LastDocumentId, app.Settings.LastDocumentModified);

                message("Refreshing dataset to export");
                sqlcon.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    try
                    {
                        while (reader.Read())
                        {
                            var document = new Document();
                            document.Import(reader);

                            GetDocumentCategories(sqlcon2, document);

                            app.Settings.LastDocumentId = document.DocumentId;
                            if (document.DateModified > app.Settings.LastDocumentModified || app.Settings.LastDocumentModified == null)
                            {
                                app.Settings.LastDocumentModified = document.DateModified;
                            }

                            counter++;
                            message("Collecting " + counter + " of " + DocumentCount.ToString());
                            more = true;

                            SaveDocument(documents, document);

                            app.Save();
                        }
                    }
                    catch (Exception e)
                    {
                        message(e.Message);
                    }
                }

                sqlcon.Close();
            }

            message("Done loading documents");
        }

        private void SaveDocument(LiteCollection<Document> documents, Document document)
        {

            var existing = documents.FindOne(x => x.Reference_Number == document.Reference_Number);
            if (existing == null)
            {
                documents.Insert(document);
            }
            else
            {
                document.Id = existing.Id;
                documents.Update(document);
            }
        }

        private static string GetConnectionString(AppRepository app)
        {
            var databaseName = app.Settings.MyobDatabaseName;
            var serverName = app.Settings.MyobServerName;

            var connectionString = "SERVER =" + serverName + "; Trusted_Connection=true;INITIAL CATALOG=" + databaseName + "; Connection Timeout=30";
            return connectionString;
        }

        private static SqlCommand GetDocumentQuery(SqlConnection sqlcon, int last_document_id, DateTime? last_modified_on)
        {
            var query = "select D.DocumentId as DocumentId, " +
                                     "D.Title as Title, " +
                                     "FC.Name as CabinetName, " +
                                     "FC.Name as CabinetName, " +
                                     "CreatedUser.Email as CreatedEmail, " +
                                     "ModifiedUser.Email as ModifiedEmail, " +
                                     "DV.DateCreated as DateCreated, " +
                                     "DV.DateModified as DateModified, " +
                                     "SubPath as SubPath, " +
                                     "FileName as FileName, " +
                                     "Con.FName as FirstName, " +
                                     "Con.LName as LastName, " +
                                     "Con.EMail as Email, " +
                                     "ISNULL(CliSup.ClientCode, Con.LNameFName) as ClientCode, " +
                                     "DV.DocumentStatusId as DocumentStatusId, " +
                                     "DV.VersionId as VersionId " +
                             "from dbo.DM_Document as D " +
                             "JOIN dbo.DM_FilingCabinet as FC ON D.FilingCabinetId = FC.FilingCabinetId " +
                             "JOIN dbo.Contact as Con ON D.ContactId = Con.ContactID " +
                             "LEFT OUTER JOIN dbo.ClientSupplier as CliSup ON CliSup.ContactID = Con.ContactID " +
                             "JOIN dbo.DM_DocumentVersion as DV ON(D.DocumentId = DV.DocumentId)" +
                             "LEFT OUTER JOIN dbo.Contact as CreatedUser ON DV.CreatedBy = CreatedUser.ContactId  " +
                             "LEFT OUTER JOIN dbo.Contact as ModifiedUser ON DV.ModifiedBy = ModifiedUser.ContactId  ";

            if (last_modified_on == null)
            {
                query += " where D.DocumentId > " + last_document_id.ToString();
            }
            else
            {
                query += " where DV.DateModified >= " + ((DateTime)last_modified_on).ToString("YYYY-MM-DD");
            }

            query += " and D.IsTrashed = 0 ";

            query += " ORDER BY DV.DateModified ASC ";
            SqlCommand command = null;

            try
            {
                //Your insert code here
                command = new SqlCommand(
                             query
                         , sqlcon);

                command.CommandTimeout = 6000;
                return command;
            }
            catch (System.Data.SqlClient.SqlException sqlException)
            {
                Console.WriteLine(sqlException.Message);
                Console.WriteLine("sql Error exception");
            }
            return command;
        }

        private static int GetDocumentCount(SqlConnection sqlcon, int last_document_id, DateTime? last_modified_on)
        {

            var DocumentCount = 0;

            var query = "select count(*) from dbo.DM_Document D " +
                "JOIN dbo.DM_DocumentVersion as DV ON(D.DocumentId = DV.DocumentId)  ";

            if (last_modified_on == null)
            {
                query += " where D.DocumentId > " + last_document_id.ToString();
            }
            else
            {
                query += " where DV.DateModified >= " + ((DateTime)last_modified_on).ToString("YYYY-MM-DD");
            }

            try
            {
                SqlCommand rowCountCommand = new SqlCommand(query, sqlcon);
                sqlcon.Open();
                rowCountCommand.CommandTimeout = 6000;
                using (SqlDataReader noOfRows = rowCountCommand.ExecuteReader())
                {
                    while (noOfRows.Read())
                    {
                        DocumentCount = (int)noOfRows[0];
                    }
                }
                sqlcon.Close();
            }
            catch (System.Data.SqlClient.SqlException sqlException)
            {
                Console.WriteLine(sqlException.Message);
                Console.WriteLine("sql Error exception");
            }

            return DocumentCount;
        }

        private static int GetDocumentCategories(SqlConnection sqlcon, Document document)
        {
            var DocumentCount = 0;
            SqlCommand command = new SqlCommand(
                "select EF.FieldName as CategoryName, EV.Value as CategoryValue " +
                "from DM_ExtraDocument ED " +
                "JOIN dbo.ExtraValue as EV ON EV.ExtraValueID = ED.ExtraValueID " +
                "JOIN dbo.ExtraField as EF ON EF.ExtraFieldID = EV.ExtraFieldID " +
                "where ED.DocumentID = " + document.DocumentId.ToString()
            , sqlcon);

            sqlcon.Open();

            command.CommandTimeout = 6000;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    document.ImportCategories(reader);
                }
            }
            sqlcon.Close();
            return DocumentCount;
        }
    }
}
