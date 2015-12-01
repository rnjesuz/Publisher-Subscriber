using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        void ConnectSubscriberReplica(string pubURL);
        void ConnectPublisherReplica(string pubURL);
        void AddSubscriptionReplica(string subURL, string subscription);
        void RemoveSubscriptionReplica(string subURL, string subscription);
        void ChangePublishingTopicReplica(string pubURL, string topic);
        void ActualizeLeader(char replicanumber);
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
}
