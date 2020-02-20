using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FYIStockPile.Models
{
    public class Client
    {

        public string FirstName { get; set; }
        public string LastName { get; set; }

        [JsonProperty("email")] 
        public string Email { get; set; }

        [JsonProperty("clientCode")] 
        public string ClientCode { get; set; }

        [JsonProperty("contact_name")] 
        public string ContactName { get; set; }


        public void Import(SqlDataReader reader)
        {
            FirstName = reader["FirstName"].ToString();
            LastName = reader["LastName"].ToString();

            Email = reader["Email"].ToString();
            ClientCode = reader["ClientCode"].ToString();

            if (LastName == null || LastName == "")
            {
                ContactName = FirstName.Trim();
            } else
            {
                ContactName = FirstName.Trim() + " " + LastName.Trim();
            }            
        }
    }
}
