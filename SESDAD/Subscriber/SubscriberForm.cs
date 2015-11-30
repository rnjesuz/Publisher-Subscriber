using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SESDAD
{
    public partial class SubscriberForm : Form
    {
        private RemoteSubscriber rs = new RemoteSubscriber();
        string topic;
        internal List<string> subscriptions = new List<string>();

        public SubscriberForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        
        }

        private void SubscribeButton_Click(object sender, EventArgs e)
        {
            topic = TopicBox.Text;
            //if topic was already subscribed therse no need to subscribe again
            if (!subscriptions.Contains(topic))
            {
                subscriptions.Add(topic);
                TopicListBox.Text = string.Join("\r\n", subscriptions);
                rs.AddSubscription(topic);
            }
        }

        private void TopicBox_TextChanged(object sender, EventArgs e)
        {

        }

        internal void UpdatePublication(string message)
        {
            PublicationBox.Text = PublicationBox.Text + message + "\r\n";
        }

        private void PublicationBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void UnsubButton_Click(object sender, EventArgs e)
        {
            string topic = UnsubBox.Text;
            //checks if the topic was ever subscribed to, before unsubing
            if (subscriptions.Contains(topic))
            {
                subscriptions.Remove(topic);
                TopicListBox.Text = string.Join("\r\n", subscriptions);
                rs.RemoveSubscription(topic);
            }
        }

        private void TopicListBox_TextChanged(object sender, EventArgs e)
        {

        }

        internal void AddTopic(string topic)
        {
            subscriptions.Add(topic);
            TopicListBox.Text = string.Join("\r\n", subscriptions);
        }

        internal void RemoveTopic(string topic)
        {
            if (subscriptions.Contains(topic))
            {
                subscriptions.Remove(topic);
                TopicListBox.Text = string.Join("\r\n", subscriptions);
            }
        }
    }
}
