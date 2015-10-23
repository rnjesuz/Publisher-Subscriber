using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace SESDAD
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            TcpChannel channel = new TcpChannel(8086);
            ChannelServices.RegisterChannel(channel, false);

            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(RemoteBroker),
                "BrokerServer",
                WellKnownObjectMode.Singleton);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            
        }
    }

    class RemoteBroker: MarshalByRefObject, BrokerInterface
    {
        //Dictionary of every publisher connected to this Broker and his topic 
        Dictionary<PublisherInterface, string> publishers = new Dictionary<PublisherInterface, string>();
        //Dictionary of every subscriber connected to this Broker and his subscription
        Dictionary<SubscriberInterface, string> subscribers = new Dictionary<SubscriberInterface, string>();
        //Father node in the Broker Tree. CANNOT be NULL
        BrokerInterface fatherBroker;
        //Child node in the Broker Tree. CAN be NULL
        BrokerInterface childBroker;

        //function called by a subscriber wishing to connect to this broker
        public void ConnectSubscriber()
        {
            SubscriberInterface newSubscriber = (SubscriberInterface)Activator.GetObject(typeof(SubscriberInterface), "tcp://localhost:8090/SubscriberServer");
            //add subscriber to the Dictionary. By default the subscription is of every publication ( denoted by root/ )
            subscribers.Add(newSubscriber, "root/");
        }

        public void AddSubscription(SubscriberInterface subscriber, string subscription)
        {
            if (subscribers.ContainsKey(subscriber))
            {
                subscribers[subscriber] = subscription;
            }
            else
            {
                //TODO trow an exception to the subscriber
                Console.WriteLine("There is no such Subscriber Connected to this Broker");
            }
        }
        
        public void ConnectPublisher()
        {
            PublisherInterface newPublisher = (PublisherInterface)Activator.GetObject(typeof(PublisherInterface), "tcp://localhost:8088/PublisherServer");
            //add publisher to the Dictionary. By default the publisher publishes to the general topic ( denoted by root/ )
            publishers.Add(newPublisher, "root/" );
        }

        //change the topic to wich a publisher will write
        public void ChangePublishTopic(PublisherInterface publisher, string topic)
        {
            if (publishers.ContainsKey(publisher))
            {
                publishers[publisher] = topic;
            }
            else
            {
                //TODO trow and exception to the publisher
                Console.WriteLine("There is no such Publisher connected to this Broker");
            }
        }

        //get the port where remote fatherbroker is and conect to it
        //TODO Get the URL also. localhost for testing only
        public void ConnectFatherBroker(int port)
        {
            fatherBroker = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://localhost:"+ port +"/BrokerServer");
        }

        //method called by a publisher to publish a publication
        //or
        //method called by a child broker to propagate a publication
        public void ReceivePublication(string publication, PublisherInterface publisher)
        {
                PropagatePublication(publication, publisher);
                SendPublication(publication, publishers[publisher]);
        }

        //method used to propagate the publication up the Broker Tree.
        //Each Broker node sends it to his father until it reaches the root
        public void PropagatePublication(string publication, PublisherInterface publisher)
        {
            //check if Broker is tree root
            if(fatherBroker != null)
                fatherBroker.ReceivePublication(publication, publisher);
        }

        //method used to send the publication to one or several subscribers of the broker
        //checks if any subscriber is intereted in the topic, and sends it to them if yes
        public void SendPublication(string publication, string publicationTopic)
        {
            //See if any subscriber is interested in this publication
            foreach(SubscriberInterface subscriber in subscribers.Keys)
            {
                if (subscribers[subscriber].Equals(publicationTopic))
                       subscriber.ReceivePublication(publication);
            }
        }

    }
}
