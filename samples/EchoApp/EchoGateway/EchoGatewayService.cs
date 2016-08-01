using LaputanDesigns.ServiceFabricHosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;

namespace EchoGateway
{
    public class EchoGatewayServiceConfig : ServiceConfig
    {
        public EchoGatewayServiceConfig()
        {
            /*
            ServiceTypeName = "EchoGatewayType";
            HttpEndpointName = "HttpEndpoint";
            PublishHttpEndpointName = "Http Endpoint";
            */
        }

        public int CloseTimeoutMs { get; set; }
    }

    /// <summary>
    /// note: don't inherit from IStatelessUserServiceInstance
    /// </summary>
    public interface IEchoGatewayService 
    {
    }

    public class EchoGatewayService : StatelessService, IEchoGatewayService
    {
        private readonly IWebHostManager _webHostCommunicationManager;
        private readonly EchoGatewayServiceConfig _config;


        public EchoGatewayService(
            StatelessServiceContext serviceContext, 
            IWebHostManager webHostManager,
            EchoGatewayServiceConfig config
            ) 
            : base(serviceContext)
        {
            if (webHostManager == null)
            {
                throw new ArgumentNullException(nameof(webHostManager));
            }
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _webHostCommunicationManager = webHostManager;
            _config = config;
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                _webHostCommunicationManager.CreateServiceInstanceListener(this),
            };
        }
    }
}
