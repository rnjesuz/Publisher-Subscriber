﻿using System;
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
        void ReceivePublication(string publication, string pubURL);
        void PropagatePublication(string publication, string pubURL);
        void SendPublication(string publication, string pubURL, string publicationTopic);
        void AddChild(string url);
    }
    
    public interface PublisherInterface
    {
        void ChangeTopic(string topic);
        void SendPublication(string publication);
    }
    public interface SubscriberInterface
    {
        void ReceivePublication(string publication, string pubURL, string pubTopic);
    }

    public interface PMInterface
    {
        void UpdateEventLog(string eventlabel, string p1, string p2, string topicname);
    }
}
