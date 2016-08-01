using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LaputanDesigns.ServiceFabricHosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Services.Runtime;

namespace EchoGateway
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // env.EnvironmentName

            /*
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            */
        }

        public IConfigurationRoot ConfigurationRoot { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // services.AddServiceFabricService<EchoGatewayService>();

            services.AddOptions();

            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            var sfListener = app.ApplicationServices.GetRequiredService<IWebHostManager>();
            ConfigurationRoot = sfListener.ConfigurationRoot;

            // var sfService = app.ApplicationServices.GetRequiredService<StatelessService>();
            // app.UseServiceFabricService<EchoGatewayService>;

            // loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            // loggerFactory.AddDebug();

            app.UseMvc();
        }
    }
}
