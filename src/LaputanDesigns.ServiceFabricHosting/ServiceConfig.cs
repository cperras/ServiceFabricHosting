
namespace LaputanDesigns.ServiceFabricHosting
{
    public class ServiceConfig
    {
        public string ServiceTypeName { get; set; }

        public string HttpEndpointName { get; set; }

        /// <summary>
        /// if not set, a random Guid will be used
        /// </summary>
        public string AppRoot { get; set; }

        /// <summary>
        /// friendly name of endpoint
        /// </summary>
        public string PublishHttpEndpointName { get; set; }
    }
}
