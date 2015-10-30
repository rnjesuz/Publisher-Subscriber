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
        void ConnectSubscriber(string pubURL);
        void AddSubscription(string subURL, string subscription);
        void RemoveSubscription(string subURL, string topic);
        void ConnectPublisher(string subURL);
        void ChangePublishTopic(string pubURL, string topic);
        void ConnectFatherBroker(string url);
        void ReceivePublication(string publication, string pubURL, string topic);
        void PropagatePublication(string publication, string pubURL, string topic);
        void SendPublication(string publication, string pubURL, string publicationTopic);
        void AddChild(string url);
        void Kill();
        void StatusUpdate();
    }
    
    public interface PublisherInterface
    {
        void ChangeTopic(string topic);
        void SendPublication(string publication);
        void Kill();
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
        void StatusUpdate();
    }
}
