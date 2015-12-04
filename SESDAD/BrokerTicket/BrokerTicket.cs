using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SESDAD
{
    public class BrokerTicket
    {
        internal static TcpChannel channel;
        internal static RemoteBrokerTicket rbt;


        static void Main(string[] args)
        {

            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            int Port = 9999;
            props["port"] = Port;

            channel = new TcpChannel(props, null, provider);

            //TcpChannel channel = new TcpChannel(myPort);
            ChannelServices.RegisterChannel(channel, false);

            rbt = new RemoteBrokerTicket();

            RemotingServices.Marshal(rbt, "brokerticket", typeof(RemoteBrokerTicket));
            //RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteBroker), "broker", WellKnownObjectMode.Singleton);
            BrokerInterface rb = (BrokerInterface)Activator.GetObject(typeof(BrokerInterface), "tcp://localhost:9999/brokerticket");


            //solely to prevent console from closing
            while (true) { }
        }
    }
    class RemoteBrokerTicket : MarshalByRefObject, BrokerTicketInterface
    {

        //an integer that saves the last given ticket
        static private int ticket = 0;
        //a lock to protect acess to the tickets
        static private Mutex lockTicket = new Mutex();
        //lock used while in totalordering AND filtering mode to lock publishing of messages on the system
        static private Mutex lockPublishing = new Mutex();
        //int used in total order AND filtering mode to save intereted nodes in the system
        static private int interestedNodes = 0;

        //dispenses the tickets and increments the counter
        public int GetTicket()
        {
            int newTicket;
            lockTicket.WaitOne();
            newTicket = ticket++;
            lockTicket.ReleaseMutex();
            return newTicket;
        }

        public void Lock()
        {
            lockPublishing.WaitOne();
        }

        public void UpdateInterested(int interested)
        {
            if (interested > interestedNodes)
                interestedNodes = interested;
        }

        public void DecreaseInterested()
        {
            if (interestedNodes == 0)
                lockPublishing.ReleaseMutex();
            else
                interestedNodes--;
        }

    }
}
}
