using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FYIStockPile.Interfaces
{
    public class Slack
    {
        public async Task TryPostJsonAsync(string message)
        {
            try
            {
                // Construct the HttpClient and Uri. This endpoint is for test purposes only.
                HttpClient httpClient = new HttpClient();
                //Uri uri = new Uri("");
                // Construct the JSON to post.
                /*
                 * HttpContent content = new HttpContent(
                    //"{ \"text\": \"Start s3 uploading\" }",
                    message,
                    Windows.Storage.Streams.UnicodeEncoding.Utf8,
                    "application/json");
                    */
                var content = new StringContent(message, Encoding.UTF8, "application/json");

                // Post the JSON and wait for a response.
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(
                    uri,
                    content);

                // Make sure the post succeeded, and write out the response.
                httpResponseMessage.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                // Write out any exceptions.       
                Console.WriteLine(ex);
            }
        }
    }
}
