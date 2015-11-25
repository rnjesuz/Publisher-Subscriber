using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

namespace SESDAD
{
    public class Broker
    {
        private static int myPort;
        internal static string myURL = null;
        internal static string fatherURL = null;
        internal static string processname;
        //boll to tell if systme is in mode filtering(1) or flooding(0). Used by remoteBroker
        internal static int isFiltering = 0;
        internal static int brokerType = 0;
        internal static string replicaURL;

        static void Main(string[] args)
        {
            //TODO remove when PuppetMaster is implemented
            // myPort = 8086;
            //TODO remove after PuppetMaster is implemented
            //myURL = "tcp://localhost:"+myPort+"/broker";
            if (args.Length == 5)
            {
                brokerType = Int32.Parse(args[4]);
                new Broker(args[0], args[1], args[2], Int32.Parse(args[3]), Int32.Parse(args[4]) );
            }
            else
            {
                brokerType = Int32.Parse(args[3]);
                new Broker(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]) );
            }

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            if (brokerType == 1)
            {
                int newPort = myPort + 1337;
                props["port"] = newPort;
            }
            else if(brokerType == 2)
            {
                int newPort = myPort + 1338;
                props["port"] = newPort;
            }
            else
            {
                props["port"] = myPort;
            }

            TcpChannel channel = new TcpChannel(props, null, provider);

            //TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            if(brokerType == 0)
            {
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker), "broker", WellKnownObjectMode.Singleton);
                BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), myURL);
                rb.ConnectFatherBroker(fatherURL);
            }
            else
            {
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker),  processname, WellKnownObjectMode.Singleton);
                Console.WriteLine(replicaURL);
                BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), replicaURL);
                rb.StartPing();
            }
                    
            //solely to prevent console from closing
            while (true) { }
        }

        //constructor for the root broker ( no father)
        public Broker(string name, string url, string fUrl, int filtering, int _brokerType)
        {
            processname = name;
            myURL = url;
            fatherURL = fUrl;
            myPort = parseURL(url);
            isFiltering = filtering;
            brokerType = _brokerType;
            if(brokerType != 0)
            {
                replicaURL = transformURL(url);
            }
            Console.WriteLine("processname: " + processname);
        }

        //constructor for leafe broker (has a father)
        public Broker(string name, string url, int filtering, int _brokerType)
        {
            processname = name;
            myURL = url;
            myPort = parseURL(url);
            isFiltering = filtering;
            brokerType = _brokerType;
            if (brokerType != 0)
            {
                replicaURL = transformURL(url);
            }
            Console.WriteLine("processname: " + processname);
        }

        public int parseURL(string url)
        {
            string[] parsedURL = url.Split(':');  //parsedURL[0] = "tcp"; parsedURL[1]= "//localhost"; parsedURL[2]= "PORT/broker";
            string[] parsedURLv2 = parsedURL[2].Split('/'); //parsedURLv2[0] = "PORT"; parsedURLv2[1]= "broker";
            myPort = int.Parse(parsedURLv2[0]);
            return myPort;
        }

        //method used by broker replics to tranform their url form the leader's
        //e.g tcp://localhost:3337/broker becomes tcp://localhost:4674/broker0-1
        public string transformURL(string url)
        {
            string[] parsedURL = url.Split(':');  //parsedURL[0] = "tcp"; parsedURL[1]= "//localhost"; parsedURL[2]= "PORT/broker";
            string[] parsedURLv2 = parsedURL[2].Split('/'); //parsedURLv2[0] = "PORT"; parsedURLv2[1]= "broker";

            //since 2 processes can't sahre same port then replicas add 1337 or 1338 to port number
            if(brokerType == 1)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 1337).ToString();
            if(brokerType == 2)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 1338).ToString();

            //if leader is broker0 then replica becomes broker0-1 or broker0-2
            parsedURLv2[1] = processname;
            //rejoin modified parsels into the new URL
            string newURLv2 = string.Join("/", parsedURLv2);
            parsedURL[2] = newURLv2;
            string newURL = string.Join(":", parsedURL);
            Console.WriteLine(newURL);
            return newURL;
        }
    }

    class RemoteBroker : MarshalByRefObject, BrokerInterface
    {
        //Dictionary of every publisher connected to this Broker and his topic 
        Dictionary<string, string> publishers = new Dictionary<string, string>();
        //Dictionary of every subscriber connected to this Broker and his subscription
        Dictionary<string, List<string>> subscribers = new Dictionary<string, List<string>>();

        //Father node in the Broker Tree. CAN be NULL. root of the tree
        //BrokerInterface fatherBroker;
        //Child node in the Broker Tree. CAN be NULL. last leaf
        //List<BrokerInterface> childBroker = new List<BrokerInterface>();

        string fatherBroker;
        List<string> childBroker = new List<string>();

        //List of father's interested subscription. NULL unless routing is mode filtering
        List<string> fatherSubscriptions = new List<string>();
        //Dictionary between each child and they're interested subscriptions. NULL unless routing is mode filtering
        Dictionary<string, List<string>> childsSubscriptions = new Dictionary<string, List<string>>();

        //Url broker
        private string myURL = Broker.myURL;

        // 0 did NOT porpagate yet; 1 already porpagate
        //private int propagate = 0;

        //bool to tell if process is freezed. 0 = NOT FREEZED; 1 = FREEZED
        private int isFreeze = 0;

        //boll to tell if systme is in mode filtering(1) or flooding(0) 
        private int isFiltering = Broker.isFiltering;

        //List of functions to call when the process is unfreezed
        private List<Action> functions = new List<Action>();

        //am i the leader or a replication? (0 for lider, 1 for replication)
        private int brokerType = Broker.brokerType;

        //constructor for remoteobject iniatialization
        public void StartPing()
        {
            //check if replica
            //if replica: keep trying to connect to leader ( active replication)
            //at first fail try and become leader.  if promotion fails check if another replica was faster: true - stay in replica cycle, false - keep trying to become leader
            //replica does ping, leader DOESN'T reply pong.

            //sleep to make sure leader had time to be initialized before pinging him
            System.Threading.Thread.Sleep(10000);
            while (true)
            {
                try
                {
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), myURL);
                    bi.ReceivePing();
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Deu bosta");

                }
            }
           
        }
        //function called by a subscriber wishing to connect to this broker
        public void ConnectSubscriber(string subURL)
        {
            if (isFreeze == 0)
            {
                //add subscriber to the Dictionary.
                List<string> auxlist = new List<string>(); /*auxlist to init list of subscriptions*/
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
                    if (!subscribers[subURL].Contains(subscription))
                    {
                        subscribers[subURL].Add(subscription);
                        System.Console.WriteLine(subURL + " subscribed to: " + subscription);
                    }
                    else
                    {
                        System.Console.WriteLine(subURL + " already subscribed to " + subscription);
                    }

                    //sends subscription to father and child if flag is active
                    if (isFiltering == 1)
                    {
                        if (fatherBroker != null)
                        {
                            BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                            fatherBI.NewSubscriptionForFather(myURL, subscription);
                        }

                        foreach (string child in childBroker)
                        {
                            BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                            childBI.NewSubscriptionForChild(subscription);
                        }
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
                    if (subscribers[subURL].Contains(topic))
                    {
                        //Normal remove of topic
                        subscribers[subURL].Remove(topic);
                        Console.WriteLine(topic + " removed from " + subURL);
                    }
                    else
                    {
                        //TODO throw an exception to the subscriber
                        Console.WriteLine("There is no such topic");
                    }

                    if (isFiltering == 1)
                    {
                        if (fatherBroker != null)
                        {
                            BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                            fatherBI.RemoveSubscriptionForFather(myURL, topic);
                        }
                        foreach (string child in childBroker)
                        {
                            BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                            childBI.RemoveSubscriptionForChild(topic);
                        }
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
                //add publisher to the Dictionary.
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
                    publishers[pubURL] = topic;
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
                fatherBroker = url;
                BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                fatherBI.AddChild(myURL);
                Console.WriteLine("Added father");
            }
        }

        //add a child broker to the list
        public void AddChild(string url)
        {
            //adds child to list of childs
            //????????BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
            if (!(childBroker.Contains(url)) && childBroker != null)
            {
                //?????????childBroker.Add((BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url));
                childBroker.Add(url);
                Console.WriteLine("Added child");
            }
            //creates and entrance in the hashmap for this child and its future subscriptions.
            if (!(childsSubscriptions.ContainsKey(url)))
            {
                childsSubscriptions.Add(url, new List<string>());
            }
        }

        //method called by a publisher to publish a publication
        //or
        //method called by a child broker to propagate a publication
        public void ReceivePublication(string publication, string pubURL, string topic, string propagatorURL)
        {
            if (isFreeze == 0)
            {

                Console.WriteLine("Started call for Log Update");
                PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                PM.UpdateEventLog("BroEvent", myURL, pubURL, topic);
                Console.WriteLine("Ended call for Log Update");

                Console.WriteLine("[ReceivePublication]");
                //if (propagate == 0)
                //{
                //propagate = 1;
                PropagatePublication(publication, pubURL, topic, propagatorURL);
                //}

                Console.WriteLine("Calling SendPublication");
                SendPublication(publication, pubURL, topic);
                Console.WriteLine("[End of ReceivePublication]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.ReceivePublication(publication, pubURL, topic, propagatorURL)); }
        }

        //method used to propagate the publication up the Broker Tree.
        //Each Broker node sends it to his father until it reaches the root
        public void PropagatePublication(string publication, string pubURL, string topic, string propagatorURL)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[PropagatePublication]");
                //mode flooding
                if (isFiltering == 0)
                {
                    //check if Broker is tree root
                    if ((fatherBroker != null) && (!fatherBroker.Equals(propagatorURL)))
                    {
                        Console.WriteLine("Propagating to father");
                        BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                        fatherBI.ReceivePublication(publication, pubURL, topic, myURL);
                        Console.WriteLine("Propagated");
                    }

                    if (childBroker != null)
                    {
                        foreach (string child in childBroker)
                        {
                            if (!child.Equals(propagatorURL))
                            {
                                Console.WriteLine("Propagating to child(s)");
                                BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                childBI.ReceivePublication(publication, pubURL, topic, myURL);
                                Console.WriteLine("Propagated");
                            }
                        }
                    }
                }
                else
                {
                    //mode filtering 
                    //check if father is interested
                    foreach (string subscription in fatherSubscriptions)
                    {
                        if (!fatherBroker.Equals(propagatorURL))
                        {
                            Console.WriteLine("sub: " + subscription);
                            if (subscription.Equals(topic))
                            {
                                Console.WriteLine("Propagating to father");
                                BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                                fatherBI.ReceivePublication(publication, pubURL, topic, myURL);
                                Console.WriteLine("Propagated");
                            }
                        }
                    }

                    //check if any child is intereted
                    foreach (string child in childsSubscriptions.Keys)
                    {
                        if (child != propagatorURL)
                        {
                            foreach (string subscription in childsSubscriptions[child])
                            {
                                if (subscription.Equals(topic))
                                {
                                    Console.WriteLine("Propagating to child(s)");
                                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                    bi.ReceivePublication(publication, pubURL, topic, myURL);
                                    Console.WriteLine("Propagated");
                                }
                            }
                        }
                    }
                }               
                Console.WriteLine("[End of PropagatePublication]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.PropagatePublication(publication, pubURL, topic, myURL)); }
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
                //propagate = 0;
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
                Environment.Exit(0);
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
                    foreach (string child in childBroker)
                    {
                        BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                        childBI.StatusUpdate();
                    }
                }
                Console.WriteLine("[End of Status Report]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.StatusUpdate()); }
        }

        public void NewSubscriptionForFather(string childURL, string subscription)
        {
            Console.WriteLine("One of my childs has a new subscription");
            childsSubscriptions[childURL].Add(subscription);

            //test is there's a father to send to
            if (fatherBroker != null)
            {
                BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                fatherBI.NewSubscriptionForFather(myURL, subscription);
            }

            //check if i have more than 1 child. if i do i propagate to them i have a new subsciption
            foreach (string child in childBroker)
            {
                if (!child.Equals(childURL))
                {
                    BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                    childBI.NewSubscriptionForChild(subscription);
                }
            }
        }

        public void NewSubscriptionForChild(string subscription)
        {
            Console.WriteLine("My father has a new subscription");
            fatherSubscriptions.Add(subscription);

            //send interests to my own childs.
            //no need to test for null the foreach doe sit for us
            foreach (string child in childBroker)
            {
                BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                childBI.NewSubscriptionForChild(subscription);
            }
        }

        public void RemoveSubscriptionForFather(string childURL, string topic)
        {
            Console.WriteLine("One of my childs removed a subscription");
            childsSubscriptions[childURL].Remove(topic);

            //test if there's a father to send to
            if (fatherBroker != null)
            {
                BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                fatherBI.RemoveSubscriptionForFather(myURL, topic);
            }

            //check if i have more than 1 child. if i do i propagate to them i no longer have insterest in this subscription
            foreach (string child in childBroker)
            {
                if (!child.Equals(childURL))
                {
                    BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                    childBI.NewSubscriptionForChild(topic);
                }
            }
        }

        public void RemoveSubscriptionForChild(string topic)
        {
            Console.WriteLine("My father removed a subscription");
            fatherSubscriptions.Remove(topic);

            //remove interests from child
            //no need to test for null the foreach does it for us
            foreach (string child in childBroker)
            {
                BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                childBI.RemoveSubscriptionForChild(topic);
            }
        }

        public void ReceivePing()
        {
            
        }
    }
}
