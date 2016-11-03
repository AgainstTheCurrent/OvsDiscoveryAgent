using System;
using System.ServiceProcess;

namespace OvsDiscoveryAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!Environment.UserInteractive)
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new OvsDiscoveryService()
                };
                ServiceBase.Run(ServicesToRun);
            }
            else
            {
                OvsDiscoveryService service = new OvsDiscoveryService();
                service.Start(args);
            }
        }
    }
}
