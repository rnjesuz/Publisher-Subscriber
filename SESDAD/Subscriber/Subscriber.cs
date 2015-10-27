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
        internal static SubscriberForm form;
        internal static RemoteSubscriber rs;
        internal static string myURL;
        internal static string brokerURL;
        internal static int myPort;
        private string processname;

        [STAThread]
        static void Main()
        {
            //TODO remove after PuppetMaster is implemented
            myURL = "tcp://localhost:8090/sub";
            //TODO remove after PuppetMaster is implemented
            myPort = 8090;

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            //TODO remove after PuppetMaster is implemented
            brokerURL = "tcp://localhost:8086/broker";

            broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface),brokerURL);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new SubscriberForm();

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteSubscriber), "sub", WellKnownObjectMode.Singleton);

            try
            {
                broker.ConnectSubscriber(myURL);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate Broker");
            }

            
            Application.Run(form);
        }

        public Subscriber(string subURL, string brkURL, int subPort, string name)
        {
            myURL = subURL;
            brokerURL = brkURL;
            myPort = subPort;
            processname = name;
        }
    }


    delegate void DelegateReceivePublication(string message);

    public class RemoteSubscriber : MarshalByRefObject, SubscriberInterface
    {
        SubscriberForm form = Subscriber.form;
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
                
            }
        }

        internal void RemoveSubscription(string topic)
        {
            try
            {
                broker.RemoveSubscription(myURL, topic);
            }
            catch(SocketException)
            {
                System.Console.WriteLine("Could not locate Broker");
            }
        }
    }


}
