using System;
using System.Management;

namespace OvsDiscoveryAgent
{
    public class WqlColumnNames
    {
        public static readonly string SystemName = "SystemName";
        public static readonly string EnabledState = "EnabledState";
        public static readonly string ElementName = "ElementName";
        public static readonly string HostResource = "HostResource";
        public static readonly string InstanceID = "InstanceID";
        public static readonly string Caption = "Caption";
    }
    public class WqlObject
    {
        public string Name { get; private set; }
        public WqlObject() { }
        public WqlObject(string type)
        {
            Name = type;
        }
        public override string ToString()
        {
            return Name;
        }
    }
    public class WqlEventType : WqlObject
    {
        public WqlEventType() { }
        public WqlEventType(string type) : base(type) { }
        public static bool operator ==(WqlEventType type, ManagementBaseObject mbo)
        {
            return type.Name == mbo.ClassPath.ClassName;
        }
        public static bool operator !=(WqlEventType type, ManagementBaseObject mbo)
        {
            return !(type == mbo);
        }
        public static bool operator == (WqlEventType type, EventArrivedEventArgs args)
        {
            return type == args.NewEvent;
        }
        public static bool operator !=(WqlEventType type, EventArrivedEventArgs args)
        {
            return !(type == args);
        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Name == obj.ToString();
        }
        public static readonly WqlEventType Operation = new WqlEventType("__InstanceOperationEvent");
        public static readonly WqlEventType Modification = new WqlEventType("__InstanceModificationEvent");
        public static readonly WqlEventType Creation = new WqlEventType("__InstanceCreationEvent");
        public static readonly WqlEventType Deletion = new WqlEventType("__InstanceDeletionEvent");
    }
    public class WqlTableName : WqlObject
    {
        public WqlTableName() { }
        public WqlTableName(string scopeName, string tableName) : base(tableName)
        {
            ScopeName = scopeName;
        }
        public override string ToString()
        {
            return string.Format("{0}:{1}", ScopeName, Name);
        }
        public string ScopeName { get; private set; }
        private static readonly string virtScope = @"\root\virtualization\v2";
        public static readonly WqlTableName SyntheticEthernetPort = new WqlTableName(virtScope, "Msvm_SyntheticEthernetPort");
        public static readonly WqlTableName EthernetSwitchExtension = new WqlTableName(virtScope, "Msvm_EthernetSwitchExtension");
        public static readonly WqlTableName EthernetPortAllocationSettingData = new WqlTableName(virtScope, "Msvm_EthernetPortAllocationSettingData");
    }

    public enum EnabledState
    {
        Unknown = 0,
        Enabled = 2,
        Disabled = 3,
        Paused = 32768,
        Suspended = 32769,
        Starting = 32770,
        Snapshotting = 32771,
        Saving = 32773,
        Stopping = 32774,
        Pausing = 32776,
        Resuming = 32777,
    }

    public class WqlHelper
    {
        public static ManagementEventWatcher GetEventWatcher(WqlEventType eventType, WqlTableName tableName)
        {
            var scope = new ManagementScope(tableName.ScopeName);
            scope.Connect();
            var query = new WqlEventQuery(eventType.Name,
                                          TimeSpan.FromSeconds(1),
                                          string.Format("TargetInstance ISA '{0}'", tableName.Name));
            return new ManagementEventWatcher(scope, query);
        }
        public static ManagementEventWatcher GetEventWatcher(WqlEventType eventType, WqlTableName tableName,
                                                             string additionalConditions)
        {
            var scope = new ManagementScope(tableName.ScopeName);
            scope.Connect();
            var query = new WqlEventQuery(eventType.Name,
                                          TimeSpan.FromSeconds(1),
                                          string.Format("TargetInstance ISA '{0}' AND ({1})", tableName.Name,
                                                        additionalConditions));
            return new ManagementEventWatcher(scope, query);
        }
        public static ManagementObjectCollection QueryAll(WqlTableName tableName, string condition)
        {
            return QueryAll(tableName, condition, new string[] { "*" });
        }
        public static ManagementObjectCollection QueryAll(WqlTableName tableName, string condition, string[] fields)
        {
            string queryStr;
            if (string.IsNullOrWhiteSpace(condition))
            {
                queryStr = string.Format("SELECT {0} FROM {1}", string.Join(",", fields), tableName.Name);
            }
            else
            {
                queryStr = string.Format("SELECT {0} FROM {1} WHERE {2}", string.Join(",", fields), tableName.Name, condition);
            }
            var searcher = new ManagementObjectSearcher(tableName.ScopeName, queryStr);
            return searcher.Get();
        }
        public static Tuple<ManagementBaseObject, ManagementBaseObject> ParseEventArrivedEventArgs(EventArrivedEventArgs args)
        {
            ManagementBaseObject oldMbo = null;
            ManagementBaseObject newMbo = null;
            if (WqlEventType.Modification == args)
            {
                oldMbo = (ManagementBaseObject)args.NewEvent["PreviousInstance"];
                newMbo = (ManagementBaseObject)args.NewEvent["TargetInstance"];
            }
            else if (WqlEventType.Creation == args)
            {
                newMbo = (ManagementBaseObject)args.NewEvent["TargetInstance"];
            }
            else if (WqlEventType.Deletion == args)
            {
                oldMbo = (ManagementBaseObject)args.NewEvent["TargetInstance"];
            }
            return new Tuple<ManagementBaseObject, ManagementBaseObject>(oldMbo, newMbo);
        }
    }
}
