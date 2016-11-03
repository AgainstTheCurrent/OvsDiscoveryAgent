using System.Management;

namespace OvsDiscoveryAgent
{
    public class OvsSwitch
    {
        public string Id { get; set; }
        public EnabledState State { get; set; }
    }
    public class OvsSwitchMonitor : WmiMonitor<OvsSwitch>
    {
        protected OvsSwitchMonitor() { }
        private static OvsSwitchMonitor monitorInstance;
        public static OvsSwitchMonitor Instance
        {
            get
            {
                if (monitorInstance == null)
                {
                    monitorInstance = new OvsSwitchMonitor();
                }
                return monitorInstance;
            }
        }

        private static readonly string OvsExtentionName = "Open vSwitch Extension";

        protected override ManagementObjectCollection QueryItems()
        {
            return WqlHelper.QueryAll(WqlTableName.EthernetSwitchExtension,
                string.Format("{0} = '{1}' AND {2} = {3}",
                WqlColumnNames.ElementName, OvsExtentionName,
                WqlColumnNames.EnabledState, (int)EnabledState.Enabled),
                new string[] { WqlColumnNames.SystemName });
        }
        protected override string GetItemId(OvsSwitch item)
        {
            return item.Id;
        }

        protected override OvsSwitch ConvertItem(ManagementBaseObject mbo)
        {
            if (mbo == null) return null;
            var result = new OvsSwitch();
            result.Id = mbo[WqlColumnNames.SystemName] as string;
            result.State = (EnabledState)((int)mbo[WqlColumnNames.EnabledState]);
            return result;
        }

        protected override bool IsSameItem(OvsSwitch oldItem, OvsSwitch newItem)
        {
            bool oldEnabled = (oldItem != null && oldItem.State == EnabledState.Enabled);
            bool newEnabled = (oldItem != null && oldItem.State == EnabledState.Enabled);
            if (oldEnabled != newEnabled) return false;
            if (oldItem != null && newItem != null && oldItem.Id != newItem.Id) return false;
            return true;
        }

        protected override ManagementEventWatcher CreateEventWatcher()
        {
            return WqlHelper.GetEventWatcher(WqlEventType.Operation,
                                             WqlTableName.EthernetSwitchExtension,
                                             string.Format("TargetInstance.{0} = '{1}'",
                                             WqlColumnNames.ElementName, OvsExtentionName));
        }
    }
}
