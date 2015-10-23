using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            TcpChannel channel = new TcpChannel(8088);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(RemotePublisher),
                "PublisherServer",
                WellKnownObjectMode.Singleton);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    class RemotePublisher : MarshalByRefObject, PublisherInterface
    {
        private BrokerInterface Broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://localhost:8086/BrokerServer");

        public void ConnectBroker() {
           Broker.ConnectPublisher();
        }

        public void ChangeTopic(string Topic)
        {
            Broker.ChangePublishTopic(this, Topic);
        }
    }
}
