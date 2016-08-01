using System;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using Microsoft.ServiceFabric.Services.Runtime;

namespace LaputanDesigns.ServiceFabricHosting
{
    /// <summary>
    /// todo: make each of these an ICommunicationListener ??
    /// so each time a service closes (assuming all services aren't closed), 
    /// then only this listener is (todo: somehow) removed from Kestrel's list of listen urls??
    /// is this even possible?? will sf name server [effectively] handle this business automagically??
    /// it doesn't seem possible (from a glance) to stop only one of kestrel's listeners...
    /// 
    /// that said, if there are multiple services, a single one shouldn't bring down the entire WebHost unless
    /// * it was the last service running
    /// * the app is shutting down (and/or service was deleted)
    /// 
    /// </summary>
    public class ServiceInfo
    {
        public ServiceInfo(ServiceConfig config, ICodePackageActivationContext activationContext)
        {
            Config = config;

            ServiceTypeDescription = activationContext.GetServiceTypes().SingleOrDefault(t => t.ServiceTypeName == config.ServiceTypeName);
            if (ServiceTypeDescription == null)
            {
                throw new ArgumentException("Unknown service type: " + config.ServiceTypeName, nameof(config.ServiceTypeName));
            }

            var endpoint = activationContext.GetEndpoint(Config.HttpEndpointName);
            string ipAddressOrFqdn = "+"; // FabricRuntime.GetNodeContext().IPAddressOrFQDN; // todo: why ever use '+' ??

            // appRoot disambiguates multiple services (both stateful and stateless)
            // note: for stateless services, it won't conform to "{PartitionId}/{ReplicaId}" postfix
            AppRoot = Config.AppRoot;
            if (string.IsNullOrWhiteSpace(AppRoot))
            {
                AppRoot = Guid.NewGuid().ToString();
            }

            ListeningAddress = $"{endpoint.Protocol.ToString().ToLower()}://{ipAddressOrFqdn}:{endpoint.Port}/{AppRoot}";

            // string ipAddressOrFQDN = FabricRuntime.GetNodeContext().IPAddressOrFQDN;
            // PublishAddress = ListeningAddress.Replace("+", ipAddressOrFQDN);

            PublishEndpointName = Config.PublishHttpEndpointName ?? Config.HttpEndpointName;
        }

        public ServiceConfig Config { get; }

        public ServiceTypeDescription ServiceTypeDescription { get; }

        public string ListeningAddress { get; }

        public string AppRoot { get; }

        public string PublishEndpointName { get; }


        public IWebHostCommunicationListener WebHostCommunicationListener { get; set; }

        /// <summary>
        /// todo: clean this up; make subclasses
        /// </summary>
        internal volatile StatelessService StatelessService = null;
        internal volatile StatefulService StatefulService = null;
    }
}
