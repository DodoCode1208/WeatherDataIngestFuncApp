using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Headers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Options;

namespace WeatherDataIngestFuncApp
{
    public class WeatherDataIngestFunc
    {
        private readonly HttpClient _httpClient;
        private readonly IWriteToBlob _writeToBlob;
        private readonly StorageConfigOptions _options;

        HttpRequestMessage _httpRequest;
        HttpResponseMessage _httpResponse;

        public WeatherDataIngestFunc(IHttpClientFactory httpClientFactory, IWriteToBlob writeToBlob, IOptions<StorageConfigOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient();
            _writeToBlob = writeToBlob;
            _options = options.Value;
        }

        [FunctionName("WeatherDataIngestFunc")]
        public async Task Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Calling the public api to ingest data 

            _httpClient.BaseAddress = new Uri(_options.baseUrl);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-Api-Key", "0a31e3da37638afea62a98c6d5960b38");

            //Adding the query parameters to the Request Uri: 
            var queryParam = new Dictionary<string, string>
            {
                {"q","Oslo" },
                {"appid","0a31e3da37638afea62a98c6d5960b38" },
                {"units","metric" }
            };

            var requestUri = QueryHelpers.AddQueryString("data/2.5/weather?", queryParam);

            _httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            _httpResponse = await _httpClient.SendAsync(_httpRequest);

            _httpResponse.EnsureSuccessStatusCode();

            var result = await _httpResponse.Content.ReadAsStringAsync();
            log.LogInformation($"JsonResponse : {result}");

            bool isSucceded = WriteToLocalFile(@"C:\dev\bdo\Dump\Json.txt", result);

            if (isSucceded)
            {
                var blobFileName = string.Concat("WeatherData", DateTime.Now);
                await _writeToBlob.WriteToblob(result,blobFileName);
            }

            dynamic content = JsonConvert.DeserializeObject<dynamic>(result);

            Console.WriteLine(content);


        }

        private bool WriteToLocalFile(string path, string content)
        {
            File.WriteAllText(path, Convert.ToString(content));
            return true;
        }
    }
}
