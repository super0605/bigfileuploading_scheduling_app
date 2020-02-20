using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FYIStockPile.Models
{
    public class Document
    {
        public int Id { get; set; }

        [JsonProperty("document_id")]
        public int DocumentId { get; set; }

        [JsonProperty("reference_number")]
        public string Reference_Number { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("created_by")]
        public string CreatedBy { get; set; }
        [JsonProperty("modified_by")] 
        public string ModifiedBy { get; set; }
        [JsonProperty("CabinetName")]
        public string CabinetName { get; set; }
        [JsonProperty("file_path")] 
        public string FilePath { get; set; }

        [JsonProperty("name")] 
        public string Name { get; set; }

        [JsonProperty("date_created")] 
        public DateTime DateCreated { get; set; }

        [JsonProperty("date_modified")] 
        public DateTime DateModified { get; set; }

        [JsonProperty("contact")] 
        public Client Contact { get; set; }  = new Client();

        [JsonProperty("categories")] 
        public Dictionary<string, string> Categories { get; set; }  = new Dictionary<string, string>();

        public void Import(SqlDataReader reader)
        {
            DocumentId = (int)reader["DocumentId"];
            CabinetName = reader["CabinetName"].ToString();
            CreatedBy = reader["CreatedEmail"] == null ? "" : reader["CreatedEmail"].ToString();
            ModifiedBy = reader["ModifiedEmail"] == null ? "" : reader["ModifiedEmail"].ToString();
            Name = reader["Title"] == null ? "" : reader["Title"].ToString();

            DateCreated = (DateTime)reader["DateCreated"];
            DateModified = (DateTime)reader["DateModified"];

            string sub_path = (string)reader["SubPath"];
            sub_path = sub_path != null ? sub_path.Trim() : "";

            FilePath = sub_path + @"\" + reader["FileName"].ToString().Trim();

            var VersionId = (int)reader["VersionId"];

            Reference_Number = DocumentId.ToString() + "_" + VersionId.ToString();

            var status = (int)reader["DocumentStatusId"];
            switch (status) {
                case 1 :{
                        status = 1;
                        break;
                }
                case 2 :{
                        status = 3;
                        break;
                }
                case 3 :{
                        status = 6;
                        break;
                }
                case 4 :{
                        status = 4;
                        break;
                }
                case 7 :{
                        status = 7;
                        break;
                }
            }

             Status = status;

            Contact.Import(reader);
        }

        public void ImportCategories(SqlDataReader reader)
        {
            Categories.Add(reader["CategoryName"].ToString(), reader["CategoryValue"].ToString());
        }
    }
}
