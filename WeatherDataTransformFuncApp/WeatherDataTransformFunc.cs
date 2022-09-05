using System;
using System.IO;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Documents.SystemFunctions;
using Microsoft.OData.Edm;

namespace WeatherDataTransformFuncApp
{
    public class WeatherDataTransformFunc
    {
        private string jsonContent = string.Empty;
        JsonSerializerOptions options;

        [FunctionName("WeatherDataTransformFunc")]
        public void Run([BlobTrigger("weatherdataingestcontainer/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob,
            string name, ILogger log, [Blob("weatherdataosloloc/{name}", FileAccess.Write,Connection = "AzureWebJobsStorage")]Stream transformBlob)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            using (StreamReader sr = new StreamReader(myBlob))
            {
                string jsonContent = sr.ReadToEnd();
            }

            // Create a JsonNode DOM from a JSON string.
            //Parse Jsonstring - To mutable DOM and make changes to the DOM elements (Input - data coming from weatherdataingestcontainer/{blobname})
            JsonNode weatherNode = JsonNode.Parse(jsonContent.ToString()!);
            Console.WriteLine(weatherNode!.ToJsonString(options));


            //Remove the nodes which are not required from weather parent node
            JsonNode updatedRootNode;
            RemoveJsonNodes(weatherNode, out updatedRootNode);

            //Update the nodes with value in corrected format ()
            JsonNode _updatedRootNode;
            List<string> elementList = new List<string> { "dt" };
            UpdateJsonNodes(weatherNode,elementList, out _updatedRootNode);

            using (StreamWriter writer = new StreamWriter(transformBlob))
            {
                writer.Write(updatedRootNode.ToJsonString(options));  
            }
        }

        private void RemoveJsonNodes(JsonNode rootNode, out JsonNode updatedRootNode)
        {
            options = new JsonSerializerOptions { WriteIndented = true };
 

            var nodeKeys = rootNode.AsObject().Select(x => x.Key).ToList();

            var mutableObj = rootNode.AsObject()!;

            foreach (var key in nodeKeys)
            {
                List<string> lis = new List<string> { "coord", "base", "sys", "cod" };
                if (lis.Contains(key))
                {
                    mutableObj.Remove(key);
                }
            };

            updatedRootNode = JsonNode.Parse(mutableObj.ToJsonString(options));
        }

        private void UpdateJsonNodes(JsonNode rootNode , List<string> elementList, out JsonNode _updatedRootNode)
        {
            foreach(var prop in elementList)
            {
                var elementValue = rootNode[prop].GetValue<DateTime>();

                var modifiedValue = !elementValue.IsNull() ? ConvertUTCToLocalDateTime(elementValue) : DateTime.Now;
            }

            _updatedRootNode = JsonNode.Parse(rootNode.ToJsonString(options));
        }

        private DateTime ConvertUTCToLocalDateTime(DateTime utcDatTime)
        {
           TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
           return TimeZoneInfo.ConvertTimeFromUtc(utcDatTime, localTimeZone);   
        }
    }
}
