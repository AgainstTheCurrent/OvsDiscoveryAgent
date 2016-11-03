namespace OvsDiscoveryAgent
{
    public class VirtualAdapterManager
    {
        public void Start()
        {
            OvsSwitchMonitor.Instance.Subscribe();
            VirtualAdapterMonitor.Instance.Subscribe();
            OvsSwitchMonitor.Instance.ItemUpdated += OvsSwitchMonitor_ItemUpdated;
            VirtualAdapterMonitor.Instance.ItemUpdated += VirtualAdapterMonitor_ItemUpdated;
        }

        private void VirtualAdapterMonitor_ItemUpdated(object sender, WmiMonitorEventArgs<VirtualAdapter> e)
        {
            throw new System.NotImplementedException();
        }

        private void OvsSwitchMonitor_ItemUpdated(object sender, WmiMonitorEventArgs<OvsSwitch> e)
        {
            throw new System.NotImplementedException();
        }

        public void Stop()
        {
            OvsSwitchMonitor.Instance.Unsubscribe();
            VirtualAdapterMonitor.Instance.Unsubscribe();
        }
    }
}
