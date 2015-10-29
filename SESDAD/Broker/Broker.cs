using System;
using System.Collections;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System.Diagnostics;

namespace SESDAD
{
    public class Broker
    {
        private static int myPort;
        internal static string myURL;
        internal static string fatherURL;
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

            TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker),"broker",WellKnownObjectMode.Singleton);

            BrokerInterface rb =(BrokerInterface)Activator.GetObject(typeof(BrokerInterface), myURL);
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

    class RemoteBroker: MarshalByRefObject, BrokerInterface
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

        //function called by a subscriber wishing to connect to this broker
        public void ConnectSubscriber(string subURL)
        {
            //SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:8090/SubscriberServer");

            //add subscriber to the Dictionary. By default the subscription is of every publication ( denoted by root/ )
            List<string> auxlist = new List<string>(); /*auxlist to init list of subscriptions*/
            auxlist.Add("root");
            subscribers.Add(subURL, auxlist);
            System.Console.WriteLine("Subscriber at: "+subURL+ " connected");
        }

        public void AddSubscription(string subURL, string subscription)
        {
            /*Verify if subURL is on the List*/
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

        public void RemoveSubscription(string subURL, string topic)
        {
            //Verify if subURL is on the List
            if (subscribers.ContainsKey(subURL))
            {
                //Verify subscriber is subscribed to topic
                if (subscribers[subURL].Contains("root/"+topic))
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
                        Console.WriteLine(topic+" removed from " + subURL);
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
        }
        
        public void ConnectPublisher(string pubURL)
        {
            //PublisherInterface newPublisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:8088/PublisherServer");
            //add publisher to the Dictionary. By default the publisher publishes to the general topic ( denoted by root/ )

            publishers.Add(pubURL, "root");
            Console.WriteLine("Publisher at: "+pubURL+" connected");
        }

        //change the topic to which a publisher will write
        public void ChangePublishTopic(string pubURL, string topic)
        {
            if (publishers.ContainsKey(pubURL))
            {
                publishers[pubURL]="root/"+ topic;
                Console.WriteLine(pubURL+" publishing to: " + topic);
            }
            else
            {
                //TODO trow and exception to the publisher
                Console.WriteLine("There is no such Publisher connected to this Broker");
            }
        }

        //conection to father url
        public void ConnectFatherBroker(string url)
        {
            if (url != null){
                fatherBroker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
                fatherBroker.AddChild(myURL);
            }
        }

        //add a child broker to the list
        public void AddChild(string url)
        {
            BrokerInterface bi = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url);
            if (!(childBroker.Contains(bi)) && childBroker != null)
            {
                childBroker.Add((BrokerInterface)Activator.GetObject(typeof(BrokerInterface), url));
            }
        }

        //method called by a publisher to publish a publication
        //or
        //method called by a child broker to propagate a publication
        public void ReceivePublication(string publication, string pubURL)
        {
            PropagatePublication(publication, pubURL);

            SendPublication(publication, pubURL, publishers[pubURL]);
        }

        //method used to propagate the publication up the Broker Tree.
        //Each Broker node sends it to his father until it reaches the root
        public void PropagatePublication(string publication, string pubURL)
        {
            //check if Broker is tree root
            if (fatherBroker != null)
            {
                fatherBroker.ReceivePublication(publication, pubURL);
                
                PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                PM.UpdateEventLog("BroEvent", myURL, pubURL, publishers[pubURL]);
            }

            if (childBroker != null)
            {
                foreach (BrokerInterface child in childBroker)
                {
                    child.ReceivePublication(publication, pubURL);

                    PMInterface PM = (PMInterface)Activator.GetObject(typeof(PMInterface), "tcp://localhost:8069/puppetmaster");
                    PM.UpdateEventLog("BroEvent", myURL, pubURL, publishers[pubURL]);
                }
            }
        }

        //method used to send the publication to one or several subscribers of the broker
        //checks if any subscriber is intereted in the topic, and sends it to them if yes
        public void SendPublication(string publication, string pubURL, string publicationTopic)
        {
            //See if any subscriber is interested in this publication
            foreach(String subscriber in subscribers.Keys)
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
        }

    }
}
