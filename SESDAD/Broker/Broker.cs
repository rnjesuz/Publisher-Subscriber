using System;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System.Diagnostics;
using System.Threading;

namespace SESDAD
{
    public class Broker
    {
        private static int myPort;
        internal static string myURL = null;
        internal static string fatherURL = null;
        internal static List<string> childURLs;
        private string processname;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //TODO remove when PuppetMaster is implemented
            // myPort = 8086;
            //TODO remove after PuppetMaster is implemented
            //myURL = "tcp://localhost:"+myPort+"/broker";
            if (args.Length == 3)
            {
                new Broker(args[0], args[1], args[2]);
            }
            else
            {
                new Broker(args[0], args[1]);
            }

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            props["port"] = myPort;
            TcpChannel channel = new TcpChannel(props, null, provider);


            //TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker),"broker",WellKnownObjectMode.Singleton);
             
            BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), myURL);
            rb.ConnectFatherBroker(fatherURL);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

           
        }

        public Broker(string name, string url, string fUrl)
        {
            processname = name;
            myURL = url;
            fatherURL = fUrl;
            myPort = parseURL(url);
        }

        public Broker (string name, string url)
        {
            processname = name;
            myURL = url;
            myPort = parseURL(url);
        }

        public int parseURL(string url)
        {
            string[] parsedURL = url.Split(':');  //parsedURL[0] = "tcp"; parsedURL[1]= "//localhost"; parsedURL[2]= "PORT/broker";
            string[] parsedURLv2 = parsedURL[2].Split('/'); //parsedURLv2[0] = "PORT"; parsedURLv2[1]= "broker";
            myPort = int.Parse(parsedURLv2[0]);
            return myPort;
        }
    }

    class RemoteBroker : MarshalByRefObject, BrokerInterface
    {
        //Dictionary of every publisher connected to this Broker and his topic 
        Dictionary<string, string> publishers = new Dictionary<string, string>();
        //Dictionary of every subscriber connected to this Broker and his subscription
        Dictionary<string, List<string>> subscribers = new Dictionary<string, List<string>>();
        //Father node in the Broker Tree. CAN be NULL. root of the tree
        BrokerInterface fatherBroker;
        //Child node in the Broker Tree. CAN be NULL. last leaf
        List<BrokerInterface> childBroker = new List<BrokerInterface>();
        //Url broker
        private string myURL = Broker.myURL;

        // 0 did NOT porpagate yet; 1 already porpagate
        private int propagate = 0;

        //bool to tell if process is freezed. 0 = NOT FREEZED; 1 = FREEZED
        private int isFreeze = 0;

        //List of functions to call when the process is unfreezed
        private List<Action> functions = new List<Action>();


        //function called by a subscriber wishing to connect to this broker
        public void ConnectSubscriber(string subURL)
        {
            if (isFreeze == 0)
            {
                //add subscriber to the Dictionary. By default the subscription is of every publication ( denoted by root/ )
                List<string> auxlist = new List<string>(); /*auxlist to init list of subscriptions*/
                auxlist.Add("root");
                subscribers.Add(subURL, auxlist);
                System.Console.WriteLine("Subscriber at: " + subURL + " connected");
            }
            else { functions.Add(() => this.ConnectSubscriber(subURL)); }
        }

        public void AddSubscription(string subURL, string subscription)
        {
            /*Verify if subURL is on the List*/
            if (isFreeze == 0)
            {
                if (subscribers.ContainsKey(subURL))
                {
                    /*Verify if already subscribed to topic*/
                    if (!subscribers[subURL].Contains("root/" + subscription))
                    {
                        /*Verify if Subscribers first subscription*/
                        if (subscribers[subURL].Count == 1 && subscribers[subURL].Contains("root"))
                        {
                            /*eliminate root entry, so no errors*/
                            subscribers[subURL].Clear();
                            subscribers[subURL].Add("root/" + subscription);
                            System.Console.WriteLine(subURL + " subscribed to: " + subscription);

                        }
                        else
                        {
                            /*if not the first subscription, do this*/
                            subscribers[subURL].Add("root/" + subscription);
                            System.Console.WriteLine(subURL + " subscribed to: " + subscription);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine(subURL + " already subscribed to " + subscription);
                    }
                }
                else
                {
                    //TODO throw an exception to the subscriber
                    Console.WriteLine("There is no such Subscriber connected to this Broker");
                }
            }
            else { functions.Add(() => this.AddSubscription(subURL, subscription)); }
        }

        public void RemoveSubscription(string subURL, string topic)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[RemoveSubscription]");
                //Verify if subURL is on the List
                if (subscribers.ContainsKey(subURL))
                {
                    //Verify subscriber is subscribed to topic
                    if (subscribers[subURL].Contains("root/" + topic))
                    {
                        //Verify if only sub -> implement defaul root
                        if (subscribers[subURL].Count == 1)
                        {
                            //remove only topic establish default root
                            subscribers[subURL].Clear();
                            subscribers[subURL].Add("root");
                            Console.WriteLine(topic + " removed from " + subURL);
                        }
                        else
                        {
                            //Normal remove of topic
                            subscribers[subURL].Remove("root/" + topic);
                            Console.WriteLine(topic + " removed from " + subURL);
                        }
                    }
                    else
                    {
                        //TODO throw an exception to the subscriber
                        Console.WriteLine("There is no such topic");
                    }
                }
                else
                {
                    //TODO throw an exception to the subscriber
                    Console.WriteLine("There is no such Subscriber connected to this Broker");
                }
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.RemoveSubscription(subURL, topic)); }
        }

        public void ConnectPublisher(string pubURL)
        {
            if (isFreeze == 0)
            {
                //add publisher to the Dictionary. By default the publisher publishes to the general topic ( denoted by root/ )
                publishers.Add(pubURL, "root");
                Console.WriteLine("Publisher at: " + pubURL + " connected");
            }
            else { functions.Add(() => this.ConnectPublisher(pubURL)); }
        }

        //change the topic to which a publisher will write
        public void ChangePublishTopic(string pubURL, string topic)
        {
            if (isFreeze == 0)
            {
                if (publishers.ContainsKey(pubURL))
                {
                    publishers[pubURL] = "root/" + topic;
                    Console.WriteLine(pubURL + " publishing to: " + topic);
                }
                else
                {
                    //TODO trow and exception to the publisher
                    Console.WriteLine("There is no such Publisher connected to this Broker");
                }
            }
            else { functions.Add(() => this.ChangePublishTopic(pubURL, topic)); }
        }

        //conection to father url
        public void ConnectFatherBroker(string url)
        {
            if (url != null)
            {
                fatherBroker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                fatherBroker.AddChild(myURL);
                Console.WriteLine("Added father");
            }
        }

        //add a child broker to the list
        public void AddChild(string url)
        {
            BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
            if (!(childBroker.Contains(bi)) && childBroker != null)
            {
                Console.WriteLine("estou a adicionar o filho");
                childBroker.Add((BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url));
                Console.WriteLine("Added child");
            }
        }

        //method called by a publisher to publish a publication
        //or
        //method called by a child broker to propagate a publication
        public void ReceivePublication(string publication, string pubURL, string topic)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[ReceivePublication]");
                if (propagate == 0)
                {
                    propagate = 1;
                    PropagatePublication(publication, pubURL, topic);
                }

                Console.WriteLine("Calling SendPublication");
                SendPublication(publication, pubURL, topic);
                Console.WriteLine("[End of ReceivePublication]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.ReceivePublication(publication, pubURL, topic)); }
        }

        //method used to propagate the publication up the Broker Tree.
        //Each Broker node sends it to his father until it reaches the root
        public void PropagatePublication(string publication, string pubURL, string topic)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[PropagatePublication]");
                //check if Broker is tree root
                if (fatherBroker != null)
                {
                    Console.WriteLine("Propagating to father");
                    fatherBroker.ReceivePublication(publication, pubURL, topic);
                    Console.WriteLine("Propagated");
                    Console.WriteLine("Started call for Log Update");
                    PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                    PM.UpdateEventLog("BroEvent", myURL, pubURL, topic);
                    Console.WriteLine("Ended call for Log Update");
                }

                if (childBroker != null)
                {
                    foreach (BrokerInterface child in childBroker)
                    {
                        Console.WriteLine("Propagating to child(s)");
                        child.ReceivePublication(publication, pubURL, topic);
                        Console.WriteLine("Propagated");
                        Console.WriteLine("Started call for Log Update");
                        PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                        PM.UpdateEventLog("BroEvent", myURL, pubURL, topic);
                        Console.WriteLine("Finsihed call for Log Update");
                    }
                }
                Console.WriteLine("[End of PropagatePublication]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.PropagatePublication(publication, pubURL, topic)); }
        }

        //method used to send the publication to one or several subscribers of the broker
        //checks if any subscriber is intereted in the topic, and sends it to them if yes
        public void SendPublication(string publication, string pubURL, string publicationTopic)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[SenPublication]");
                Console.WriteLine("Sending publication to Subscribers");
                //See if any subscriber is interested in this publication
                foreach (String subscriber in subscribers.Keys)
                {
                    foreach (String topic in subscribers[subscriber])
                    {
                        if (publicationTopic.Contains(topic))
                        {
                            SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subscriber);
                            newSubscriber.ReceivePublication(publication, pubURL, publicationTopic);
                        }
                    }
                }
                propagate = 0;
                Console.WriteLine("Finished sending publication");
                Console.WriteLine("[End of SendPublication]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.SendPublication(publication, pubURL, publicationTopic)); }
        }

        public void Kill()
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[Kill]");
                Console.WriteLine("killing... RIP");
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

        //calls for a status report on every node linked to himself. brokers subs and publishers
        public void StatusUpdate()
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[Status Broker]");
                Console.WriteLine("I'm alive at: " + myURL);
                Console.WriteLine("Presumed alive subscribers are: ");

                foreach (string sub in subscribers.Keys)
                {
                    Console.WriteLine(sub);
                }
                //call for status report of the subs
                foreach (string sub in subscribers.Keys)
                {
                    SubscriberInterface Subscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), sub);
                    Subscriber.StatusUpdate();
                }
                Console.WriteLine("Presumed alive publishers are: ");
                foreach (string pub in publishers.Keys)
                {
                    Console.WriteLine(pub);
                }
                //call for status report of the pubs
                foreach (string pub in publishers.Keys)
                {
                    PublisherInterface Publisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), pub);
                    Publisher.StatusUpdate();
                }

                //call for status report on child nodes
                //call starts on the root of the Broker-Tree and propagates downwards
                if (childBroker != null)
                {
                    Console.WriteLine("I have " + childBroker.Count + " child Nodes");
                    foreach (BrokerInterface bi in childBroker)
                    {
                        bi.StatusUpdate();
                    }
                }
                Console.WriteLine("[End of Status Report]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.StatusUpdate() ); }
        }
    }
}
