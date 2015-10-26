using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    class Subscriber
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 

        internal static BrokerInterface broker;
        internal static Form1 form;
        internal static RemoteSubscriber rs;
        internal static string myURL;
        internal static string brokerURL;
        internal static int myPort;
        [STAThread]
        static void Main()
        {
            //TODO remove after PuppetMaster is implemented
            myURL = "tcp://localhost:8090/SubscriberServer";
            //TODO remove after PuppetMaster is implemented
            myPort = 8090;

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            //TODO remove after PuppetMaster is implemented
            brokerURL = "tcp://localhost:8086/BrokerServer";

            broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),brokerURL);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteSubscriber), "SubscriberServer", WellKnownObjectMode.Singleton);

            try
            {
                broker.ConnectSubscriber(myURL);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate Broker");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new Form1();
            Application.Run(form);
        }

        public Subscriber(string subURL, string brkURL, int subPort)
        {
            myURL = subURL;
            brokerURL = brkURL;
            myPort = subPort;
        }
    }


    delegate void DelegateReceivePublication(string message);

    public class RemoteSubscriber : MarshalByRefObject, SubscriberInterface
    {
        public static Form1 form = Subscriber.form;
        private BrokerInterface broker = Subscriber.broker;
        private string myURL = Subscriber.myURL;

        public void ReceivePublication(string publication)
        {
            form.Invoke(new DelegateReceivePublication(form.UpdatePublication), publication);
        }

        internal void AddSubscription(string topic)
        {
            try
            {
                broker.AddSubscription(myURL, topic);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate Broker");
            }
        }
    }


}
