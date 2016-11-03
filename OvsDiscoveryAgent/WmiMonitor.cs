using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Diagnostics;

namespace OvsDiscoveryAgent
{
    public enum ItemOperation
    {
        Add,
        Remove,
        Modify
    }
    public class WmiMonitorEventArgs<T> : EventArgs
    {
        public T OldValue { get; set; }
        public T NewValue { get; set; }
        public ItemOperation Operation { get; set; }
        public ManagementBaseObject NewEvent { get; set; }
    }
    public delegate void WmiMonitorEventHandler<T>(object sender, WmiMonitorEventArgs<T> e);
    public abstract class WmiMonitor<T> where T : class
    {
        protected ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();
        public Dictionary<string, T> ItemsMap { get; private set; }
        protected WmiMonitor()
        {
            ItemsMap = new Dictionary<string, T>();
        }
        public event WmiMonitorEventHandler<T> ItemUpdated;
        protected abstract ManagementObjectCollection QueryItems();
        protected abstract ManagementEventWatcher CreateEventWatcher();
        protected abstract string GetItemId(T item);
        protected abstract T ConvertItem(ManagementBaseObject mbo);
        public void EnterReadLock()
        {
            cacheLock.EnterReadLock();
        }
        public void ExitReadLock()
        {
            cacheLock.ExitReadLock();
        }
        public bool Contains(string id)
        {
            cacheLock.EnterReadLock();
            try
            {
                return ItemsMap.ContainsKey(id);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }
        private bool AddOrDeleteItem(ManagementBaseObject mo, bool isAdd)
        {
            T item = ConvertItem(mo);
            string id = GetItemId(item);
            if (item == null)
            {
                Trace.TraceError("Cannot parse {0} as item {1}", mo.ToString(),
                                 typeof(T).ToString());
                return false;
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                Trace.TraceError("Cannot find ID for {0}", mo.ToString());
                return false;
            }
            if (isAdd)
            {
                if (ItemsMap.ContainsKey(id))
                {
                    Trace.TraceError("Duplicate ID {0} found for {1}", id.ToString(),
                                     typeof(T).ToString());
                    return false;
                }
                else
                {
                    ItemsMap.Add(id, item);
                    return true;
                }
            }
            else
            {
                if (!ItemsMap.ContainsKey(id))
                {
                    Trace.TraceError("Unable to find {1} that has ID {0}", id.ToString(),
                                     typeof(T).ToString());
                    return false;
                }
                else
                {
                    ItemsMap.Remove(id);
                    return true;
                }
            }
        }
        public void LoadAllItems()
        {
            var result = QueryItems();
            cacheLock.EnterWriteLock();
            ItemsMap.Clear();
            foreach (var mo in result)
            {
                AddOrDeleteItem(mo, true);
            }
            cacheLock.ExitWriteLock();
        }
        public void Subscribe()
        {
            if (EventWatcher != null) return;
            EventWatcher = CreateEventWatcher();
            EventWatcher.EventArrived += EventWatcher_EventArrived;
            EventWatcher.Start();
        }

        public void Unsubscribe()
        {
            if (EventWatcher == null) return;
            EventWatcher.EventArrived -= EventWatcher_EventArrived;
            EventWatcher.Stop();
            EventWatcher = null;
        }

        protected abstract bool IsSameItem(T oldItem, T newItem);

        private void EventWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            var changed = WqlHelper.ParseEventArrivedEventArgs(e);
            T oldItem = default(T), newItem = default(T);
            if (changed.Item1 != null)
            {
                oldItem = ConvertItem(changed.Item1);
            }
            if (changed.Item2 != null)
            {
                newItem = ConvertItem(changed.Item2);
            }
            if (IsSameItem(oldItem, newItem))
            {
                //Nothing relevant is changed.
                return;
            }
            var args = new WmiMonitorEventArgs<T>();
            bool updateResult;
            args.NewEvent = e.NewEvent;
            args.OldValue = oldItem;
            args.NewValue = newItem;
            cacheLock.EnterWriteLock();
            if (IsSameItem(oldItem, null) && !IsSameItem(newItem, null))
            {
                args.Operation = ItemOperation.Remove;
                updateResult = AddOrDeleteItem(changed.Item1, false);
            }
            else if (!IsSameItem(oldItem, null) && IsSameItem(newItem, null))
            {
                args.Operation = ItemOperation.Add;
                updateResult = AddOrDeleteItem(changed.Item2, true);
            }
            else
            {
                args.Operation = ItemOperation.Modify;
                updateResult = AddOrDeleteItem(changed.Item1, false) && AddOrDeleteItem(changed.Item2, true);
            }
            cacheLock.ExitWriteLock();
            if (updateResult)
            {
                cacheLock.EnterReadLock();
                ItemUpdated?.Invoke(this, args);
                cacheLock.ExitReadLock();
            }
        }
        protected ManagementEventWatcher EventWatcher { get; set; }
    }
}
