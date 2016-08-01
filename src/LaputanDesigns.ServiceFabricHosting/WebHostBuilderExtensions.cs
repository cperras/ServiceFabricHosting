using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace LaputanDesigns.ServiceFabricHosting
{
    public static class WebHostBuilderExtensions
    {
        public static IWebHostBuilder RegisterServiceFabricService<TServiceType, T>(
            this IWebHostBuilder webHostBuilder,
            IWebHostManager manager, // listener,
            ServiceConfig serviceConfig,
            Func<StatelessServiceContext, T> serviceFactory,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken))
            where TServiceType : class
            where T : StatelessService, TServiceType
        {

            // todo: do not inject url; let callers explicitly set this (so they can customize)

            string serviceTypeName = serviceConfig.ServiceTypeName;
            if (!manager.RegisterServiceConfig(serviceConfig))
            {
                Debug.WriteLine("ServiceFabric service config failed to register: " + serviceTypeName);
                return webHostBuilder;
            }

            return webHostBuilder.ConfigureServices(services =>
            {
                if (services.FirstOrDefault(s => s.ServiceType == typeof(IWebHostManager)) == null)
                {
                    services.AddSingleton(manager);
                }

                T statelessService = manager.RegisterServiceAsync(
                    serviceConfig,
                    serviceContext => statelessService = serviceFactory(serviceContext),
                    timeout,
                    cancellationToken
                ).GetAwaiter().GetResult();

                if (statelessService != null)
                {
                    services.AddSingleton<TServiceType>(statelessService);
                    Debug.WriteLine("ServiceFabric service registered: " + serviceTypeName);
                }
                // services.AddSingleton<StatelessService>(statelessService); // todo: may be several of these; disambiguate??
                // services.AddSingleton<StatelessServiceContext>(statelessService.Context);
            });
        }
    }
}
