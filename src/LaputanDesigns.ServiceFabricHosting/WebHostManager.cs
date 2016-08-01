using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace LaputanDesigns.ServiceFabricHosting
{
    /// <summary>
    /// todo: ServiceManager ??
    /// </summary>
    public interface IWebHostManager
    {
        /// <summary>
        /// todo: all registered urls (via IServerAddressesFeature)
        /// vs: all registered urls via list of ServiceInfo...
        /// currently returns the latter. 
        /// </summary>
        string[] ListenerUrls { get; }

        IConfigurationRoot ConfigurationRoot { get; }

        IWebHost WebHost { get; set; }

        Task<T> RegisterServiceAsync<T>(
            ServiceConfig serviceConfig,
            Func<StatelessServiceContext, T> serviceFactory,
            TimeSpan timeout,
            CancellationToken cancellationToken) where T : StatelessService;

        /// <summary>
        /// todo: bit of a hack; return false if the service fails to register
        /// (eg: has already been configured). for some reason, this is being called during shutdown!!
        /// </summary>
        /// <param name="serviceConfig"></param>
        /// <returns></returns>
        bool RegisterServiceConfig(ServiceConfig serviceConfig);

        ServiceInstanceListener CreateServiceInstanceListener(StatelessService statelessService);
    }

    public interface ICommunicationManager : IWebHostManager
    {
        void OnAbort(IWebHostCommunicationListener listener);
        Task OnOpenAsync(IWebHostCommunicationListener listener, CancellationToken cancellationToken);
        Task OnCloseAsync(IWebHostCommunicationListener listener, CancellationToken cancellationToken);
    }

    public class WebHostManager : ICommunicationManager
    {
        /// <summary>
        /// key: ServiceTypeName
        /// </summary>
        private readonly Dictionary<string, IWebHostCommunicationListener> _services = new Dictionary<string, IWebHostCommunicationListener>();

        private readonly CodePackageActivationContext _activationContext = null;

        private volatile IWebHost _webHost;
        private bool _disposed;

        public WebHostManager(IConfigurationRoot configurationRoot)
        {
            ConfigurationRoot = configurationRoot;


            // cdp: noted that, when deleting a service, Main starts again and the app might hang here
            // todo: give this a timeout, then bail (failfast)??
            // var activationContext = FabricRuntime.GetActivationContext();

            // var timeout = new CancellationToken(1000);
            try
            {
                _activationContext = FabricRuntime.GetActivationContextAsync(TimeSpan.FromSeconds(5), CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetActivationContextAsync", ex);
                Environment.FailFast("GetActivationContextAsync");
            }

        }

        public IConfigurationRoot ConfigurationRoot { get; }

        public IWebHost WebHost { get { return _webHost; } set { _webHost = value; } }

        public string[] ListenerUrls
        {
            get
            {
                var urls = _services.Values.Select(s => s.ServiceInfo.ListeningAddress).ToArray();
                return urls;
            }
        }

        public bool RegisterServiceConfig(ServiceConfig serviceConfig)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("WebHostManager.RegisterServiceConfig");
            }

            // todo: validate
            lock (_services)
            {
                string serviceTypeName = serviceConfig.ServiceTypeName;
                if (_services.ContainsKey(serviceTypeName))
                {
                    return false;
                }

                var serviceInfo = new ServiceInfo(serviceConfig, _activationContext);
                var commListener = new WebHostCommunicationListener(this, serviceInfo);
                _services.Add(serviceTypeName, commListener);
                return true;
            }
        }

        public ServiceInstanceListener CreateServiceInstanceListener(StatelessService statelessService)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("WebHostManager.CreateServiceInstanceListener");
            }

            // todo: validate
            var commListener = _services[statelessService.Context.ServiceTypeName];
            return new ServiceInstanceListener(_ => commListener, commListener.ServiceInfo.PublishEndpointName);
        }

        public async Task<T> RegisterServiceAsync<T>(
            ServiceConfig serviceConfig,
            Func<StatelessServiceContext, T> serviceFactory,
            TimeSpan timeout,
            CancellationToken cancellationToken) where T : StatelessService
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("WebHostManager.RegisterServiceAsync");
            }

            string serviceTypeName = serviceConfig.ServiceTypeName;
            await ServiceRuntime.RegisterServiceAsync(
                serviceTypeName,
                serviceContext => _services[serviceTypeName].ServiceInfo.StatelessService = serviceFactory(serviceContext),
                // serviceContext => _statelessService = serviceFactory(serviceContext),
                timeout,
                cancellationToken
                );

            // todo: on shutdown (service deletion anyway), Main restarts, and this fails (endless loop)

            int maxErrorCount = 100;
            int errorCount = 0;
            while (_services[serviceTypeName].ServiceInfo.StatelessService == null)
            {
                Debug.WriteLine("_statelessService null...");
                // todo: check if closing down ??
                ++errorCount;
                if (errorCount >= maxErrorCount)
                {
                    // note: debugger stays attached...
                    Debug.WriteLine("maxErrorCount reached in RegisterServiceAsync: " + serviceTypeName);
                    // Environment.FailFast("maxErrorCount reached in RegisterServiceAsync");
                    return null; // maybe other services exist?? does that even make sense??
                }

                // await Task.Delay(20, cancellationToken);
                Thread.Sleep(50);
            }

            return _services[serviceTypeName].ServiceInfo.StatelessService as T;
        }

        public void OnAbort(IWebHostCommunicationListener listener)
        {
            OnCloseAsync(listener, CancellationToken.None).GetAwaiter().GetResult();

        }

        public async Task OnOpenAsync(IWebHostCommunicationListener listener, CancellationToken cancellationToken)
        {
            // wait for WebHost.... do so on first listener
            while (!_disposed && _webHost == null)
            {
                Debug.WriteLine("webHost null...");
                await Task.Delay(20, cancellationToken);
            }
        }

        public Task OnCloseAsync(IWebHostCommunicationListener listener, CancellationToken cancellationToken)
        {
            lock (_services)
            {
                // if all services are closed, dispose of WebHost
                if (!_services.Values.Any(s => s.IsOpen))
                {
                    WebHost?.Dispose();
                    _disposed = true;
                }
                
                return Task.FromResult(true);
            }
        }
    }
}
