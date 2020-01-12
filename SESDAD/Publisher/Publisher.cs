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
        //bool to check is system is in mode total order.
        internal static bool totalOrder = false;

        public Publisher(string name, string pubURL, string brkURL, int order)
        {
            myURL = pubURL;
            brokerURL = brkURL;
            myPort = parseURL(pubURL);
            processname = name;
            if (order == 1)
                totalOrder = true;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Publisher publisher = new Publisher(args[0], args[1], args[2], Int32.Parse(args[3]));

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemotePublisher), "pub", WellKnownObjectMode.Singleton);

            publisher.ConnectToBroker();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PublisherForm());

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


    [Serializable]
    class RemotePublisher : MarshalByRefObject, PublisherInterface
    {

        private static BrokerInterface broker = Publisher.broker;
        private static string myURL = Publisher.myURL;
        private static string myTopic;
        private static bool totalOrder = Publisher.totalOrder;
        // Controls number of publications done by this publisher
        int publications = 0;
        // Mutex to control acces to publications variable
        private static Mutex publicationsMut = new Mutex();
        // bool to tell if process is freezed. 0 = NOT FREEZED; 1 = FREEZED
        private int isFreeze = 0;
        // List of functions to call when the process is unfreezed
        private List<Action> functions = new List<Action>();

        public void ChangeTopic(string topic)
        {
            if (isFreeze == 0)
            {
                myTopic = topic;
                do {
                    try {
                        broker.ChangePublishTopic(myURL, topic);
                        // Success! Exit loop
                        break;
                    } catch (System.Net.Sockets.SocketException) {
                        // Broker is down. Waiting till he returns from doctor's appointment
                        Console.WriteLine("Can't connect to broker... waiting and trying again");
                        System.Threading.Thread.Sleep(5000);
                    }
                } while(true);
            }
            else { functions.Add(() => this.ChangeTopic(topic)); }
        }

        public void SendPublication(string publication)
        {
            if (isFreeze == 0) {
                if (myTopic != null) {
                    if (totalOrder) {
                        SendLogEvent();
                        do {
                            try {
                                SendPublicationToBroker(publication, broker.GetTicket());
                                break;
                            } catch (System.Net.Sockets.SocketException) {
                                // Broker is down. Waiting till he returns from doctor's appointment
                                Console.WriteLine("Can't connect to broker... waiting and trying again");
                                System.Threading.Thread.Sleep(5000);
                            }
                        } while (true);
                    } else {
                        SendLogEvent();
                        do {
                            try {
                                SendPublicationToBroker(publication);
                                break;
                            } catch (System.Net.Sockets.SocketException) {
                                // Broker is down. Waiting till he returns from doctor's appointment
                                Console.WriteLine("Can't connect to broker... waiting and trying again");
                                System.Threading.Thread.Sleep(5000);
                            }
                        } while (true);
                    }
                } else {
                    System.Windows.Forms.MessageBox.Show("Please select a topic to publish to");
                }
            } else { functions.Add(() => this.SendPublication(publication)); }
        }

        public void MultipleSendPublication(string publication, int sleepInterval, int numberofevents)
        {
            if (isFreeze == 0) {
                new Thread(() => {
                    int sequenceNumber = 0;
                    for (int i = 0; i < numberofevents; i++) {
                        sequenceNumber += 1;
                        if (myTopic != null) {
                            if (totalOrder) {
                                SendLogEvent();
                                do {
                                    try {
                                        SendPublicationToBroker(publication, broker.GetTicket());
                                    } catch (System.Net.Sockets.SocketException) {
                                        // Broker is down. Waiting till he returns from doctor's appointment
                                        Console.WriteLine("Can't connect to broker... waiting and trying again");
                                        System.Threading.Thread.Sleep(5000);
                                    }
                                } while (true);
                            } else {
                                SendLogEvent();
                                do {
                                    try {
                                        SendPublicationToBroker(publication);
                                    } catch (System.Net.Sockets.SocketException) {
                                        // Broker is down. Waiting till he returns from doctor's appointment
                                        Console.WriteLine("Can't connect to broker... waiting and trying again");
                                        System.Threading.Thread.Sleep(5000);
                                    }
                                } while (true);
                            }
                        }
                        System.Threading.Thread.Sleep(sleepInterval);
                    }
                }).Start();
            }
            else { functions.Add(() => this.MultipleSendPublication(publication, sleepInterval, numberofevents)); }
        }

        private void SendPublicationToBroker(string publication, int ticket = -1) {
            publicationsMut.WaitOne();
            if (ticket != -1) {
                broker.ReceivePublicationTOTAL(publication, myURL, myTopic, myURL, ticket);
            } else {
                broker.ReceivePublication(publication, myURL, topicName, myURL, publications);
            }
            publications++;
            publicationsMut.ReleaseMutex();
        }

        private void SendLogEvent() {
            PMInterface puppetMaster = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
            puppetMaster.UpdateEventLog("PubEvent", myURL, myURL, topicName);
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
            foreach (var function in functions)
            {
                function.Invoke();
            }
            functions.Clear();
            isFreeze = 0;
        }

        // Gives a status report on the node
        // This includes saying it's alive and what the current publishing topic is
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