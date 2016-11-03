using System.ServiceProcess;
using System.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OvsDiscoveryAgent
{
    public class OvsDiscoveryService: ServiceBase
    {
#region Console related functions
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                default:
                    VirtualAdapterMonitor.Instance.Unsubscribe();
                    return false;
            }
        }

        /// <summary>
        /// Start the service as a console application.
        /// </summary>
        /// <param name="args">Startup parameters.</param>
        public void Start(string[] args)
        {
            _handler += Handler;
            OnStart(args);
            (new ManualResetEvent(false)).WaitOne();
        }
#endregion

        protected override void OnStart(string[] args)
        {
            var worker = new BackgroundWorker();
            worker.DoWork += (s, e) =>
            {

            };
            worker.RunWorkerAsync();
        }

        protected override void OnStop()
        {

        }
    }
}
