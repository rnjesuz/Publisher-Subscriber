using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    static class Publisher
    {

        internal static BrokerInterface broker;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            TcpChannel channel = new TcpChannel(8088);
            ChannelServices.RegisterChannel(channel, false);

            broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://localhost:8086/BrokerServer");

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(RemotePublisher),
                "PublisherServer",
                WellKnownObjectMode.Singleton);

            try
            {
                broker.ConnectPublisher("localhost:8088");
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate server");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    class RemotePublisher : MarshalByRefObject, PublisherInterface
    {

        private BrokerInterface broker = Publisher.broker;

        public void ChangeTopic(string Topic)
        {
            broker.ChangePublishTopic("localhost:8088", Topic);
        }
    }
}
