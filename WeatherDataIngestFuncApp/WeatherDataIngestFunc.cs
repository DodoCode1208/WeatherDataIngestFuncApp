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

        //Injecting registered classes under startUp.cs via Constructor
        public WeatherDataIngestFunc(IHttpClientFactory httpClientFactory, IWriteToBlob writeToBlob, IOptions<StorageConfigOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient();
            _writeToBlob = writeToBlob;
            _options = options.Value;
        }

        [FunctionName("WeatherDataIngestFunc")]
        public async Task Run([TimerTrigger("*/5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"WeatherDataIngestFunc function executed at: {DateTime.Now}");

            try
            {
                // Calling the (OpenWeather) public api to ingest data 
                _httpClient.BaseAddress = new Uri(_options.baseUrl);
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                //Adding the query parameters to the Request Uri:q={CityName},appid={applicationkey},units={temperature in Celsius}
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

                // Write api response json data to local storage
                var fileName = "WeatherDataJson" + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var path = string.Concat(@"C:\dev\bdo\Dump\", fileName);
                bool isSucceded = WriteToLocalFile(path, result);

                //On Successful reponse and reponse is not null , storing Json data under blob.
                if (isSucceded)
                {
                    var blobFileName = string.Concat("WeatherData", "_", DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss"));
                    await _writeToBlob.WriteToblob(result, blobFileName);
                }

            }
            catch(Exception e)
            {
                log.LogError( $"Exception occured:{e.Message}");
            }
        }

        private bool WriteToLocalFile(string path, string content)
        {

            if (File.Exists(path))
                File.WriteAllText(path, content);
            else
            {
                //Create new file at specified path on stream
                using (FileStream stream = File.Create(path))
                {
                    StreamWriter writer = new StreamWriter(stream);
                    writer.WriteLine(content);
                    writer.Close();
                }
            }
               
            return true;
        }
    }
}
