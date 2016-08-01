using LaputanDesigns.ServiceFabricHosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;

namespace EchoGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // note: Main is being re-entered after (only) service is deleted...
                // calls like FabricRuntime.GetActivationContext hang indefinitely
                // todo: clarify expected behaviour on service delete

                // todo: how to get env ?? IHostingEnvironment is not available...
                // this is set in Properties/launchSettings.json...
                // perhaps set env var in SetupEntryPoint?
                string envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
                string contentRootPath = Directory.GetCurrentDirectory();

                var configRoot = new ConfigurationBuilder()
                    .SetBasePath(contentRootPath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                    // .Add(options)
                    .Build();

                var echoGatewayServiceConfig = new EchoGatewayServiceConfig();
                configRoot.GetSection("EchoGatewayServiceConfig").Bind(echoGatewayServiceConfig);

                // this needs all services up-front, so UseUrls can be initialized. 
                // todo: try to register config immediately, as actual sf registration must be delayed
                IWebHostManager webHostCommunicationManager = new WebHostManager(configRoot);

                var webHost = new WebHostBuilder()
                    .UseKestrel(opts =>
                    {
                        configRoot.GetSection("KestrelOptions")?.Bind(opts);
                    })
                    // .UseConfiguration(config) // not here??
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .RegisterServiceFabricService<IEchoGatewayService, EchoGatewayService>(
                        webHostCommunicationManager,
                        echoGatewayServiceConfig,
                        serviceContext =>
                            new EchoGatewayService(serviceContext, webHostCommunicationManager, echoGatewayServiceConfig))
                    .UseUrls(webHostCommunicationManager.ListenerUrls)
                    .UseStartup<Startup>()
                    .Build();

                webHostCommunicationManager.WebHost = webHost;
                webHost.Run();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Main caught ex: " + ex);
                // Console.WriteLine("Main caught ex: " + ex);
            }
        }
    }
}

