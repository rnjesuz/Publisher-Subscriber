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
    class Publisher
    {

        internal static BrokerInterface broker;
        internal static string brokerURL;
        internal static string myURL;
        internal static int myPort;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //TODO remove after PuppetMaster is implemented
            myURL = "tcp://localhost:8088/PublisherServer";
            //TODO remove after PuppetMaster is implemented
            myPort = 8088;

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            //TODO remove after PuppetMaster is implemented
            brokerURL = "tcp://localhost:8086/BrokerServer";

            broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brokerURL);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotePublisher),"PublisherServer",WellKnownObjectMode.Singleton);

            try
            {
                broker.ConnectPublisher(myURL);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate Broker");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public Publisher(string pubURL, string brkURL, int pubPort)
        {
            myURL = pubURL;
            brokerURL = brkURL;
            myPort = pubPort;
        }
    }

    class RemotePublisher : MarshalByRefObject, PublisherInterface
    {

        private BrokerInterface broker = Publisher.broker;
        private string myURL = Publisher.myURL;

        public void ChangeTopic(string Topic)
        {
            broker.ChangePublishTopic(myURL, Topic);
        }
    }
}
