namespace OvsDiscoveryAgent
{
    public class VirtualAdapter
    {
        public VirtualAdapter()
        {
            SwitchId = OvsPortName = Id = VmId = string.Empty;
        }
        public VirtualAdapter(string id, string vmId, string switchId, string ovsPort)
        {
            Id = id;
            SwitchId = switchId;
            OvsPortName = ovsPort;
        }
        public string SwitchId { get; set; }
        public string OvsPortName { get; set; }
        public string Id { get; set; }
        public string VmId { get; set; }
        public EnabledState State { get; set; }
        public override bool Equals(object obj)
        {
            var otherAdapter = obj as VirtualAdapter;
            if (otherAdapter == null) return false;
            return SwitchId == otherAdapter.SwitchId &&
                   OvsPortName == otherAdapter.OvsPortName &&
                   Id == otherAdapter.Id &&
                   State == otherAdapter.State &&
                   VmId == otherAdapter.VmId;
        }
        public override int GetHashCode()
        {
            return string.Format("{0}{1}{2}{3}{4}", SwitchId, Id, OvsPortName, VmId, State).GetHashCode();
        }
    }
}
