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

            rs.AddSubscription(topic);
        }

        private void TopicBox_TextChanged(object sender, EventArgs e)
        {

        }

        internal void UpdatePublication(string message)
        {
            PublicationBox.Text = message + "\r\n";
        }

        private void PublicationBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
