﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Threading;

namespace SESDAD
{

    public class Broker
    {
        internal static int myPort;
        internal static string myURL = null;
        internal static string fatherURL = null;
        internal static string processname;
        //bool to tell if systme is in mode filtering(1) or flooding(0). Used by remoteBroker
        internal static int isFiltering = 0;
        //bool to tell what mode of ordering is the system in. -1 is for No order. 0 is for FIFO order, 1 is for TOTAL order
        internal static int order = 0;
        internal static int brokerType = 0;
        internal static string replicaURL;
        internal static TcpChannel channel;

        internal static RemoteBroker rm;

        static void Main(string[] args)
        {
            //TODO remove when PuppetMaster is implemented
            // myPort = 8086;
            //TODO remove after PuppetMaster is implemented
            //myURL = "tcp://localhost:"+myPort+"/broker";
            if (args.Length == 6)
            {
                brokerType = Int32.Parse(args[4]);
                new Broker(args[0], args[1], args[2], Int32.Parse(args[3]), Int32.Parse(args[4]), Int32.Parse(args[5]));
            }
            else
            {
                brokerType = Int32.Parse(args[3]);
                new Broker(args[0], args[1], Int32.Parse(args[2]), Int32.Parse(args[3]), Int32.Parse(args[4]));
            }

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            if (brokerType == 1)
            {
                int newPort = myPort + 20 + (myPort % 100);
                props["port"] = newPort;
            }
            else if (brokerType == 2)
            {
                int newPort = myPort + 21 + (myPort % 100);
                props["port"] = newPort;
            }
            else
            {
                props["port"] = myPort;
            }

            channel = new TcpChannel(props, null, provider);

            //TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            rm = new RemoteBroker();

            if (brokerType == 0)
            {
                RemotingServices.Marshal(rm, "broker", typeof(RemoteBroker));
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker), "broker", WellKnownObjectMode.Singleton);
                BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), myURL);
                rb.ConnectFatherBroker(fatherURL);
            }
            else
            {
                RemotingServices.Marshal(rm, "broker", typeof(RemoteBroker));
                //RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker),  "broker", WellKnownObjectMode.Singleton);
                BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), replicaURL);
                rb.StartPing();
            }

            //solely to prevent console from closing
            while (true) { }
        }

        //constructor for the root broker ( no father)
        public Broker(string name, string url, string fUrl, int filtering, int _brokerType, int _order)
        {
            processname = name;
            myURL = url;
            fatherURL = fUrl;
            myPort = parseURL(url);
            isFiltering = filtering;
            brokerType = _brokerType;
            if (brokerType != 0)
            {
                replicaURL = transformURL(url);
            }
            order = _order;
            Console.WriteLine("processname: " + processname);
        }

        //constructor for leafe broker (has a father)
        public Broker(string name, string url, int filtering, int _brokerType, int _order)
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
            order = _order;
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

            //since 2 processes can't sahre same port then replicas add 20 or 21 to port number
            //to prevent collision with other replicas we also add the last number from this specific replica
            if (brokerType == 1)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 20 + (int.Parse(parsedURLv2[0]) % 100)).ToString();
            if (brokerType == 2)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 21 + (int.Parse(parsedURLv2[0]) % 100)).ToString();

            //rejoin modified parsels into the new URL
            string newURLv2 = string.Join("/", parsedURLv2);
            parsedURL[2] = newURLv2;
            string newURL = string.Join(":", parsedURL);
            Console.WriteLine(newURL);
            return newURL;
        }
        public void ConnectReplica()
        {

        }
    }


    [Serializable]
    class RemoteBroker : MarshalByRefObject, BrokerInterface
    {

        //Url broker
        //private string myURL = Broker.myURL;
        // 0 did NOT porpagate yet; 1 already porpagate
        //private int propagate = 0;
        //bool to tell if process is freezed. 0 = NOT FREEZED; 1 = FREEZED
        private int isFreeze = 0;
        private int order = Broker.order;
        //boll to tell if systme is in mode filtering(1) or flooding(0) 
        //private int isFiltering = Broker.isFiltering;
        //List of functions to call when the process is unfreezed
        private List<Action> functions = new List<Action>();
        //list of publications ahead of time. the pairing is from publicationnbmr to the action it will execute when the time comes 
        private Dictionary<string, Dictionary<int, Action>> waitingPublications = new Dictionary<string, Dictionary<int, Action>>();
        //for each broker connected to this one. check the last received publication received by each publisher
        private Dictionary<string, Dictionary<string, int>> NeighbourBrokerLastPub = new Dictionary<string, Dictionary<string, int>>();
        //mutex to acess and modify last pub received by a broker
        private Mutex lastPubForBrokerMut = new Mutex();
        //mutex to control access to list of last publications
        private Mutex lastPubMut = new Mutex();
        //mutex to control acess to list of last publications during TOTAL order
        private Mutex lastPubTOTALMut = new Mutex();
        //int to save the last received publication during TOTAL order
        int lastPublicationTOTAL = -1;
        //List of waiting publication that arrived ahead of time during TOTAL order; ( mapping is done by ticketnumber -> publication method)
        private Dictionary<int, Action> waitingPublicationsTOTAL = new Dictionary<int, Action>();
        //boolean to test if there were changes to the waitingpublications list
        bool waitingListChange = false;
        //Dictionary associating a Publisher to the last received publication( last received is indicated by a integer)
        Dictionary<string, int> lastPublication = new Dictionary<string, int>();
        //am i the leader or a replication? (0 for lider, 1 for replication)
        //private int brokerType = Broker.brokerType;
        //Monitor for replicas when attempnting to takeover leader
        //object replicaMonitor = new object();

        //Dictionary of every publisher connected to this Broker and his topic 
        Dictionary<string, string> publishers = new Dictionary<string, string>();
        //Dictionary of every subscriber connected to this Broker and his subscription
        Dictionary<string, List<string>> subscribers = new Dictionary<string, List<string>>();
        //Father node in the Broker Tree. CAN be NULL. root of the tree
        string fatherBroker;
        //Child node in the Broker Tree. CAN be NULL. last leaf
        List<string> childBroker = new List<string>();
        //List of father's interested subscription. NULL unless routing is mode filtering
        List<string> fatherSubscriptions = new List<string>();
        //Dictionary between each child and they're interested subscriptions. NULL unless routing is mode filtering
        Dictionary<string, List<string>> childsSubscriptions = new Dictionary<string, List<string>>();

        //Booleans that represent the state of the replicas. when one overtakes the leader bool is updated
        bool aliveReplica1 = true;
        bool aliveReplica2 = true;



        //constructor for remoteobject iniatialization
        public RemoteBroker()
        {
            Console.WriteLine("Starting RemoteBroker");
        }

        //auxiliary method that preform the overtake of the replica
        //first it unregisters itself and tries to become the leader creating a new channel with leader properties
        //if failure because other replica was faster then it return to passive mode
        //TODO if failure because leader has failed but is not yet properly dead ( channel lingers cause garbage colector is slow)  then it keep retrying untill sucess
        //@return return a int for sucess/failure of takeover. 0 is sucess,-1 means failure.
        private int overtakeLeader()
        {
            try
            {
                Console.WriteLine("Unregestering old channel");
                ChannelServices.UnregisterChannel(Broker.channel);
                char replicanmbr = Broker.processname[Broker.processname.Length - 1];
                Console.WriteLine("Doing channel properties");
                BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
                provider.TypeFilterLevel = TypeFilterLevel.Full;
                IDictionary props = new Hashtable();
                props["port"] = Broker.myPort;
                TcpChannel newchannel = new TcpChannel(props, null, provider);
                Console.WriteLine("Registeting new channel");
                ChannelServices.RegisterChannel(newchannel, false);

                BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), Broker.myURL);
                rb.ConnectFatherBroker(Broker.fatherURL);
                Console.WriteLine("I '" + Broker.processname + "' took over and am now performing as Leader");

                //now we change the bool related to the replica that took over
                //the replica that took over is no longer available on the old channel so leader shouldnt try to reach it
                if (replicanmbr.Equals('1'))
                {
                    aliveReplica1 = false;
                    string brkReplica = transformURL(Broker.processname, Broker.myURL, 2);
                    //see if other replica is alive before starting conversaiton
                    if (aliveReplica2)
                    {
                        //TODO try catch in case broker dies by itself without system intervention
                        BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica);
                        //Notify other replica that im now the leader and former channel no longer exists
                        bi.ActualizeLeader(replicanmbr);
                        Console.WriteLine("Replica2 notified of takeover by Replica1");
                    }
                }
                if (replicanmbr.Equals('2'))
                {
                    aliveReplica2 = false;
                    //get other replica URL
                    string brkReplica = transformURL(Broker.processname, Broker.myURL, 1);
                    //see if other replica is alive before starting conversaiton
                    if (aliveReplica1)
                    {
                        //TODO try catch in case broker dies by itself without system intervention
                        BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica);
                        //Notify other replica that im now the leader and former channel no longer exists
                        bi.ActualizeLeader(replicanmbr);
                        Console.WriteLine("Replica1 notified of takeover by Replica2");
                    }
                }
                return 0;
            }
            //remoting exception - The channel has already been registered.
            catch (RemotingException)
            {
                Console.WriteLine("Channel in use");
                Console.WriteLine("Take over failed, returning to passive mode");
                //sleep to make sure channel is unregistered before registering again
                System.Threading.Thread.Sleep(5000);
                ChannelServices.RegisterChannel(Broker.channel, false);
                return -1;
            }
            //SocketException - garbage colector hans't yet cleared the Leader 
            catch (SocketException)
            {
                Console.WriteLine("Socket in use");
                Console.WriteLine("Take over failed, returning to passive mode");
                //sleep to make sure channel is unregistered before registering again
                System.Threading.Thread.Sleep(5000);
                ChannelServices.RegisterChannel(Broker.channel, false);
                return -1;
            }
        }
        public void StartPing()
        {
            //sleep to make sure leader had time to be initialized before pinging him
            System.Threading.Thread.Sleep(10000);
            while (true)
            {
                try
                {
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), Broker.myURL);
                    bi.ReceivePing();
                    //Sleep so we don't overuse the system doing ping pong messages
                    System.Threading.Thread.Sleep(5000);
                }
                //TODO catch right exception
                //unknowservice??
                catch (Exception)
                {
                    Console.WriteLine("Main broker died, attempting to take over...");
                    int sucess = 0;
                    sucess = overtakeLeader();
                    if (sucess == 0)
                        break;
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
                //now, propagate to replicas
                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                if (aliveReplica1)
                {
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                    bi.ConnectSubscriberReplica(subURL);
                }
                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                if (aliveReplica2)
                {
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                    bi2.ConnectSubscriberReplica(subURL);
                }
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
                    if (Broker.isFiltering == 1)
                    {
                        if (fatherBroker != null)
                        {
                            BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                            fatherBI.NewSubscriptionForFather(Broker.myURL, subscription);
                        }

                        foreach (string child in childBroker)
                        {
                            BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                            childBI.NewSubscriptionForChild(subscription);
                        }
                    }

                    //now, propagate to replicas
                    string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                    if (aliveReplica1)
                    {
                        //TODO try catch in case broker dies by itself without system intervention
                        BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                        bi.AddSubscriptionReplica(subURL, subscription);
                    }
                    string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                    if (aliveReplica2)
                    {
                        //TODO try catch in case broker dies by itself without system intervention
                        BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                        bi2.AddSubscriptionReplica(subURL, subscription);
                    }
                }
                else
                {
                    //TODO throw an exception to the subscriber
                    Console.WriteLine("There is no such Subscriber connected to this Broker");
                    Console.WriteLine(subURL);
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

                    if (Broker.isFiltering == 1)
                    {
                        if (fatherBroker != null)
                        {
                            BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                            fatherBI.RemoveSubscriptionForFather(Broker.myURL, topic);
                        }
                        foreach (string child in childBroker)
                        {
                            BrokerInterface childBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                            childBI.RemoveSubscriptionForChild(topic);
                        }
                    }

                    //now, propagate to replicas
                    string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                    if (aliveReplica1)
                    {
                        //TODO try catch in case broker dies by itself without system intervention
                        BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                        bi.RemoveSubscriptionReplica(subURL, topic);
                    }
                    string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                    if (aliveReplica2)
                    {
                        //TODO try catch in case broker dies by itself without system intervention
                        BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                        bi2.RemoveSubscriptionReplica(subURL, topic);
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
                //now, propagate to replicas
                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                if (aliveReplica1)
                {
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                    bi.ConnectPublisherReplica(pubURL);
                }
                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                if (aliveReplica2)
                {
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                    bi2.ConnectPublisherReplica(pubURL);
                }
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

                //now, propagate to replicas
                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                if (aliveReplica1)
                {
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                    bi.ChangePublishingTopicReplica(pubURL, topic);
                }
                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                if (aliveReplica2)
                {
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                    bi2.ChangePublishingTopicReplica(pubURL, topic);
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
                Console.WriteLine("my father is: " + url);
                BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                Console.WriteLine("calling my child for: " + Broker.myURL);
                fatherBI.AddChild(Broker.myURL);
                //make space in dictionary used for fifo ordering, for this broker ( the father)
                if (!(NeighbourBrokerLastPub.ContainsKey(url)))
                {
                    NeighbourBrokerLastPub.Add(url, new Dictionary<string, int>());
                }
                Console.WriteLine("Added father");
            }
        }

        //add a child broker to the list
        public void AddChild(string url)
        {
            //adds child to list of childs
            if (!(childBroker.Contains(url)) && childBroker != null)
            {
                childBroker.Add(url);
                Console.WriteLine("Added child");
            }

            //creates and entrance in the hashmap for this child and its future subscriptions.
            //used for filtering routing
            if (!(childsSubscriptions.ContainsKey(url)))
            {
                childsSubscriptions.Add(url, new List<string>());
            }

            //make space in dictionary for this broker ( one of te childs )
            //used for fifo ordering
            if (!(NeighbourBrokerLastPub.ContainsKey(url)))
            {
                NeighbourBrokerLastPub.Add(url, new Dictionary<string, int>());
            }

            //and propagate to replicas
            if (aliveReplica1)
            {
                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                //TODO try catch in case broker dies by itself without system intervention
                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                bi.AddChildReplica(url);
            }
            if (aliveReplica2)
            {
                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                //TODO try catch in case broker dies by itself without system intervention
                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                bi2.AddChildReplica(url);
            }
        }

        //method to test if a waiting publication is the next one in line.
        //if it's time for it to be exectued do so,
        //if not keep it in the list
        private void TestPublicationTOTAL(string publication, string pubURL, string topic, string propagatorURL, int ticket)
        {
            //is my number the next one in line?            
            lastPubTOTALMut.WaitOne();
            if (ticket <= lastPublicationTOTAL + 1)
            {
                //signal there was a modification on the waiting list
                waitingListChange = true;
                //remove myself from the waiting list
                waitingPublicationsTOTAL.Remove(ticket);
                lastPubTOTALMut.ReleaseMutex();

                //update replicas                           
                if (aliveReplica1)
                {
                    string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                    bi.RemoveWaitingPubTOTALReplica(ticket);
                }
                if (aliveReplica2)
                {
                    string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                    bi2.RemoveWaitingPubTOTALReplica(ticket);
                }

                //call receiveiPublication
                Console.WriteLine("Publication has reached it's turn");
                ReceivePublicationTOTAL(publication, pubURL, topic, propagatorURL, ticket);
            }
            else
                waitingListChange = false;
        }

        public void ReceivePublicationTOTAL(string publication, string pubURL, string topic, string propagatorURL, int ticket)
        {
            if (isFreeze == 0)
            {
                //double check if were in total ordering
                if (order == 1)
                {
                    //mode flooding
                    if (Broker.isFiltering == 0)
                    {
                        lastPubTOTALMut.WaitOne();
                        //see if publication is the next one in line ( or a previous one delayed by network)
                        if (ticket <= lastPublicationTOTAL + 1)
                        {
                            //do regular publication reception
                            Console.WriteLine("[ReceivePublicationTOTAL]");
                            //added last received publication
                            Console.WriteLine("Received publication " + ticket + " from " + pubURL);
                            lastPublicationTOTAL = ticket;
                            lastPubTOTALMut.ReleaseMutex();

                            //update replicas                            
                            if (aliveReplica1)
                            {
                                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                bi.LastPublicationTOTALReplica(ticket);
                            }
                            if (aliveReplica2)
                            {
                                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                bi2.LastPublicationTOTALReplica(ticket);
                            }

                            //perform regular propagation
                            Console.WriteLine("Started call for Log Update");
                            PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                            PM.UpdateEventLog("BroEvent", Broker.myURL, pubURL, topic);
                            Console.WriteLine("Ended call for Log Update");

                            Console.WriteLine("Propagating...");
                            PropagatePublication(publication, pubURL, topic, propagatorURL, ticket);

                            Console.WriteLine("Sending Publication");
                            SendPublication(publication, pubURL, topic);

                            Console.WriteLine("[End of ReceivePublicationTOTAL]");
                            Console.WriteLine("-------------------------------");
                            //Test all remaining publications that could be waiting for this one
                            //if theres was a publication waiting that got executed, keep testing the list until no more publications can be executed      
                            do
                            {
                                foreach (var function in waitingPublicationsTOTAL.Values)
                                {
                                    Console.WriteLine("invoking a waiting publication");
                                    function.Invoke();
                                }
                            } while (waitingListChange);
                        }
                        //if it's not the next one, then we wait for the correct one to arrive
                        else
                        {
                            //TODO make sure they are ordered properly or something
                            Console.WriteLine("Wasn't my turn, waiting... Ticket:" + ticket + " from pub:" + pubURL);
                            waitingPublicationsTOTAL.Add(ticket, () => this.TestPublicationTOTAL(publication, pubURL, topic, propagatorURL, ticket));
                            lastPubTOTALMut.ReleaseMutex();

                            //update replicas
                            if (aliveReplica1)
                            {
                                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                bi.AddWaitingPubTOTALReplica(ticket, () => this.TestPublicationTOTAL(publication, pubURL, topic, propagatorURL, ticket));
                            }
                            if (aliveReplica2)
                            {
                                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                bi2.AddWaitingPubTOTALReplica(ticket, () => this.TestPublicationTOTAL(publication, pubURL, topic, propagatorURL, ticket));
                            }
                        }
                    }
                    //mode filtering
                    else
                    {
                        Console.WriteLine("[ReceivePublicationTOTAL]");
                        BrokerTicket.Lock();
                        int interested = 0;
                        //check how many adjacent nodes are interested
                        //check father then childs
                        foreach (string subscription in fatherSubscriptions)
                        {
                            if (!fatherBroker.Equals(propagatorURL))
                            {
                                Console.WriteLine("sub: " + subscription);
                                if (subscription.Equals(topic))
                                {
                                    interested++;
                                }
                            }
                        }
                        foreach (string child in childsSubscriptions.Keys)
                        {
                            if (child != propagatorURL)
                            {
                                foreach (string subscription in childsSubscriptions[child])
                                {
                                    if (subscription.Equals(topic))
                                    {
                                        interested++;
                                    }
                                }
                            }
                        }
                        //now i have interested nodes
                        //updated variable that controls system propagation and it's ending
                        BrokerTicketInterface bt = (BrokerTicketInterface)Activator.GetObject(typeof(BrokerTicketInterface), "tcp://localhost:9999/brokerticket");
                        bt.UpdateInterested(interested);

                        //perform regular propagation
                        Console.WriteLine("Started call for Log Update");
                        PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                        PM.UpdateEventLog("BroEvent", Broker.myURL, pubURL, topic);
                        Console.WriteLine("Ended call for Log Update");

                        Console.WriteLine("Propagating...");
                        PropagatePublication(publication, pubURL, topic, propagatorURL, ticket);

                        Console.WriteLine("Sending Publication");
                        SendPublication(publication, pubURL, topic);

                        Console.WriteLine("[End of ReceivePublicationTOTAL]");
                        Console.WriteLine("-------------------------------");
                    }
                }
            }
            else { functions.Add(() => this.ReceivePublicationTOTAL(publication, pubURL, topic, propagatorURL, ticket)); }
        }

        //method to test if a waiting publication is the next one in line.
        //if it's time for it to be exectued do so,
        //if not keep it in the list
        private void TestPublication(string publication, string pubURL, string topic, string propagatorURL, int publicationNmbr)
        {

            //is my number the next one in line?            
            lastPubMut.WaitOne();
            if (publicationNmbr <= lastPublication[pubURL] + 1)
            {
                //signal there was a modification on the waiting list
                waitingListChange = true;
                //remove myself from the waiting list
                waitingPublications[pubURL].Remove(publicationNmbr);
                lastPubMut.ReleaseMutex();

                //update replicas                           
                if (aliveReplica1)
                {
                    string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                    bi.RemoveWaitingPubReplica(pubURL, publicationNmbr);
                }
                if (aliveReplica2)
                {
                    string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                    //TODO try catch in case broker dies by itself without system intervention
                    BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                    bi2.RemoveWaitingPubReplica(pubURL, publicationNmbr);
                }

                //call receiveiPublication
                Console.WriteLine("Publication has reached it's turn");
                ReceivePublication(publication, pubURL, topic, propagatorURL, publicationNmbr);
            }
            else
                waitingListChange = false;
        }

        //method called by a publisher to publish a publication
        //or
        //method called by a child broker to propagate a publication
        public void ReceivePublication(string publication, string pubURL, string topic, string propagatorURL, int publicationNmbr)
        {
            if (isFreeze == 0)
            {
                //fifo order
                if (order == 0)
                {
                    //have i received any publication from this publisher?
                    lastPubMut.WaitOne();
                    if (lastPublication.ContainsKey(pubURL))
                    {
                        //see if publication is the next one in line ( or a previous one delayed by network)
                        if (publicationNmbr <= lastPublication[pubURL] + 1)
                        {
                            //do regular publication reception
                            Console.WriteLine("[ReceivePublication]");
                            //added new publisher and its corresponding publicationnmbr to the dictionary
                            Console.WriteLine("Received publication " + publicationNmbr + " from " + pubURL);
                            lastPublication[pubURL] = publicationNmbr;
                            lastPubMut.ReleaseMutex();

                            //update replicas                            
                            if (aliveReplica1)
                            {
                                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                bi.LastPublicationReplica(pubURL, publicationNmbr);
                            }
                            if (aliveReplica2)
                            {
                                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                bi2.LastPublicationReplica(pubURL, publicationNmbr);
                            }

                            //perform regular propagation
                            Console.WriteLine("Started call for Log Update");
                            PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                            PM.UpdateEventLog("BroEvent", Broker.myURL, pubURL, topic);
                            Console.WriteLine("Ended call for Log Update");

                            Console.WriteLine("Propagating...");
                            PropagatePublication(publication, pubURL, topic, propagatorURL, publicationNmbr);

                            Console.WriteLine("Sending Publication");
                            SendPublication(publication, pubURL, topic);

                            Console.WriteLine("[End of ReceivePublication]");
                            Console.WriteLine("-------------------------------");
                            //Test all remaining publications that could be waiting for this one
                            //if theres was a publication waiting that got executed, keep testing the list until no more publications can be executed      
                            do
                            {
                                foreach (var function in waitingPublications[pubURL].Values)
                                {
                                    Console.WriteLine("invoking a waiting publication");
                                    function.Invoke();
                                }
                            } while (waitingListChange);
                        }
                        //if it's not the next one, then we wait for the correct one to arrive
                        else
                        {
                            //TODO make sure they are ordered properly or something
                            Console.WriteLine("Wasn't my turn, waiting... PubNmbr:" + publicationNmbr + " from pub:" + pubURL);
                            if (waitingPublications.ContainsKey(pubURL))
                            {
                                waitingPublications[pubURL].Add(publicationNmbr, () => this.TestPublication(publication, pubURL, topic, propagatorURL, publicationNmbr));
                                lastPubMut.ReleaseMutex();
                            }
                            else
                            {
                                waitingPublications.Add(pubURL, new Dictionary<int, Action>());
                                waitingPublications[pubURL].Add(publicationNmbr, () => this.TestPublication(publication, pubURL, topic, propagatorURL, publicationNmbr));
                                lastPubMut.ReleaseMutex();
                            }

                            //update replicas
                            if (aliveReplica1)
                            {
                                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                bi.AddWaitingPubReplica(pubURL, publicationNmbr, () => this.TestPublication(publication, pubURL, topic, propagatorURL, publicationNmbr));
                            }
                            if (aliveReplica2)
                            {
                                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                //TODO try catch in case broker dies by itself without system intervention
                                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                bi2.AddWaitingPubReplica(pubURL, publicationNmbr, () => this.TestPublication(publication, pubURL, topic, propagatorURL, publicationNmbr));
                            }
                        }

                    }
                    //if i haven't then i add it to the list
                    //assume the first one received is the lowest publicationnmbr. if not, the comparison for <= takes care of the problem
                    //this has to be done for cases where publication travel up the tree halfway trough the publishing cause of a new subscription
                    else
                    {
                        Console.WriteLine("[ReceivePublication]");
                        //added new publisher and its corresponding publicationnmbr to the dictionary
                        Console.WriteLine("Received publication from a NEW publisher: " + pubURL);
                        Console.WriteLine("Received publication at number: " + publicationNmbr);
                        lastPublication.Add(pubURL, publicationNmbr);
                        waitingPublications.Add(pubURL, new Dictionary<int, Action>());
                        lastPubMut.ReleaseMutex();

                        //update replicas
                        if (aliveReplica1)
                        {
                            string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                            //TODO try catch in case broker dies by itself without system intervention
                            BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                            bi.LastPublicationReplica(pubURL, publicationNmbr);
                        }
                        if (aliveReplica2)
                        {
                            string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                            //TODO try catch in case broker dies by itself without system intervention
                            BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                            bi2.LastPublicationReplica(pubURL, publicationNmbr);
                        }

                        //perform regular propagation
                        Console.WriteLine("Started call for Log Update");
                        PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                        PM.UpdateEventLog("BroEvent", Broker.myURL, pubURL, topic);
                        Console.WriteLine("Ended call for Log Update");

                        Console.WriteLine("Propagating...");
                        PropagatePublication(publication, pubURL, topic, propagatorURL, publicationNmbr);

                        Console.WriteLine("Sending Publication");
                        SendPublication(publication, pubURL, topic);

                        Console.WriteLine("[End of ReceivePublication]");
                        Console.WriteLine("-------------------------------");
                    }
                }
                //no order
                if (order == -1)
                {
                    Console.WriteLine("Started call for Log Update");
                    PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                    PM.UpdateEventLog("BroEvent", Broker.myURL, pubURL, topic);
                    Console.WriteLine("Ended call for Log Update");

                    Console.WriteLine("[ReceivePublication]");

                    Console.WriteLine("Propagating...");
                    PropagatePublication(publication, pubURL, topic, propagatorURL, publicationNmbr);

                    Console.WriteLine("Sending Publication");
                    SendPublication(publication, pubURL, topic);

                    Console.WriteLine("[End of ReceivePublication]");
                    Console.WriteLine("-------------------------------");
                }
            }
            else { functions.Add(() => this.ReceivePublication(publication, pubURL, topic, propagatorURL, publicationNmbr)); }
        }

        //method used to propagate the publication up the Broker Tree.
        //Each Broker node sends it to his father until it reaches the root
        public void PropagatePublication(string publication, string pubURL, string topic, string propagatorURL, int publicationNmbr)
        {
            if (isFreeze == 0)
            {
                Console.WriteLine("[PropagatePublication]");
                //mode flooding
                if (Broker.isFiltering == 0)
                {
                    //check if Broker is tree root
                    if ((fatherBroker != null) && (!fatherBroker.Equals(propagatorURL)))
                    {
                        Console.WriteLine("Propagating to father");
                        BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                        fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, publicationNmbr);
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
                                childBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, publicationNmbr);
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
                                //total ordering
                                if (order == 1)
                                {
                                    Console.WriteLine("Propagating to father");
                                    BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                                    fatherBI.ReceivePublicationTOTAL(publication, pubURL, topic, Broker.myURL, publicationNmbr);
                                    Console.WriteLine("Propagated");
                                }
                                //no ordering mode
                                if (order == -1)
                                {
                                    Console.WriteLine("Propagating to father");
                                    BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                                    fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, publicationNmbr);
                                    Console.WriteLine("Propagated");
                                }
                                //fifo ordering
                                if (order == 0)
                                {
                                    //see if it's my first time sending to him
                                    //if not first time..
                                    if (NeighbourBrokerLastPub[fatherBroker].ContainsKey(pubURL))
                                    {
                                        //send the propagation with a little alteration. encapsulate the publicationNmbr from a real one to a "fake" one
                                        //check whats the last publicationNmbr he received and send next one
                                        Console.WriteLine("Propagating to father");
                                        BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                                        lastPubForBrokerMut.WaitOne();
                                        int pubNbmrforBroker = NeighbourBrokerLastPub[fatherBroker][pubURL];
                                        lastPubForBrokerMut.ReleaseMutex();
                                        fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, pubNbmrforBroker);
                                        lastPubForBrokerMut.WaitOne();
                                        NeighbourBrokerLastPub[fatherBroker][pubURL]++;
                                        lastPubForBrokerMut.ReleaseMutex();
                                        Console.WriteLine("Propagated");

                                        //update replicas
                                        if (aliveReplica1)
                                        {
                                            string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                            //TODO try catch in case broker dies by itself without system intervention
                                            BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                            bi.UpdateNeighbourPubNmbrReplica(fatherBroker, pubURL);
                                        }
                                        if (aliveReplica2)
                                        {
                                            string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                            //TODO try catch in case broker dies by itself without system intervention
                                            BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                            bi2.UpdateNeighbourPubNmbrReplica(fatherBroker, pubURL);
                                        }
                                    }
                                    //if it's first time
                                    else
                                    {
                                        //initialize dicitonary on the dict<string, dict<string, int>> variable
                                        //since its first time propagating the fake pubnumber starts at 0
                                        NeighbourBrokerLastPub[fatherBroker].Add(pubURL, 0);
                                        Console.WriteLine("Propagating to father");
                                        BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), fatherBroker);
                                        lastPubForBrokerMut.WaitOne();
                                        int pubNbmrforBroker = NeighbourBrokerLastPub[fatherBroker][pubURL];
                                        lastPubForBrokerMut.ReleaseMutex();
                                        fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, pubNbmrforBroker);
                                        lastPubForBrokerMut.WaitOne();
                                        NeighbourBrokerLastPub[fatherBroker][pubURL]++;
                                        lastPubForBrokerMut.ReleaseMutex();
                                        Console.WriteLine("Propagated");

                                        //update replicas
                                        if (aliveReplica1)
                                        {
                                            string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                            //TODO try catch in case broker dies by itself without system intervention
                                            BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                            bi.UpdateNeighbourPubNmbrReplica(fatherBroker, pubURL);
                                        }
                                        if (aliveReplica2)
                                        {
                                            string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                            //TODO try catch in case broker dies by itself without system intervention
                                            BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                            bi2.UpdateNeighbourPubNmbrReplica(fatherBroker, pubURL);
                                        }
                                    }
                                }
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
                                    //total ordering
                                    if (order == 1)
                                    {
                                        Console.WriteLine("Propagating to father");
                                        BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                        fatherBI.ReceivePublicationTOTAL(publication, pubURL, topic, Broker.myURL, publicationNmbr);
                                        Console.WriteLine("Propagated");
                                    }
                                    //No ordering mode
                                    if (order == -1)
                                    {
                                        Console.WriteLine("Propagating to child(s)");
                                        BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                        bi.ReceivePublication(publication, pubURL, topic, Broker.myURL, publicationNmbr);
                                        Console.WriteLine("Propagated");
                                    }
                                    //fifo ordering
                                    if (order == 0)
                                    {
                                        //see if it's my first time sending to him
                                        //if not first time..
                                        if (NeighbourBrokerLastPub[child].ContainsKey(pubURL))
                                        {
                                            //send the propagation with a little alteration. encapsulate the publicationNmbr from a real one to a "fake" one
                                            //check whats the last publicationNmbr he received and send next one
                                            Console.WriteLine("Propagating to child(s)");
                                            BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                            lastPubForBrokerMut.WaitOne();
                                            int pubNbmrforBroker = NeighbourBrokerLastPub[child][pubURL];
                                            lastPubForBrokerMut.ReleaseMutex();
                                            fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, pubNbmrforBroker);
                                            lastPubForBrokerMut.WaitOne();
                                            NeighbourBrokerLastPub[child][pubURL]++;
                                            lastPubForBrokerMut.ReleaseMutex();
                                            Console.WriteLine("Propagated");

                                            //update replicas
                                            if (aliveReplica1)
                                            {
                                                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                                //TODO try catch in case broker dies by itself without system intervention
                                                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                                bi.UpdateNeighbourPubNmbrReplica(child, pubURL);
                                            }
                                            if (aliveReplica2)
                                            {
                                                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                                //TODO try catch in case broker dies by itself without system intervention
                                                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                                bi2.UpdateNeighbourPubNmbrReplica(child, pubURL);
                                            }
                                        }
                                        //if it's first time
                                        else
                                        {
                                            //initialize dicitonary on the dict<string, dict<string, int>> variable
                                            //since its first time propagating the fake pubnumber starts at 0
                                            NeighbourBrokerLastPub[child].Add(pubURL, 0);
                                            Console.WriteLine("Propagating to child(s)");
                                            BrokerInterface fatherBI = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), child);
                                            lastPubForBrokerMut.WaitOne();
                                            int pubNbmrforBroker = NeighbourBrokerLastPub[child][pubURL];
                                            lastPubForBrokerMut.ReleaseMutex();
                                            fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, pubNbmrforBroker);
                                            lastPubForBrokerMut.WaitOne();
                                            NeighbourBrokerLastPub[child][pubURL]++;
                                            lastPubForBrokerMut.ReleaseMutex();
                                            fatherBI.ReceivePublication(publication, pubURL, topic, Broker.myURL, pubNbmrforBroker);
                                            Console.WriteLine("Propagated");

                                            //update replicas
                                            if (aliveReplica1)
                                            {
                                                string brkReplica1 = transformURL(Broker.processname, Broker.myURL, 1);
                                                //TODO try catch in case broker dies by itself without system intervention
                                                BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica1);
                                                bi.UpdateNeighbourPubNmbrReplica(child, pubURL);
                                            }
                                            if (aliveReplica2)
                                            {
                                                string brkReplica2 = transformURL(Broker.processname, Broker.myURL, 2);
                                                //TODO try catch in case broker dies by itself without system intervention
                                                BrokerInterface bi2 = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), brkReplica2);
                                                bi2.UpdateNeighbourPubNmbrReplica(child, pubURL);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                Console.WriteLine("[End of PropagatePublication]");
                Console.WriteLine("-------------------------------");
            }
            else { functions.Add(() => this.PropagatePublication(publication, pubURL, topic, Broker.myURL, publicationNmbr)); }
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
                            //different behaviour for total ordering mode
                            if ((Broker.isFiltering == 1) && (order == 1))
                            {
                                BrokerTicketInterface bt = (BrokerTicketInterface)Activator.GetObject(typeof(BrokerTicketInterface), "tcp://localhost:9999/brokerticket");
                                bt.DecreaseInterested();
                            }

                            SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), subscriber);
                            newSubscriber.ReceivePublication(publication, pubURL, publicationTopic);
                        }
                    }
                }
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
                Console.WriteLine("I'm alive at: " + Broker.myURL);
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
                fatherBI.NewSubscriptionForFather(Broker.myURL, subscription);
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
                fatherBI.RemoveSubscriptionForFather(Broker.myURL, topic);
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

        public int GetTicket()
        {
            Console.WriteLine("Getting Global Ticket");
            int ticket;
            BrokerTicketInterface bt = (BrokerTicketInterface)Activator.GetObject(typeof(BrokerTicketInterface), "tcp://localhost:9999/brokerticket");
            ticket = bt.GetTicket();
            Console.WriteLine("Ticket is " + ticket);
            return ticket;
        }

        //--------------------------------------------------------
        //from now on method are pretty much duplicated.
        // the difference is this method are used by the leader to keep replicas up to date.
        //--------------------------------------------------------

        //method to transform a broker url into one of his replicas
        //done by adding a predefined value into the port
        //and changing the processname for that of the replica
        private string transformURL(string processname, string url, int replicaNum)
        {
            string[] parsedURL = url.Split(':');  //parsedURL[0] = "tcp"; parsedURL[1]= "//localhost"; parsedURL[2]= "PORT/broker";
            string[] parsedURLv2 = parsedURL[2].Split('/'); //parsedURLv2[0] = "PORT"; parsedURLv2[1]= "broker";

            //since 2 processes can't sahre same port then replicas add 20 or 21 to port number
            //to prevent collision with other replicas we also add the last number from this specific replica
            if (replicaNum == 1)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 20 + (int.Parse(parsedURLv2[0]) % 100)).ToString();
            if (replicaNum == 2)
                parsedURLv2[0] = (int.Parse(parsedURLv2[0]) + 21 + (int.Parse(parsedURLv2[0]) % 100)).ToString();

            //rejoin modified parsels into the new URL
            string newURLv2 = string.Join("/", parsedURLv2);
            parsedURL[2] = newURLv2;
            string newURL = string.Join(":", parsedURL);
            return newURL;
        }

        //method to propagate connections from leader to replicas
        //the replica then adds the connection to it's list
        public void ConnectSubscriberReplica(string subURL)
        {
            //add subscriber to the Dictionary.
            List<string> auxlist = new List<string>(); /*auxlist to init list of subscriptions*/
            subscribers.Add(subURL, auxlist);
            System.Console.WriteLine("Replica:Subscriber at: " + subURL + " connected");
        }

        //method to propagate connections from leader to replicas
        //the replica then adds the connection to it's list
        public void ConnectPublisherReplica(string pubURL)
        {
            //add publisher to the Dictionary.
            publishers.Add(pubURL, "root");
            Console.WriteLine("Replica:Publisher at: " + pubURL + " connected");
        }

        //method to propagate subscriptions from leader to replicas
        //the replica then adds the subscription to it's List
        public void AddSubscriptionReplica(string subURL, string subscription)
        {
            /*Verify if subURL is on the List*/
            if (subscribers.ContainsKey(subURL))
            {
                /*Verify if already subscribed to topic*/
                if (!subscribers[subURL].Contains(subscription))
                {
                    subscribers[subURL].Add(subscription);
                    System.Console.WriteLine("Replica:" + subURL + " subscribed to: " + subscription);
                }
                else
                {
                    System.Console.WriteLine("Replica:" + subURL + " already subscribed to " + subscription);
                }
            }
            else
            {
                //TODO throw an exception to the subscriber
                Console.WriteLine("Replica:There is no such Subscriber connected to this Broker");
                Console.WriteLine(subURL);
            }
        }

        //method to propagate subscription removal from leader to replicas
        //the replica then removes the subscription from it's List
        public void RemoveSubscriptionReplica(string subURL, string topic)
        {
            Console.WriteLine("[Replica:RemoveSubscription]");
            //Verify if subURL is on the List
            if (subscribers.ContainsKey(subURL))
            {
                //Verify subscriber is subscribed to topic
                if (subscribers[subURL].Contains(topic))
                {
                    //Normal remove of topic
                    subscribers[subURL].Remove(topic);
                    Console.WriteLine("Replica:" + topic + " removed from " + subURL);
                }
                else
                {
                    //TODO throw an exception to the subscriber
                    Console.WriteLine("Replica:There is no such topic");
                }
            }
            else
            {
                //TODO throw an exception to the subscriber
                Console.WriteLine("Replica:There is no such Subscriber connected to this Broker");
            }
            Console.WriteLine("-------------------------------");
        }

        //method to propagate publisher topic from leader to replicas
        //the replica then associates the topic to the publisher in it's List
        public void ChangePublishingTopicReplica(string pubURL, string topic)
        {
            if (publishers.ContainsKey(pubURL))
            {
                publishers[pubURL] = topic;
                Console.WriteLine("Replica:" + pubURL + " publishing to: " + topic);
            }
            else
            {
                //TODO trow and exception to the publisher
                Console.WriteLine("Replica: There is no such Publisher connected to this Broker");
            }
        }

        //method used to actualize booleans
        //each booleans is the state of a replica.
        //when one takes over its according boolean goes from true to false
        public void ActualizeLeader(char replicaNumber)
        {
            Console.WriteLine("Leader-Broker actualized");
            if (replicaNumber == '1')
                aliveReplica1 = false;
            if (replicaNumber == '2')
                aliveReplica2 = false;
        }

        //method to propagate broker childs from leader to replicas
        //the replica then adds the childs URL and propreties ubti it's tables
        public void AddChildReplica(string url)
        {
            //adds child to list of childs
            if (!(childBroker.Contains(url)) && childBroker != null)
            {
                childBroker.Add(url);
                Console.WriteLine("Replica: Added child");
            }

            //creates and entrance in the hashmap for this child and its future subscriptions.
            //used for filtering routing
            if (!(childsSubscriptions.ContainsKey(url)))
            {
                childsSubscriptions.Add(url, new List<string>());
            }

            //make space in dictionary for this broker ( one of te childs )
            //used for fifo ordering
            if (!(NeighbourBrokerLastPub.ContainsKey(url)))
            {
                NeighbourBrokerLastPub.Add(url, new Dictionary<string, int>());
            }
        }

        public void RemoveWaitingPubReplica(string pubURL, int publicationNmbr)
        {
            lastPubMut.WaitOne();
            waitingPublications[pubURL].Remove(publicationNmbr);
            lastPubMut.ReleaseMutex();
            Console.WriteLine("Replica: Removed waiting action nº " + publicationNmbr + " for " + pubURL);
        }

        public void LastPublicationReplica(string pubURL, int pubNmbr)
        {
            lastPubMut.WaitOne();
            if (lastPublication.ContainsKey(pubURL))
            {
                lastPublication[pubURL] = pubNmbr;
                lastPubMut.ReleaseMutex();
            }
            else
            {
                lastPublication.Add(pubURL, pubNmbr);
                waitingPublications.Add(pubURL, new Dictionary<int, Action>());
                lastPubMut.ReleaseMutex();
            }
            Console.WriteLine("Replica: Added publication nº " + pubNmbr + " from " + pubURL);
        }

        public void AddWaitingPubReplica(string pubURL, int publicationNmbr, Action action)
        {
            lastPubMut.WaitOne();
            if (waitingPublications.ContainsKey(pubURL))
            {
                waitingPublications[pubURL].Add(publicationNmbr, action);
                lastPubMut.ReleaseMutex();
            }
            else
            {
                waitingPublications.Add(pubURL, new Dictionary<int, Action>());
                waitingPublications[pubURL].Add(publicationNmbr, action);
                lastPubMut.ReleaseMutex();
            }
            Console.WriteLine("Replica: Added Publication nº " + publicationNmbr + " to waiting queue");
        }

        public void UpdateNeighbourPubNmbrReplica(string BrokerURL, string pubURL)
        {
            int pubNbmrforBroker = 1;
            if (!(NeighbourBrokerLastPub.ContainsKey(BrokerURL)))
            {
                lastPubForBrokerMut.WaitOne();
                NeighbourBrokerLastPub.Add(BrokerURL, new Dictionary<string, int>());
                lastPubForBrokerMut.ReleaseMutex();
            }
            if (NeighbourBrokerLastPub[BrokerURL].ContainsKey(pubURL))
            {
                lastPubForBrokerMut.WaitOne();
                NeighbourBrokerLastPub[BrokerURL][pubURL]++;
                pubNbmrforBroker = NeighbourBrokerLastPub[BrokerURL][pubURL];
                lastPubForBrokerMut.ReleaseMutex();
            }
            else
            {
                NeighbourBrokerLastPub[BrokerURL].Add(pubURL, 0);
                lastPubForBrokerMut.WaitOne();
                pubNbmrforBroker = NeighbourBrokerLastPub[BrokerURL][pubURL];
                lastPubForBrokerMut.ReleaseMutex();
            }
            Console.WriteLine("Replica: Updated broker:" + BrokerURL + " with last pub nº received:" + pubNbmrforBroker);
        }

        public void RemoveWaitingPubTOTALReplica(int ticket)
        {
            lastPubTOTALMut.WaitOne();
            waitingPublicationsTOTAL.Remove(ticket);
            lastPubTOTALMut.ReleaseMutex();
            Console.WriteLine("Replica: Removed waiting action nº " + ticket);
        }

        public void LastPublicationTOTALReplica(int ticket)
        {
            lastPubTOTALMut.WaitOne();
            lastPublicationTOTAL = ticket;
            lastPubTOTALMut.ReleaseMutex();
            Console.WriteLine("Replica: Updated last received publication to: " + ticket);
        }

        public void AddWaitingPubTOTALReplica(int ticket, Action function)
        {
            lastPubTOTALMut.WaitOne();
            waitingPublicationsTOTAL.Add(ticket, function);
            lastPubTOTALMut.ReleaseMutex();
            Console.WriteLine("Replica: Added waiting action nº " + ticket);
        }
    }
}
