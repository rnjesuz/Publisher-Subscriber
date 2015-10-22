using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace SESDAD
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(RemoteServer),
                "ChatServer",
                WellKnownObjectMode.Singleton);
        }
    }

    class Broker: BrokerInterface
    {
        //list of every publisher connected to this Broker
        List<PublisherInterface> publishers = new List<PublisherInterface>();
        //list of every subscriber connected to this Broker
        List<SubscriberInterface> subscribers = new List<SubscriberInterface>();
        //Father node in the Broker Tree. CANNOT be NULL
        BrokerInterface fatherBroker;
        //Child node in the Broker Tree. CAN be NULL
        BrokerInterface childBroker;
    }
}
