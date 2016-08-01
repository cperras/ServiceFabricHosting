using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace LaputanDesigns.ServiceFabricHosting
{
    public class WebHostCommunicationListener : IWebHostCommunicationListener
    {
        private readonly ICommunicationManager _manager;
        private bool _isOpen;

        public WebHostCommunicationListener(ICommunicationManager manager, ServiceInfo serviceInfo)
        {
            _manager = manager;
            ServiceInfo = serviceInfo;
        }

        public ServiceInfo ServiceInfo { get; }

        public bool IsOpen => _isOpen;

        public void Abort()
        {
            _isOpen = false;
            _manager.OnAbort(this);
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            await _manager.OnOpenAsync(this, cancellationToken);
            _isOpen = true;

            string address = ServiceInfo.ListeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);
            return address;
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _isOpen = false;
            return _manager.OnCloseAsync(this, cancellationToken);
        }
    }
}
