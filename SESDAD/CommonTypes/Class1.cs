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
        void ConnectSubscriber();
        void AddSubscription(SubscriberInterface subscriber, string subscription);
        void ConnectPublisher();
        void ChangePublishTopic(PublisherInterface publisher, string topic);
        void ConnectFatherBroker(int port);
        void ReceivePublication(string publication, PublisherInterface publisher);
        void PropagatePublication(string publication, PublisherInterface publisher);
        void SendPublication(string publication, string topic);
    }
    
    public interface PublisherInterface
    {
        void ConnectBroker();
        void ChangeTopic(string topic);
    }
    public interface SubscriberInterface
    {
        void ReceivePublication(string publication);
    }
}
