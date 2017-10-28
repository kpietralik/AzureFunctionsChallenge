#define DEBUG

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureFunctionsChallenge
{
    public static class SortArrayWriter
    {
        /// <summary>
        /// Azure Functions Challenge - Sort Array writer
        /// This function will save input int values array in Azure Table Storage       
        /// Required application settings:
        /// - SortArrayConnection - connection to Azure Table Storage 
        /// </summary>
        /// <param name="req">Request</param>
        /// <param name="outTable">Storage Table reference</param>
        /// <param name="log">Logger</param>
        /// <example>
        /// Headers:
        /// Content-Type = application/json
        /// Body:
        /// {
        ///     "key": "1b189de1-5c682222-49ab-aa3b-5a8311111",
        /// 	"ArrayOfValues": [95,45,34,3,700]
        /// }
        /// </example>
        /// <returns>
        /// Key and values count if in DEBUG mode
        /// </returns>
        /// <see cref="https://functionschallenge.azure.com/functions"/>
        [FunctionName("SortArrayWriter")]
        public static async Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, [Table("SortArray", Connection = "SortArrayConnection")]IAsyncCollector<DataTable> outTable, TraceWriter log)
        {
            log.Info("C# HTTP trigger SortArrayWriter");

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();
            string key = data?.key;
            List<int> values;

            // Get values
            try
            {
                values = ((JArray)data?.ArrayOfValues).Select(x => (int)x).ToList();
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            // Write to table
            values.ForEach(async value => await outTable.AddAsync(new DataTable()
            {
                PartitionKey = key,
                RowKey = Guid.NewGuid().ToString(),
                Value = value
            }));
            
            // Return
            log.Info($"Key = \"{key}\", values count = {values.Count}");
            var myObj = new { key = key, count = values.Count };
            var jsonToReturn = JsonConvert.SerializeObject(myObj);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
#if DEBUG
                Content = new StringContent(jsonToReturn, Encoding.UTF8, "application/json")
#endif            
            };
        }
    }
}