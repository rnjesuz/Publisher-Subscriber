using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Threading;
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
    delegate void DelegateAddSubscriptionRemote(string topic);
    delegate void DelegateRemoveSubscriptionRemote(string topic);


    public class RemoteSubscriber : MarshalByRefObject, SubscriberInterface
    {
        SubscriberForm form = Subscriber.form;
        private BrokerInterface broker = Subscriber.broker;
        private string myURL = Subscriber.myURL;

        //bool to tell if process is freezed. 0 = NOT FREEZED; 1 = FREEZED
        private int isFreeze = 0;

        //List of functions to call when the process is unfreezed
        private List<Action> functions = new List<Action>();

        public void ReceivePublication(string publication, string pubURL, string pubTopic)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("received publication for my subscription");
                PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                PM.UpdateEventLog("SubEvent", myURL, pubURL, pubTopic);

                form.Invoke(new DelegateReceivePublication(form.UpdatePublication), publication);

                Console.WriteLine("finished receiving publication for my subscription");
            }
            else { functions.Add(() => this.ReceivePublication(publication, pubURL,  pubTopic)); }
        }

        public void AddSubscription(string topic)
        {
            if (isFreeze == 0)
            {
                try
                {
                    broker.AddSubscription(myURL, topic);
                }
                catch (SocketException)
                {
                    Console.WriteLine("can't connect to broker... waiting and trying again");
                    System.Threading.Thread.Sleep(5000);
                    try
                    {
                        broker.AddSubscription(myURL, topic);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("can't connect to broker");
                    }
                }
            }
            else { functions.Add(() => this.AddSubscription(topic)); }
        }

        public void RemoveSubscription(string topic)
        {
            if (isFreeze == 0)
            {
                try
                {
                    broker.RemoveSubscription(myURL, topic);
                }
                catch (SocketException)
                {
                    Console.WriteLine("can't connect to broker... waiting and trying again");
                    System.Threading.Thread.Sleep(5000);
                    try
                    {
                        broker.RemoveSubscription(myURL, topic);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("can't connect to broker");
                    }
                }
            }
            else { functions.Add(() => this.RemoveSubscription(topic)); }
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

        public void AddSubscriptionRemote(string topic)
        {
            if (isFreeze == 0)
            {
                try
                {
                    broker.AddSubscription(myURL, topic);
                    form.Invoke(new DelegateAddSubscriptionRemote(form.AddTopic), topic);
                }
                catch (SocketException)
                {
                    Console.WriteLine("can't connect to broker... waiting and trying again");
                    System.Threading.Thread.Sleep(5000);
                    try
                    {
                        broker.AddSubscription(myURL, topic);
                        form.Invoke(new DelegateAddSubscriptionRemote(form.AddTopic), topic);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("can't connect to broker");
                    }
                }
            }
            else { functions.Add(() => this.AddSubscriptionRemote(topic)); }
        }

        public void RemoveSubscriptionRemote(string topic)
        {
            if (isFreeze == 0)
            {
                try
                {
                    broker.RemoveSubscription(myURL, topic);
                    form.Invoke(new DelegateRemoveSubscriptionRemote(form.RemoveTopic), topic);
                }
                catch (SocketException)
                {
                    Console.WriteLine("can't connect to broker... waiting and trying again");
                    System.Threading.Thread.Sleep(5000);
                    try
                    {
                        broker.RemoveSubscription(myURL, topic);
                        form.Invoke(new DelegateRemoveSubscriptionRemote(form.RemoveTopic), topic);
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("can't connect to broker");
                    }
                }
            }
            else { functions.Add(() => this.RemoveSubscriptionRemote(topic)); }
        }

        //gives a status report on the node
        //this includes saying its alive and the current subscriptions
        public void StatusUpdate()
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[Subscriber Status]");
                Console.WriteLine("I'm alive at: " + myURL);
                Console.WriteLine("My subscriptions are:");
                foreach (string topic in form.subscriptions)
                {
                    Console.WriteLine(topic);
                }
            }
            else { functions.Add(() => this.StatusUpdate()); }
        }
    }


}
