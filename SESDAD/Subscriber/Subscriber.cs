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
    public class Subscriber
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
        static void Main(string[] args)
        {
            /*//TODO remove after PuppetMaster is implemented
            myURL = "tcp://localhost:8090/sub";
            //TODO remove after PuppetMaster is implemented
            myPort = 8090;*/

            Subscriber subscriber = new Subscriber(args[0], args[1], args[2]);

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            //TODO remove after PuppetMaster is implemented
            //brokerURL = "tcp://localhost:8086/broker";            

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteSubscriber), "sub", WellKnownObjectMode.Singleton);
            subscriber.ConnectToBroker();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new SubscriberForm();
            Application.Run(form);
        }

        public Subscriber(string name, string subURL, string brkURL)
        {
            myURL = subURL;
            brokerURL = brkURL;
            myPort = parseURL(subURL);
            processname = name;
        }

        public void ConnectToBroker()
        {
            broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brokerURL);

            try
            {
                broker.ConnectSubscriber(myURL);
            }
            catch (SocketException)
            {
                System.Console.WriteLine("Could not locate Broker");
            }
        }

        public int parseURL(string url)
        {
            string[] parsedURL = url.Split(':');  //parsedURL[0] = "tcp"; parsedURL[1]= "//localhost"; parsedURL[2]= "PORT/broker";
            string[] parsedURLv2 = parsedURL[2].Split('/'); //parsedURLv2[0] = "PORT"; parsedURLv2[1]= "broker";
            myPort = int.Parse(parsedURLv2[0]);
            return myPort;
        }
    }


    delegate void DelegateReceivePublication(string message);

    public class RemoteSubscriber : MarshalByRefObject, SubscriberInterface
    {
        SubscriberForm form = Subscriber.form;
        private BrokerInterface broker = Subscriber.broker;
        private string myURL = Subscriber.myURL;

        public void ReceivePublication(string publication, string pubURL, string pubTopic)
        {
            PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
            PM.UpdateEventLog("SubEvent", myURL, pubURL, pubTopic);

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
