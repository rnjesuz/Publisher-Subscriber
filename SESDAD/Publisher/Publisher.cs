using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    public class Publisher
    {

        internal static BrokerInterface broker;
        internal static string brokerURL;
        internal static string myURL;
        internal static int myPort;
        private string processname;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //TODO remove after PuppetMaster is implemented
            //myURL = "tcp://localhost:8088/pub";
            //TODO remove after PuppetMaster is implemented
            //myPort = 8088;
            Publisher publisher = new Publisher(args[0], args[1], args[2]);

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            //TODO remove after PuppetMaster is implemented
            //brokerURL = "tcp://localhost:8086/broker";
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotePublisher),"pub",WellKnownObjectMode.Singleton);

            publisher.ConnectToBroker();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PublisherForm());
            
        }

        public Publisher(string name, string pubURL, string brkURL)
        {
            myURL = pubURL;
            brokerURL = brkURL;
            myPort = parseURL(pubURL);
            processname = name;
        }

        public void ConnectToBroker()
        {
            broker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brokerURL);

            try
            {
                broker.ConnectPublisher(myURL);
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

    class RemotePublisher : MarshalByRefObject, PublisherInterface
    {

        private BrokerInterface broker = Publisher.broker;
        private string myURL = Publisher.myURL;
        string myTopic;

        //bool to tell if process is freezed. 0 = NOT FREEZED; 1 = FREEZED
        private int isFreeze = 0;

        //List of functions to call when the process is unfreezed
        private List<Action> functions = new List<Action>();

        public void ChangeTopic(string Topic)
        {
            if (isFreeze == 0)
            {
                myTopic = Topic;
                try {
                    broker.ChangePublishTopic(myURL, Topic);
                }
                catch (System.Net.Sockets.SocketException)
                {
                    Console.WriteLine("can't connect to broker");
                }
            }
            else { functions.Add(() => this.ChangeTopic(Topic)); }
        }

        public void SendPublication(string publication)
        {
            if (isFreeze == 0)
            {
                if (myTopic != null)
                {
                    PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                    PM.UpdateEventLog("PubEvent", myURL, myURL, myTopic);
                    try {
                        broker.ReceivePublication(publication, myURL, myTopic);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("can't connect to broker");
                    }

                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Please select a topic to publish to");
                }
            }
            else { functions.Add(() => this.SendPublication(publication)); }
        }

        public void Kill()
        {
            if (isFreeze == 0)
            {
                Application.Exit();
            }
            else { functions.Add(() => this.Kill()); }
        }

        public void Freeze()
        {
            isFreeze = 1;
        }
        public void Unfreeze()
        {
            isFreeze = 0;
            foreach (var function in functions)
            {
                function.Invoke();
            }
            functions.Clear();
        }

        //gives a status report on the node
        //this includes saying its alive and the current publishing topic
        public void StatusUpdate()
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[Status Publisher]");
                Console.WriteLine("I'm alive at: " + myURL);
                Console.WriteLine("My current publishing topic is: " + myTopic);
            }
            else { functions.Add(() => this.StatusUpdate()); }
        }
    }
}