using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using Microsoft.Extensions.Azure;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Configuration;

[assembly: FunctionsStartup(typeof(WeatherDataIngestFuncApp.StartUp))]

namespace WeatherDataIngestFuncApp
{
    public class StartUp : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();
            builder.Services.AddAzureClients(c =>
            {
                c.AddBlobServiceClient("DefaultEndpointsProtocol=https;AccountName=bdo01azurestorcontainer;AccountKey=WBOyY9HSqHZjgmaOSTrs6O9td+zyJlVy1RY6NfI0FeX3y8dQcDP/xaJqjypexBUUIGA45reRoQDH+AStV/TV/w==;EndpointSuffix=core.windows.net");
                c.UseCredential(new DefaultAzureCredential());
            });

            builder.Services.AddSingleton<IWriteToBlob, WriteToBlob>();

            builder.Services.AddOptions<StorageConfigOptions>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("StorageConfig").Bind(settings);
            });
     
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
        }
    }
}
