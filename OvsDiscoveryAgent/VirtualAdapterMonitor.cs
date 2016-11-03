using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;


namespace OvsDiscoveryAgent
{
    public class VirtualAdapterMonitor : WmiMonitor<VirtualAdapter>
    {
        protected VirtualAdapterMonitor() { }
        private static VirtualAdapterMonitor monitorInstance;
        private static readonly string VirtualAdapterCaption = "Ethernet Connection Settings";
        private static readonly string VirtualAdapterDefaultElementName = "Dynamic Ethernet Switch Port";
        private ReaderWriterLockSlim adapterMapLock = new ReaderWriterLockSlim();
        private Dictionary<string, VirtualAdapter> adapterMap = new Dictionary<string, VirtualAdapter>();
        public static VirtualAdapterMonitor Instance
        {
            get
            {
                if (monitorInstance == null)
                {
                    monitorInstance = new VirtualAdapterMonitor();
                }
                return monitorInstance;
            }
        }
        protected override ManagementObjectCollection QueryItems()
        {
            return WqlHelper.QueryAll(WqlTableName.EthernetPortAllocationSettingData,
                string.Format("{0} = '{1}' AND {2} = {3}", WqlColumnNames.Caption,
                VirtualAdapterCaption, WqlColumnNames.EnabledState, (int)EnabledState.Enabled),
                new string[] { WqlColumnNames.ElementName, WqlColumnNames.InstanceID,
                WqlColumnNames.HostResource });
        }

        protected override ManagementEventWatcher CreateEventWatcher()
        {
            return WqlHelper.GetEventWatcher(
               WqlEventType.Operation, WqlTableName.EthernetPortAllocationSettingData,
               string.Format("TargetInstance.{0} = '{1}'", WqlColumnNames.Caption, VirtualAdapterCaption));
        }

        protected override string GetItemId(VirtualAdapter item)
        {
            return item.Id;
        }

        protected override VirtualAdapter ConvertItem(ManagementBaseObject mbo)
        {
            if (mbo == null) return null;
            VirtualAdapter result = new VirtualAdapter();
            object o = mbo[WqlColumnNames.InstanceID];
            if (o != null)
            {
                var ids = o.ToString().Split(':', '\\');
                if (ids.Count() < 3)
                {
                    Trace.TraceError("Unable to parse virutal adapter ID: {0}", o.ToString());
                }
                else
                {
                    result.VmId = ids[1];
                    result.Id = ids[2];
                }
            }
            o = mbo[WqlColumnNames.ElementName];
            if (o != null)
            {
                var name = o.ToString();
                if (name != VirtualAdapterDefaultElementName)
                {
                    result.OvsPortName = name;
                }
            }
            o = mbo[WqlColumnNames.EnabledState];
            if (o != null)
            {
                result.State = (EnabledState)(int)o;
            }
            var resources = mbo[WqlColumnNames.HostResource] as string[];
            if (resources != null)
            {
                string pattern = "Msvm_VirtualEthernetSwitch.CreationClassName=" +
                                 "\"Msvm_VirtualEthernetSwitch\",Name=\"([A-Za-z0-9\\-]+)\"";
                foreach (string res in resources)
                {
                    Match match = Regex.Match(res, pattern, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        result.SwitchId = match.Groups[1].Value;
                    }
                }
            }
            return result;
        }

        protected override bool IsSameItem(VirtualAdapter oldItem, VirtualAdapter newItem)
        {
            bool oldEnabled = (oldItem != null && oldItem.State == EnabledState.Enabled);
            bool newEnabled = (oldItem != null && oldItem.State == EnabledState.Enabled);
            if (oldEnabled != newEnabled) return false;
            if (oldItem != null && !oldItem.Equals(newItem)) return false;
            return true;
        }
    }
}
