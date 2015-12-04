using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SESDAD
{
    public class Class1
    {

    }

    public interface BrokerInterface
    {
        void StartPing();
        void ReceivePing();
        void ConnectSubscriber(string pubURL);
        void AddSubscription(string subURL, string subscription);
        void RemoveSubscription(string subURL, string topic);
        void ConnectPublisher(string subURL);
        void ChangePublishTopic(string pubURL, string topic);
        void ConnectFatherBroker(string url);
        void ReceivePublicationTOTAL(string publication, string pubURL, string topic, string propagatorURL, int ticket);
        void ReceivePublication(string publication, string pubURL, string topic, string propagatorURL, int publicationNumber);
        void PropagatePublication(string publication, string pubURL, string topic, string propagatorURL, int publicationNumber);
        void SendPublication(string publication, string pubURL, string publicationTopic);
        void AddChild(string url);
        void Kill();
        void Freeze();
        void Unfreeze();
        void StatusUpdate();
        void NewSubscriptionForFather(string myurl, string subscription);
        void NewSubscriptionForChild(string subscription);
        void RemoveSubscriptionForFather(string myURL, string topic);
        void RemoveSubscriptionForChild(string topic);
        int GetTicket();

        void ConnectSubscriberReplica(string pubURL);
        void ConnectPublisherReplica(string pubURL);
        void AddSubscriptionReplica(string subURL, string subscription);
        void RemoveSubscriptionReplica(string subURL, string subscription);
        void ChangePublishingTopicReplica(string pubURL, string topic);
        void ActualizeLeader(char replicanumber);
        void AddChildReplica(string url);
        void RemoveWaitingPubReplica(string pubURL, int publicationNmbr);
        void LastPublicationReplica(string pubURL, int pubNmbr);
        void AddWaitingPubReplica(string pubURL, int publicationNmbr, Action action);
        void UpdateNeighbourPubNmbrReplica(string BrokerURL, string pubURL);
        void RemoveWaitingPubTOTALReplica(int ticket);
        void LastPublicationTOTALReplica(int ticket);
        void AddWaitingPubTOTALReplica(int ticket, Action function);
    }

    public interface PublisherInterface
    {
        void ChangeTopic(string topic);
        void SendPublication(string publication);
        void MultipleSendPublication(string publication, int sleepInterval, int numberofevents, string topicName);
        void Kill();
        void Freeze();
        void Unfreeze();
        void StatusUpdate();

    }
    public interface SubscriberInterface
    {
        void ReceivePublication(string publication, string pubURL, string pubTopic);
        void AddSubscription(string topic);
        void AddSubscriptionRemote(string topic);
        void RemoveSubscription(string topic);
        void RemoveSubscriptionRemote(string topic);
        void Kill();
        void Freeze();
        void Unfreeze();
        void StatusUpdate();
    }

    public interface PMInterface
    {
        void UpdateEventLog(string eventlabel, string p1, string p2, string topicname);
        void SendSubscribeOrder(String subURL, string topic);
        void SendUnsubscribeOrder(String subURL, string topic);
        void SendPublishOrder(String pubURL, string processName, string topicname, int numberofevents, int sleepInterval);
        void KillBroker(string URL);
        void KillSubscriber(string URL);
        void KillPublisher(string URL);
        void FreezeBroker(string URL);
        void FreezeSubscriber(string URL);
        void FreezePublisher(string URL);
        void UnfreezeBroker(string URL);
        void UnfreezeSubscriber(string URL);
        void UnfreezePublisher(string URL);
        void StatusUpdate();
        void Quit();
    }

    public interface BrokerTicketInterface
    {
        void Start();
        int GetTicket();
        void Lock();
        void UpdateInterested(int interested);
        void DecreaseInterested();
    }

    //this class is a class available to all brokers.
    //its a simple class that is used to dispense "tickets" for other processes to use
    public static class BrokerTicket
    {
        //an integer that saves the last given ticket
        static private int ticket = 0;
        //a lock to protect acess to the tickets
        static private Mutex lockTicket = new Mutex();
        //lock used while in totalordering AND filtering mode to lock publishing of messages on the system
        static private Mutex lockPublishing = new Mutex();
        //int used in total order AND filtering mode to save intereted nodes in the system
        static private int interestedNodes = 0;

        static private FileStream fs = new FileStream(@"" + Directory.GetCurrentDirectory() + "\\..\\..\\Ticket.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite);

        //dispenses the tickets and increments the counter
        public static int GetTicket()
        {
            int Ticket;
            lockTicket.WaitOne();
            fs.Lock(0, 1);
            const Int32 BufferSize = 512;
            StreamReader streamReader = new StreamReader(fs);
            String line = streamReader.ReadLine();
            Ticket = Int32.Parse(line);
            Ticket++;

            StreamWriter writer = new StreamWriter(fs);
            writer.WriteLine(Ticket.ToString());
            writer.Flush();

            fs.Unlock(0, 1);
            lockTicket.ReleaseMutex();
            return Ticket;
        }

        public static void Lock()
        {
            lockPublishing.WaitOne();
        }

        public static void UpdateInterested(int interested)
        {
            if (interested > interestedNodes)
                interestedNodes = interested;
        }

        public static void DecreaseInterested()
        {
            if (interestedNodes == 0)
                lockPublishing.ReleaseMutex();
            else
                interestedNodes--;
        }
        
    }
}
