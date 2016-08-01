using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace LaputanDesigns.ServiceFabricHosting
{
    public interface IWebHostCommunicationListener : ICommunicationListener
    {
        ServiceInfo ServiceInfo { get; }

        bool IsOpen { get; }
    }
}
