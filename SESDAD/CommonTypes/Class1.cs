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
        void ReceivePublication(string publication);
        void PropagatePublication(string publication);
        void SendPublication(string publication);
    }
    
    public interface PublisherInterface
    {

    }
    public interface SubscriberInterface
    {
        void ReceivePublication(string publication);
    }
}
